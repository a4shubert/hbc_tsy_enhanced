using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HbcRest.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Linq.Expressions;
using System.Text.Json.Serialization;

namespace HbcRest;

internal static class ApiHelpers
{
    private static PropertyInfo? ResolveProperty<T>(string columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName)) return null;
        var name = columnName.Trim();

        var props = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        foreach (var p in props)
        {
            if (p.Name.Equals(name, System.StringComparison.OrdinalIgnoreCase)) return p;

            var jsonName = p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name;
            if (!string.IsNullOrWhiteSpace(jsonName) &&
                jsonName.Equals(name, System.StringComparison.OrdinalIgnoreCase))
            {
                return p;
            }

            var colAttr = p.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.ColumnAttribute>()?.Name;
            if (!string.IsNullOrWhiteSpace(colAttr) &&
                colAttr.Equals(name, System.StringComparison.OrdinalIgnoreCase))
            {
                return p;
            }
        }

        return null;
    }

    private static object? CoerceStringToType(string raw, System.Type targetType)
    {
        var t = System.Nullable.GetUnderlyingType(targetType) ?? targetType;
        var s = (raw ?? "").Trim();

        if (t == typeof(string)) return s;
        if (t == typeof(int)) return int.TryParse(s, out var v) ? v : null;
        if (t == typeof(long)) return long.TryParse(s, out var v) ? v : null;
        if (t == typeof(double)) return double.TryParse(s, out var v) ? v : null;
        if (t == typeof(float)) return float.TryParse(s, out var v) ? v : null;
        if (t == typeof(decimal)) return decimal.TryParse(s, out var v) ? v : null;
        if (t == typeof(bool))
        {
            if (bool.TryParse(s, out var bv)) return bv;
            if (s == "1") return true;
            if (s == "0") return false;
            return null;
        }
        if (t == typeof(System.DateTime))
        {
            return System.DateTime.TryParse(s, out var dt) ? dt : null;
        }

        try
        {
            return System.Convert.ChangeType(s, t);
        }
        catch
        {
            return null;
        }
    }

    private static Expression BuildBinary(Expression left, string op, Expression right)
    {
        return op.ToLowerInvariant() switch
        {
            "eq" or "=" => Expression.Equal(left, right),
            "ne" or "!=" => Expression.NotEqual(left, right),
            "gt" or ">" => Expression.GreaterThan(left, right),
            "ge" or ">=" => Expression.GreaterThanOrEqual(left, right),
            "lt" or "<" => Expression.LessThan(left, right),
            "le" or "<=" => Expression.LessThanOrEqual(left, right),
            _ => throw new System.NotSupportedException($"Unsupported operator: {op}")
        };
    }

    public static IQueryable<T> ApplyFilterGeneric<T>(string filter, IQueryable<T> query)
    {
        if (string.IsNullOrWhiteSpace(filter)) return query;

        // Supports:
        // - comparisons: col (eq|ne|gt|ge|lt|le|=|!=|>|>=|<|<=) literal
        // - boolean operators: and/or
        // - parentheses: ( ... )
        // - string literals in single quotes (supports OData-style escaping: '' inside string)
        //
        // Fail-fast on unknown columns/operators to avoid silent no-op filters.
        var tokenizer = new FilterTokenizer(filter);
        var tokens = tokenizer.Tokenize();
        var parser = new FilterParser(tokens);
        var ast = parser.ParseExpression();
        parser.ExpectEnd();

        var param = Expression.Parameter(typeof(T), "x");
        var expr = BuildFilterExpression<T>(ast, param);

        var lambda = Expression.Lambda<System.Func<T, bool>>(expr, param);
        return query.Where(lambda);
    }

    private static Expression BuildFilterExpression<T>(FilterNode node, ParameterExpression param)
    {
        return node switch
        {
            FilterLogicalNode ln => ln.Op == "or"
                ? Expression.OrElse(BuildFilterExpression<T>(ln.Left, param), BuildFilterExpression<T>(ln.Right, param))
                : Expression.AndAlso(BuildFilterExpression<T>(ln.Left, param), BuildFilterExpression<T>(ln.Right, param)),
            FilterComparisonNode cn => BuildComparisonExpression<T>(cn, param),
            FilterFunctionNode fn => BuildFunctionExpression<T>(fn, param),
            _ => throw new System.NotSupportedException("Unsupported filter AST node.")
        };
    }

    private static Expression BuildFunctionExpression<T>(FilterFunctionNode node, ParameterExpression param)
    {
        var name = node.Name.ToLowerInvariant();
        if (name != "contains")
        {
            throw new System.NotSupportedException($"Unsupported function in $filter: {node.Name}");
        }

        if (node.Args.Count != 2)
        {
            throw new System.ArgumentException("contains() requires exactly 2 arguments: contains(column,'value')");
        }

        if (node.Args[0] is not FilterIdentifierNode ident)
        {
            throw new System.ArgumentException("contains() first argument must be a column name.");
        }

        if (node.Args[1] is not FilterLiteralNode lit || lit.Kind != FilterTokenKind.String)
        {
            throw new System.ArgumentException("contains() second argument must be a string literal.");
        }

        var prop = ResolveProperty<T>(ident.Name);
        if (prop is null)
        {
            throw new System.ArgumentException($"Unknown filter column: {ident.Name}");
        }

        if (prop.PropertyType != typeof(string))
        {
            throw new System.ArgumentException($"contains() is only supported on string columns. '{ident.Name}' is {prop.PropertyType.Name}.");
        }

        var left = Expression.Property(param, prop);
        var notNull = Expression.NotEqual(left, Expression.Constant(null, typeof(string)));
        var containsMethod = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;
        var call = Expression.Call(left, containsMethod, Expression.Constant(lit.RawValue, typeof(string)));
        return Expression.AndAlso(notNull, call);
    }

    private static Expression BuildComparisonExpression<T>(FilterComparisonNode node, ParameterExpression param)
    {
        var prop = ResolveProperty<T>(node.Column);
        if (prop is null)
        {
            throw new System.ArgumentException($"Unknown filter column: {node.Column}");
        }

        var left = Expression.Property(param, prop);

        if (node.IsNullLiteral)
        {
            var nullConst = Expression.Constant(null, prop.PropertyType);
            return BuildBinary(left, node.Operator, nullConst);
        }

        var coerced = CoerceStringToType(node.RawValue, prop.PropertyType);
        if (coerced is null)
        {
            throw new System.ArgumentException(
                $"Could not parse value '{node.RawValue}' for column '{node.Column}'."
            );
        }

        var underlying = System.Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
        var right = Expression.Constant(coerced, underlying);

        if (System.Nullable.GetUnderlyingType(prop.PropertyType) is not null)
        {
            if (!(node.Operator.Equals("eq", System.StringComparison.OrdinalIgnoreCase) || node.Operator == "="
                  || node.Operator.Equals("ne", System.StringComparison.OrdinalIgnoreCase) || node.Operator == "!="))
            {
                var hasValue = Expression.Property(left, "HasValue");
                var value = Expression.Property(left, "Value");
                var inner = BuildBinary(value, node.Operator, right);
                return Expression.AndAlso(hasValue, inner);
            }
            return BuildBinary(left, node.Operator, Expression.Convert(right, prop.PropertyType));
        }

        return BuildBinary(left, node.Operator, right);
    }

    private enum FilterTokenKind
    {
        Identifier,
        Operator,
        String,
        Number,
        Null,
        And,
        Or,
        LParen,
        RParen,
        Comma,
        End
    }

    private readonly record struct FilterToken(FilterTokenKind Kind, string Value);

    private abstract record FilterNode;

    private sealed record FilterLogicalNode(string Op, FilterNode Left, FilterNode Right) : FilterNode;

    private sealed record FilterComparisonNode(string Column, string Operator, string RawValue, bool IsNullLiteral) : FilterNode;

    private sealed record FilterIdentifierNode(string Name) : FilterNode;

    private sealed record FilterLiteralNode(string RawValue, FilterTokenKind Kind) : FilterNode;

    private sealed record FilterFunctionNode(string Name, List<FilterNode> Args) : FilterNode;

    private sealed class FilterTokenizer
    {
        private readonly string _s;
        private int _i;

        public FilterTokenizer(string s)
        {
            _s = s ?? "";
            _i = 0;
        }

        public List<FilterToken> Tokenize()
        {
            var tokens = new List<FilterToken>();
            while (true)
            {
                SkipWs();
                if (_i >= _s.Length)
                {
                    tokens.Add(new FilterToken(FilterTokenKind.End, ""));
                    return tokens;
                }

                var ch = _s[_i];
                if (ch == '(')
                {
                    _i++;
                    tokens.Add(new FilterToken(FilterTokenKind.LParen, "("));
                    continue;
                }
                if (ch == ')')
                {
                    _i++;
                    tokens.Add(new FilterToken(FilterTokenKind.RParen, ")"));
                    continue;
                }
                if (ch == ',')
                {
                    _i++;
                    tokens.Add(new FilterToken(FilterTokenKind.Comma, ","));
                    continue;
                }

                if (ch == '\'')
                {
                    tokens.Add(new FilterToken(FilterTokenKind.String, ReadQuotedString()));
                    continue;
                }

                // Operators: >= <= != = > <
                if (ch is '>' or '<' or '!' or '=')
                {
                    tokens.Add(new FilterToken(FilterTokenKind.Operator, ReadSymbolOperator()));
                    continue;
                }

                if (IsIdentStart(ch))
                {
                    var ident = ReadIdentifier();
                    var lower = ident.ToLowerInvariant();
                    if (lower == "and") tokens.Add(new FilterToken(FilterTokenKind.And, "and"));
                    else if (lower == "or") tokens.Add(new FilterToken(FilterTokenKind.Or, "or"));
                    else if (lower == "null") tokens.Add(new FilterToken(FilterTokenKind.Null, "null"));
                    else if (IsWordOperator(lower)) tokens.Add(new FilterToken(FilterTokenKind.Operator, lower));
                    else tokens.Add(new FilterToken(FilterTokenKind.Identifier, ident));
                    continue;
                }

                if (char.IsDigit(ch) || (ch == '-' && _i + 1 < _s.Length && char.IsDigit(_s[_i + 1])))
                {
                    tokens.Add(new FilterToken(FilterTokenKind.Number, ReadNumber()));
                    continue;
                }

                throw new System.ArgumentException($"Unexpected character in $filter: '{ch}'");
            }
        }

        private static bool IsWordOperator(string lower) =>
            lower is "eq" or "ne" or "gt" or "ge" or "lt" or "le";

        private static bool IsIdentStart(char ch) =>
            char.IsLetter(ch) || ch == '_' || ch == '$';

        private static bool IsIdentChar(char ch) =>
            char.IsLetterOrDigit(ch) || ch == '_' || ch == '$';

        private void SkipWs()
        {
            while (_i < _s.Length && char.IsWhiteSpace(_s[_i])) _i++;
        }

        private string ReadIdentifier()
        {
            var start = _i;
            while (_i < _s.Length && IsIdentChar(_s[_i])) _i++;
            return _s.Substring(start, _i - start);
        }

        private string ReadNumber()
        {
            var start = _i;
            if (_s[_i] == '-') _i++;
            while (_i < _s.Length && char.IsDigit(_s[_i])) _i++;
            if (_i < _s.Length && _s[_i] == '.')
            {
                _i++;
                while (_i < _s.Length && char.IsDigit(_s[_i])) _i++;
            }
            return _s.Substring(start, _i - start);
        }

        private string ReadSymbolOperator()
        {
            var ch = _s[_i];
            if (_i + 1 < _s.Length)
            {
                var two = _s.Substring(_i, 2);
                if (two is ">=" or "<=" or "!=")
                {
                    _i += 2;
                    return two;
                }
            }
            _i++;
            return ch.ToString();
        }

        private string ReadQuotedString()
        {
            // Reads OData-style single-quoted string with '' escape.
            // Returns the unescaped raw string value (no surrounding quotes).
            _i++; // consume opening '
            var sb = new System.Text.StringBuilder();
            while (_i < _s.Length)
            {
                var ch = _s[_i];
                if (ch == '\'')
                {
                    if (_i + 1 < _s.Length && _s[_i + 1] == '\'')
                    {
                        sb.Append('\'');
                        _i += 2;
                        continue;
                    }
                    _i++; // closing '
                    return sb.ToString();
                }
                sb.Append(ch);
                _i++;
            }
            throw new System.ArgumentException("Unterminated string literal in $filter.");
        }
    }

    private sealed class FilterParser
    {
        private readonly List<FilterToken> _tokens;
        private int _pos;

        public FilterParser(List<FilterToken> tokens)
        {
            _tokens = tokens;
            _pos = 0;
        }

        private FilterToken Peek() => _tokens[_pos];
        private FilterToken Next() => _tokens[_pos++];

        public FilterNode ParseExpression() => ParseOr();

        private FilterNode ParseOr()
        {
            var left = ParseAnd();
            while (Peek().Kind == FilterTokenKind.Or)
            {
                Next();
                var right = ParseAnd();
                left = new FilterLogicalNode("or", left, right);
            }
            return left;
        }

        private FilterNode ParseAnd()
        {
            var left = ParsePrimary();
            while (Peek().Kind == FilterTokenKind.And)
            {
                Next();
                var right = ParsePrimary();
                left = new FilterLogicalNode("and", left, right);
            }
            return left;
        }

        private FilterNode ParsePrimary()
        {
            if (Peek().Kind == FilterTokenKind.LParen)
            {
                Next();
                var inner = ParseExpression();
                if (Peek().Kind != FilterTokenKind.RParen)
                {
                    throw new System.ArgumentException("Missing ')' in $filter.");
                }
                Next();
                return inner;
            }

            return ParseFunctionOrComparison();
        }

        private FilterNode ParseFunctionOrComparison()
        {
            var head = Next();
            if (head.Kind != FilterTokenKind.Identifier)
            {
                throw new System.ArgumentException("Expected column name or function name in $filter.");
            }

            if (Peek().Kind == FilterTokenKind.LParen)
            {
                return ParseFunctionCall(head.Value);
            }

            var op = Next();
            if (op.Kind != FilterTokenKind.Operator)
            {
                throw new System.ArgumentException("Expected operator in $filter.");
            }

            var val = Next();
            if (val.Kind is not (FilterTokenKind.String or FilterTokenKind.Number or FilterTokenKind.Null or FilterTokenKind.Identifier))
            {
                throw new System.ArgumentException("Expected literal value in $filter.");
            }

            if (val.Kind == FilterTokenKind.Null || val.Value.Equals("null", System.StringComparison.OrdinalIgnoreCase))
            {
                return new FilterComparisonNode(head.Value, op.Value, "null", true);
            }

            return new FilterComparisonNode(head.Value, op.Value, val.Value, false);
        }

        private FilterNode ParseFunctionCall(string name)
        {
            Next(); // (
            var args = new List<FilterNode>();

            if (Peek().Kind == FilterTokenKind.RParen)
            {
                Next();
                return new FilterFunctionNode(name, args);
            }

            while (true)
            {
                var t = Peek();
                if (t.Kind == FilterTokenKind.Identifier) args.Add(new FilterIdentifierNode(Next().Value));
                else if (t.Kind == FilterTokenKind.String) args.Add(new FilterLiteralNode(Next().Value, FilterTokenKind.String));
                else if (t.Kind == FilterTokenKind.Number) args.Add(new FilterLiteralNode(Next().Value, FilterTokenKind.Number));
                else if (t.Kind == FilterTokenKind.Null)
                {
                    Next();
                    args.Add(new FilterLiteralNode("null", FilterTokenKind.Null));
                }
                else throw new System.ArgumentException("Expected function argument in $filter.");

                if (Peek().Kind == FilterTokenKind.Comma)
                {
                    Next();
                    continue;
                }
                if (Peek().Kind == FilterTokenKind.RParen)
                {
                    Next();
                    break;
                }
                throw new System.ArgumentException("Expected ',' or ')' in function call.");
            }

            return new FilterFunctionNode(name, args);
        }

        public void ExpectEnd()
        {
            if (Peek().Kind != FilterTokenKind.End)
            {
                throw new System.ArgumentException("Unexpected token at end of $filter.");
            }
        }
    }

    public static IQueryable<T> ApplyOrderByGeneric<T>(string orderBy, IQueryable<T> query)
    {
        if (string.IsNullOrWhiteSpace(orderBy)) return query;
        var parts = orderBy.Split(',', System.StringSplitOptions.TrimEntries | System.StringSplitOptions.RemoveEmptyEntries);

        var isFirst = true;
        foreach (var part in parts)
        {
            var tokens = part.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0) continue;
            var col = tokens[0];
            var desc = tokens.Length > 1 && tokens[1].Equals("desc", System.StringComparison.OrdinalIgnoreCase);

            var prop = ResolveProperty<T>(col);
            if (prop is null)
            {
                throw new System.ArgumentException($"Unknown orderby column: {col}");
            }

            var param = Expression.Parameter(typeof(T), "x");
            var body = Expression.Property(param, prop);
            var lambda = Expression.Lambda(body, param);

            var method = isFirst
                ? (desc ? "OrderByDescending" : "OrderBy")
                : (desc ? "ThenByDescending" : "ThenBy");

            query = (IQueryable<T>)typeof(Queryable).GetMethods()
                .Single(m => m.Name == method && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(T), prop.PropertyType)
                .Invoke(null, new object[] { query, lambda })!;

            isFirst = false;
        }

        return query;
    }

    public static IEnumerable<dynamic> ApplySelectGeneric<T>(string? select, IEnumerable<T> items)
    {
        if (string.IsNullOrWhiteSpace(select)) return items.Cast<dynamic>();

        var cols = select.Split(',', System.StringSplitOptions.TrimEntries | System.StringSplitOptions.RemoveEmptyEntries)
            .Select(c => c.Trim())
            .ToList();

        var props = cols.Select(c => (col: c, prop: ResolveProperty<T>(c))).ToList();
        var bad = props.FirstOrDefault(p => p.prop is null).col;
        if (!string.IsNullOrWhiteSpace(bad))
        {
            throw new System.ArgumentException($"Unknown $select column: {bad}");
        }

        return items.Select(i =>
        {
            var dict = new Dictionary<string, object?>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var (col, prop) in props)
            {
                dict[col] = prop!.GetValue(i);
            }
            return dict;
        });
    }

    public static async Task<IResult> ApplyGroupByGeneric<T>(string apply, IQueryable<T> query)
    {
        // Supports: $apply=groupby((field))
        var match = Regex.Match(apply.Trim(), @"^groupby\(\(\s*([a-zA-Z0-9_]+)\s*\)\)\s*$", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return Results.BadRequest("Unsupported $apply. Only groupby((field)) is supported.");
        }

        var field = match.Groups[1].Value.Trim();
        var prop = ResolveProperty<T>(field);
        if (prop is null)
        {
            return Results.BadRequest($"Unknown groupby column: {field}");
        }

        // Use EF.Property<T> for strong translation, then shape to dictionaries in-memory
        // so JSON uses the requested field name.
        var list = await GroupByToDictAsync(query, prop, field);
        return Results.Ok(list);
    }

    private static Task<List<Dictionary<string, object?>>> GroupByToDictAsync<T>(
        IQueryable<T> query,
        PropertyInfo prop,
        string requestedFieldName)
    {
        var method = typeof(ApiHelpers)
            .GetMethod(nameof(GroupByToDictAsyncImpl), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeof(T), prop.PropertyType);
        return (Task<List<Dictionary<string, object?>>>)method.Invoke(
            null,
            new object[] { query, prop.Name, requestedFieldName }
        )!;
    }

    private static async Task<List<Dictionary<string, object?>>> GroupByToDictAsyncImpl<T, TKey>(
        IQueryable<T> query,
        string propertyName,
        string requestedFieldName)
    {
        var grouped = await query
            .GroupBy(x => EF.Property<TKey>(x!, propertyName))
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .ToListAsync();

        return grouped
            .Select(x => new Dictionary<string, object?>
            {
                [requestedFieldName] = x.Key,
                ["count"] = x.Count
            })
            .ToList();
    }

    public static IQueryable<NycOpenData311CustomerSatisfactionSurvey> ApplyFilter(string filter, IQueryable<NycOpenData311CustomerSatisfactionSurvey> query)
    {
        if (string.IsNullOrWhiteSpace(filter)) return query;
        var parts = filter.Split(" and ", System.StringSplitOptions.TrimEntries | System.StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var tokens = part.Split(" ", System.StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length >= 3)
            {
                var col = tokens[0];
                var op = tokens[1];
                var val = string.Join(" ", tokens.Skip(2)).Trim('\'', '"');
                // Basic equality only
                if (op.Equals("eq", System.StringComparison.OrdinalIgnoreCase) || op == "=")
                {
                    query = col.ToLower() switch
                    {
                        "hbc_unique_key" or "unique_key" => query.Where(x => x.HbcUniqueKey == val),
                        "campaign" => query.Where(x => x.Campaign == val),
                        "channel" => query.Where(x => x.Channel == val),
                        "year" => query.Where(x => x.Year == val),
                        "survey_type" => query.Where(x => x.SurveyType == val),
                        "survey_language" => query.Where(x => x.SurveyLanguage == val),
                        "overall_satisfaction" => query.Where(x => x.OverallSatisfaction == val),
                        "wait_time" => query.Where(x => x.WaitTime == val),
                        "agent_customer_service" => query.Where(x => x.AgentCustomerService == val),
                        "agent_job_knowledge" => query.Where(x => x.AgentJobKnowledge == val),
                        "answer_satisfaction" => query.Where(x => x.AnswerSatisfaction == val),
                        "nps" => int.TryParse(val, out var npsVal) ? query.Where(x => x.Nps == npsVal) : query,
                        "start_time" => DateTime.TryParse(val, out var st) ? query.Where(x => x.StartTime == st) : query,
                        "completion_time" => DateTime.TryParse(val, out var ct) ? query.Where(x => x.CompletionTime == ct) : query,
                        _ => query
                    };
                }
            }
        }
        return query;
    }

    public static IQueryable<NycOpenData311CustomerSatisfactionSurvey> ApplyOrderBy(string orderBy, IQueryable<NycOpenData311CustomerSatisfactionSurvey> query)
    {
        var parts = orderBy.Split(',', System.StringSplitOptions.TrimEntries | System.StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts.Reverse())
        {
            var tokens = part.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            var col = tokens[0];
            var desc = tokens.Length > 1 && tokens[1].Equals("desc", System.StringComparison.OrdinalIgnoreCase);
            query = col.ToLower() switch
            {
                "campaign" => desc ? query.OrderByDescending(x => x.Campaign) : query.OrderBy(x => x.Campaign),
                "channel" => desc ? query.OrderByDescending(x => x.Channel) : query.OrderBy(x => x.Channel),
                "year" => desc ? query.OrderByDescending(x => x.Year) : query.OrderBy(x => x.Year),
                _ => query
            };
        }
        return query;
    }

    public static IEnumerable<dynamic> ApplySelect(string? select, IEnumerable<NycOpenData311CustomerSatisfactionSurvey> items)
    {
        if (string.IsNullOrWhiteSpace(select))
        {
            return items;
        }
        var cols = select.Split(',', System.StringSplitOptions.TrimEntries | System.StringSplitOptions.RemoveEmptyEntries)
            .Select(c => c.ToLower())
            .ToHashSet();
        return items.Select(i => new Dictionary<string, object?>
        {
            ["hbc_unique_key"] = i.HbcUniqueKey,
            ["year"] = i.Year,
            ["campaign"] = i.Campaign,
            ["channel"] = i.Channel,
            ["survey_type"] = i.SurveyType,
            ["start_time"] = i.StartTime,
            ["completion_time"] = i.CompletionTime,
            ["survey_language"] = i.SurveyLanguage,
            ["overall_satisfaction"] = i.OverallSatisfaction,
            ["wait_time"] = i.WaitTime,
            ["agent_customer_service"] = i.AgentCustomerService,
            ["agent_job_knowledge"] = i.AgentJobKnowledge,
            ["answer_satisfaction"] = i.AnswerSatisfaction,
            ["nps"] = i.Nps
        }.Where(kv => cols.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value));
    }

    public static async Task<IResult> ApplyGroupBy(string apply, IQueryable<NycOpenData311CustomerSatisfactionSurvey> query)
    {
        // support $apply=groupby((field))
        var match = Regex.Match(apply, @"groupby\(\(([^)]+)\)\)", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return Results.BadRequest("Invalid $apply syntax");
        }
        var field = match.Groups[1].Value.Trim().ToLower();
        if (field == "campaign")
        {
            var res = await query.GroupBy(x => x.Campaign).Select(g => new { campaign = g.Key, count = g.Count() }).ToListAsync();
            return Results.Ok(res);
        }
        if (field == "year")
        {
            var res = await query.GroupBy(x => x.Year).Select(g => new { year = g.Key, count = g.Count() }).ToListAsync();
            return Results.Ok(res);
        }
        return Results.BadRequest("Unsupported groupby field");
    }

    public static IQueryable<NycOpenData311CallCenterInquiry> ApplyFilterCall(string filter, IQueryable<NycOpenData311CallCenterInquiry> query)
    {
        if (string.IsNullOrWhiteSpace(filter)) return query;
        var parts = filter.Split(" and ", System.StringSplitOptions.TrimEntries | System.StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var tokens = part.Split(" ", System.StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length >= 3)
            {
                var col = tokens[0];
                var op = tokens[1];
                var val = string.Join(" ", tokens.Skip(2)).Trim('\'', '"');
                if (op.Equals("eq", System.StringComparison.OrdinalIgnoreCase) || op == "=")
                {
                    query = col.ToLower() switch
                    {
                        "hbc_unique_key" => query.Where(x => x.HbcUniqueKey == val),
                        "agency" => query.Where(x => x.Agency == val),
                        "agency_name" => query.Where(x => x.AgencyName == val),
                        "inquiry_name" => query.Where(x => x.InquiryName == val),
                        "call_resolution" => query.Where(x => x.CallResolution == val),
                        "time" => query.Where(x => x.Time == val),
                        "date" => DateTime.TryParse(val, out var dt)
                            ? query.Where(x => x.Date.HasValue && x.Date.Value.Date == dt.Date)
                            : query,
                        _ => query
                    };
                }
            }
        }
        return query;
    }

    public static IQueryable<NycOpenData311CallCenterInquiry> ApplyOrderByCall(string orderBy, IQueryable<NycOpenData311CallCenterInquiry> query)
    {
        var parts = orderBy.Split(',', System.StringSplitOptions.TrimEntries | System.StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts.Reverse())
        {
            var tokens = part.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            var col = tokens[0];
            var desc = tokens.Length > 1 && tokens[1].Equals("desc", System.StringComparison.OrdinalIgnoreCase);
            query = col.ToLower() switch
            {
                "agency" => desc ? query.OrderByDescending(x => x.Agency) : query.OrderBy(x => x.Agency),
                "agency_name" => desc ? query.OrderByDescending(x => x.AgencyName) : query.OrderBy(x => x.AgencyName),
                "inquiry_name" => desc ? query.OrderByDescending(x => x.InquiryName) : query.OrderBy(x => x.InquiryName),
                "call_resolution" => desc ? query.OrderByDescending(x => x.CallResolution) : query.OrderBy(x => x.CallResolution),
                "time" => desc ? query.OrderByDescending(x => x.Time) : query.OrderBy(x => x.Time),
                "date" => desc ? query.OrderByDescending(x => x.Date) : query.OrderBy(x => x.Date),
                _ => query
            };
        }
        return query;
    }

    public static IEnumerable<dynamic> ApplySelectCall(string? select, IEnumerable<NycOpenData311CallCenterInquiry> items)
    {
        if (string.IsNullOrWhiteSpace(select))
        {
            return items;
        }
        var cols = select.Split(',', System.StringSplitOptions.TrimEntries | System.StringSplitOptions.RemoveEmptyEntries)
            .Select(c => c.ToLower())
            .ToHashSet();
        return items.Select(i => new Dictionary<string, object?>
        {
            ["hbc_unique_key"] = i.HbcUniqueKey,
            ["unique_id"] = i.UniqueId,
            ["agency"] = i.Agency,
            ["agency_name"] = i.AgencyName,
            ["inquiry_name"] = i.InquiryName,
            ["brief_description"] = i.BriefDescription,
            ["call_resolution"] = i.CallResolution,
            ["date"] = i.Date,
            ["time"] = i.Time,
            ["date_time"] = i.DateTime
        }.Where(kv => cols.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value));
    }

    public static IQueryable<NycOpenData311ServiceRequests> ApplyFilterService(string filter, IQueryable<NycOpenData311ServiceRequests> query)
    {
        if (string.IsNullOrWhiteSpace(filter)) return query;
        var parts = filter.Split(" and ", System.StringSplitOptions.TrimEntries | System.StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var tokens = part.Split(" ", System.StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length >= 3)
            {
                var col = tokens[0];
                var op = tokens[1];
                var val = string.Join(" ", tokens.Skip(2)).Trim('\'', '"');
                if (op.Equals("eq", System.StringComparison.OrdinalIgnoreCase) || op == "=")
                {
                    query = col.ToLower() switch
                    {
                        "hbc_unique_key" or "unique_key" => query.Where(x => x.HbcUniqueKey == val),
                        "agency" => query.Where(x => x.Agency == val),
                        "borough" => query.Where(x => x.Borough == val),
                        "status" => query.Where(x => x.Status == val),
                        "agency_name" => query.Where(x => x.AgencyName == val),
                        "complaint_type" => query.Where(x => x.ComplaintType == val),
                        "descriptor" => query.Where(x => x.Descriptor == val),
                        "incident_zip" => query.Where(x => x.IncidentZip == val),
                        "incident_address" => query.Where(x => x.IncidentAddress == val),
                        "created_date" => DateTime.TryParse(val, out var created) ? query.Where(x => x.CreatedDate == created) : query,
                        "closed_date" => DateTime.TryParse(val, out var closed) ? query.Where(x => x.ClosedDate == closed) : query,
                        _ => query
                    };
                }
            }
        }
        return query;
    }

    public static IQueryable<NycOpenData311ServiceRequests> ApplyOrderByService(string orderBy, IQueryable<NycOpenData311ServiceRequests> query)
    {
        var parts = orderBy.Split(',', System.StringSplitOptions.TrimEntries | System.StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts.Reverse())
        {
            var tokens = part.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            var col = tokens[0];
            var desc = tokens.Length > 1 && tokens[1].Equals("desc", System.StringComparison.OrdinalIgnoreCase);
            query = col.ToLower() switch
            {
                "agency" => desc ? query.OrderByDescending(x => x.Agency) : query.OrderBy(x => x.Agency),
                "borough" => desc ? query.OrderByDescending(x => x.Borough) : query.OrderBy(x => x.Borough),
                "status" => desc ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
                _ => query
            };
        }
        return query;
    }

    public static IEnumerable<dynamic> ApplySelectService(string? select, IEnumerable<NycOpenData311ServiceRequests> items)
    {
        if (string.IsNullOrWhiteSpace(select))
        {
            return items;
        }
        var cols = select.Split(',', System.StringSplitOptions.TrimEntries | System.StringSplitOptions.RemoveEmptyEntries)
            .Select(c => c.ToLower())
            .ToHashSet();
        return items.Select(i => new Dictionary<string, object?>
        {
            ["hbc_unique_key"] = i.HbcUniqueKey,
            ["unique_key"] = i.UniqueKey,
            ["agency"] = i.Agency,
            ["borough"] = i.Borough,
            ["complaint_type"] = i.ComplaintType,
            ["created_date"] = i.CreatedDate,
            ["closed_date"] = i.ClosedDate,
            ["status"] = i.Status,
            ["descriptor"] = i.Descriptor,
            ["latitude"] = i.Latitude,
            ["longitude"] = i.Longitude
        }.Where(kv => cols.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value));
    }

    public static void CopyFields(NycOpenData311CustomerSatisfactionSurvey target, NycOpenData311CustomerSatisfactionSurvey source)
    {
        target.HbcUniqueKey = source.HbcUniqueKey;
        target.Year = source.Year;
        target.Campaign = source.Campaign;
        target.Channel = source.Channel;
        target.SurveyType = source.SurveyType;
        target.StartTime = source.StartTime;
        target.CompletionTime = source.CompletionTime;
        target.SurveyLanguage = source.SurveyLanguage;
        target.OverallSatisfaction = source.OverallSatisfaction;
        target.WaitTime = source.WaitTime;
        target.AgentCustomerService = source.AgentCustomerService;
        target.AgentJobKnowledge = source.AgentJobKnowledge;
        target.AnswerSatisfaction = source.AnswerSatisfaction;
        target.Nps = source.Nps;
    }

    public static void CopyFieldsCall(NycOpenData311CallCenterInquiry target, NycOpenData311CallCenterInquiry source)
    {
        target.HbcUniqueKey = source.HbcUniqueKey;
        target.UniqueId = source.UniqueId;
        target.Agency = source.Agency;
        target.AgencyName = source.AgencyName;
        target.InquiryName = source.InquiryName;
        target.BriefDescription = source.BriefDescription;
        target.Time = source.Time;
        target.Date = source.Date;
        target.DateTime = source.DateTime;
        target.CallResolution = source.CallResolution;
    }

    public static void CopyFieldsService(NycOpenData311ServiceRequests target, NycOpenData311ServiceRequests source)
    {
        target.HbcUniqueKey = source.HbcUniqueKey;
        target.UniqueKey = source.UniqueKey;
        target.ClosedDate = source.ClosedDate;
        target.CreatedDate = source.CreatedDate;
        target.IncidentAddress = source.IncidentAddress;
        target.IncidentZip = source.IncidentZip;
        target.IncidentAddress = source.IncidentAddress;
        target.AddressType = source.AddressType;
        target.Agency = source.Agency;
        target.AgencyName = source.AgencyName;
        target.Borough = source.Borough;
        target.BridgeHighwayDirection = source.BridgeHighwayDirection;
        target.BridgeHighwayName = source.BridgeHighwayName;
        target.BridgeHighwaySegment = source.BridgeHighwaySegment;
        target.City = source.City;
        target.CommunityBoard = source.CommunityBoard;
        target.ComplaintType = source.ComplaintType;
        target.CrossStreet1 = source.CrossStreet1;
        target.CrossStreet2 = source.CrossStreet2;
        target.Descriptor = source.Descriptor;
        target.DueDate = source.DueDate;
        target.FacilityType = source.FacilityType;
        target.FerryDirection = source.FerryDirection;
        target.FerryTerminalName = source.FerryTerminalName;
        target.GarageLotName = source.GarageLotName;
        target.IncidentAddress = source.IncidentAddress;
        target.IncidentZip = source.IncidentZip;
        target.IntersectionStreet1 = source.IntersectionStreet1;
        target.IntersectionStreet2 = source.IntersectionStreet2;
        target.Landmark = source.Landmark;
        target.Latitude = source.Latitude;
        target.Location = source.Location;
        target.LocationType = source.LocationType;
        target.Longitude = source.Longitude;
        target.ParkBorough = source.ParkBorough;
        target.ParkFacilityName = source.ParkFacilityName;
        target.ResolutionActionUpdatedDate = source.ResolutionActionUpdatedDate;
        target.RoadRamp = source.RoadRamp;
        target.SchoolAddress = source.SchoolAddress;
        target.SchoolCity = source.SchoolCity;
        target.SchoolCode = source.SchoolCode;
        target.SchoolName = source.SchoolName;
        target.SchoolNotFound = source.SchoolNotFound;
        target.SchoolNumber = source.SchoolNumber;
        target.SchoolOrCitywideComplaint = source.SchoolOrCitywideComplaint;
        target.SchoolPhoneNumber = source.SchoolPhoneNumber;
        target.SchoolRegion = source.SchoolRegion;
        target.SchoolState = source.SchoolState;
        target.SchoolZip = source.SchoolZip;
        target.Status = source.Status;
        target.StreetName = source.StreetName;
        target.TaxiCompanyBorough = source.TaxiCompanyBorough;
        target.TaxiPickUpLocation = source.TaxiPickUpLocation;
        target.UniqueKey = source.UniqueKey;
        target.VehicleType = source.VehicleType;
        target.XCoordinateStatePlane = source.XCoordinateStatePlane;
        target.YCoordinateStatePlane = source.YCoordinateStatePlane;
    }
}
