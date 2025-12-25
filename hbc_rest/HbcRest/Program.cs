using System;
using HbcRest.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

var envDbPath = Environment.GetEnvironmentVariable("HBC_DB_PATH");

var connString = builder.Configuration.GetConnectionString("HbcSqlite");
if (!string.IsNullOrWhiteSpace(envDbPath))
{
    var absPath = Path.GetFullPath(envDbPath);
    connString = $"Data Source={absPath}";
}
Console.WriteLine($"[HbcRest] Using SQLite connection: {connString}");

// Configure URLs from env if provided
var urlsToUse = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
if (string.IsNullOrWhiteSpace(urlsToUse))
{
    urlsToUse = "http://localhost:5047";
}
builder.WebHost.UseUrls(urlsToUse);
Console.WriteLine($"[HbcRest] Binding URLs: {urlsToUse}");

builder.Services.AddDbContext<HbcContext>(options =>
    options.UseSqlite(connString));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

const string Moniker = "nyc_open_data_311_customer_satisfaction_survey";
const int MaxTop = 100;

app.MapGet($"/{Moniker}", async (
    [FromQuery(Name = "$top")] long? top,
    [FromQuery(Name = "$filter")] string? filter,
    [FromQuery(Name = "$apply")] string? apply,
    [FromQuery(Name = "$orderby")] string? orderBy,
    [FromQuery(Name = "$skip")] int? skip,
    [FromQuery(Name = "$count")] bool? count,
    [FromQuery(Name = "$select")] string? select,
    [FromQuery(Name = "$expand")] string? expand,
    HbcContext db) =>
{
    IQueryable<CustomerSatisfactionSurvey> query = db.CustomerSatisfactionSurveys.AsQueryable();

    if (!string.IsNullOrWhiteSpace(filter))
    {
        query = ApplyFilter(filter, query);
    }

    if (!string.IsNullOrWhiteSpace(apply))
    {
        return await ApplyGroupBy(apply, query);
    }

    if (!string.IsNullOrWhiteSpace(expand))
    {
        return Results.BadRequest("$expand is not supported.");
    }

    if (!string.IsNullOrWhiteSpace(orderBy))
    {
        query = ApplyOrderBy(orderBy, query);
    }

    var totalCount = count == true ? await query.CountAsync() : (int?)null;

    if (skip.HasValue && skip.Value > 0)
    {
        query = query.Skip(skip.Value);
    }

    // If $top is specified:
    //   >0 => clamp to MaxTop; <=0 => no limit
    // If $top is not specified:
    //   apply default 10 only when no filter; otherwise no limit.
    if (top.HasValue)
    {
        if (top.Value > 0)
        {
            var take = (int)Math.Min(top.Value, MaxTop);
            query = query.Take(take);
        }
        // top <= 0 means no limit
    }
    else
    {
        if (string.IsNullOrWhiteSpace(filter) && count != true)
        {
            query = query.Take(10); // default limit when not specified and no filter
        }
    }

    var results = await query.AsNoTracking().ToListAsync();
    var projected = ApplySelect(select, results);
    if (totalCount.HasValue)
    {
        return Results.Ok(new { count = totalCount.Value, value = projected });
    }
    return Results.Ok(projected);
});

app.MapGet($"/{Moniker}/{{id}}", async (long id, HbcContext db) =>
    await db.CustomerSatisfactionSurveys.FindAsync(id) is { } survey
        ? Results.Ok(survey)
        : Results.NotFound());

app.MapPost($"/{Moniker}", async (CustomerSatisfactionSurvey survey, HbcContext db) =>
{
    if (!string.IsNullOrWhiteSpace(survey.HbcUniqueKey))
    {
        var existing = await db.CustomerSatisfactionSurveys
            .FirstOrDefaultAsync(s => s.HbcUniqueKey == survey.HbcUniqueKey);
        if (existing is not null)
        {
            CopyFields(existing, survey);
            var savedExisting = await db.SaveChangesAsync();
            Console.WriteLine($"[HbcRest] POST /{Moniker} updated existing {savedExisting} row(s) by hbc_unique_key");
            return Results.Ok(existing);
        }
    }

    db.CustomerSatisfactionSurveys.Add(survey);
    var saved = await db.SaveChangesAsync();
    Console.WriteLine($"[HbcRest] POST /{Moniker} saved {saved} row(s)");
    return Results.Created($"/{Moniker}/{survey.Id}", survey);
});

app.MapPost($"/{Moniker}/batch", async ([FromBody] List<CustomerSatisfactionSurvey> surveys, HbcContext db) =>
{
    if (surveys is null || surveys.Count == 0) return Results.BadRequest("No surveys provided");

    int updated = 0;
    var toInsert = new List<CustomerSatisfactionSurvey>();

    foreach (var survey in surveys)
    {
        if (!string.IsNullOrWhiteSpace(survey.HbcUniqueKey))
        {
            var existing = await db.CustomerSatisfactionSurveys
                .FirstOrDefaultAsync(s => s.HbcUniqueKey == survey.HbcUniqueKey);
            if (existing is not null)
            {
                CopyFields(existing, survey);
                updated++;
                continue;
            }
        }
        toInsert.Add(survey);
    }

    if (toInsert.Count > 0)
    {
        await db.CustomerSatisfactionSurveys.AddRangeAsync(toInsert);
    }

    var saved = await db.SaveChangesAsync();
    Console.WriteLine($"[HbcRest] POST /{Moniker}/batch saved {saved} row(s), updated {updated} existing");
    return Results.Ok(surveys);
});

app.MapPut($"/{Moniker}/{{id}}", async (long id, CustomerSatisfactionSurvey input, HbcContext db) =>
{
    var survey = await db.CustomerSatisfactionSurveys.FindAsync(id);

    if (survey is null) return Results.NotFound();

    CopyFields(survey, input);
    var saved = await db.SaveChangesAsync();
    Console.WriteLine($"[HbcRest] PUT /{Moniker}/{id} saved {saved} row(s)");
    return Results.NoContent();
});

app.MapDelete($"/{Moniker}/{{id}}", async (long id, HbcContext db) =>
{
    var survey = await db.CustomerSatisfactionSurveys.FindAsync(id);
    if (survey is null) return Results.NotFound();
    db.CustomerSatisfactionSurveys.Remove(survey);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();

static void CopyFields(CustomerSatisfactionSurvey target, CustomerSatisfactionSurvey source)
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

static IQueryable<CustomerSatisfactionSurvey> ApplyFilter(
    string filter,
    IQueryable<CustomerSatisfactionSurvey> query)
{
    // Very small OData-like filter support: "<field> eq '<value>'"
    var parts = filter.Split(" eq ", 2, StringSplitOptions.TrimEntries);
    if (parts.Length != 2) return query;

    var field = parts[0].Trim().ToLowerInvariant();
    var value = parts[1].Trim().Trim('\'', '"');

    return field switch
    {
        "campaign" => query.Where(s => s.Campaign == value),
        "channel" => query.Where(s => s.Channel == value),
        "survey_type" => query.Where(s => s.SurveyType == value),
        "survey_language" => query.Where(s => s.SurveyLanguage == value),
        "wait_time" => query.Where(s => s.WaitTime == value),
        "overall_satisfaction" => query.Where(s => s.OverallSatisfaction == value),
        "agent_customer_service" => query.Where(s => s.AgentCustomerService == value),
        "agent_job_knowledge" => query.Where(s => s.AgentJobKnowledge == value),
        "answer_satisfaction" => query.Where(s => s.AnswerSatisfaction == value),
        "year" => query.Where(s => s.Year == value),
        "unique_key" or "hbc_unique_key" => query.Where(s => s.HbcUniqueKey == value),
        "nps" => int.TryParse(value, out var npsVal) ? query.Where(s => s.Nps == npsVal) : query,
        _ => query
    };
}

static async Task<IResult> ApplyGroupBy(
    string apply,
    IQueryable<CustomerSatisfactionSurvey> query)
{
    var match = Regex.Match(apply, @"groupby\(\(([^)]+)\)\)", RegexOptions.IgnoreCase);
    if (!match.Success) return Results.BadRequest("Unsupported $apply expression");
    var col = match.Groups[1].Value.Trim().ToLowerInvariant();

    switch (col)
    {
        case "start_time":
            return Results.Ok(await query.GroupBy(s => s.StartTime)
                .Select(g => new { start_time = g.Key })
                .ToListAsync());
        case "campaign":
            return Results.Ok(await query.GroupBy(s => s.Campaign)
                .Select(g => new { campaign = g.Key })
                .ToListAsync());
        case "channel":
            return Results.Ok(await query.GroupBy(s => s.Channel)
                .Select(g => new { channel = g.Key })
                .ToListAsync());
        case "survey_type":
            return Results.Ok(await query.GroupBy(s => s.SurveyType)
                .Select(g => new { survey_type = g.Key })
                .ToListAsync());
        case "survey_language":
            return Results.Ok(await query.GroupBy(s => s.SurveyLanguage)
                .Select(g => new { survey_language = g.Key })
                .ToListAsync());
        case "year":
            return Results.Ok(await query.GroupBy(s => s.Year)
                .Select(g => new { year = g.Key })
                .ToListAsync());
        default:
            return Results.BadRequest($"Unsupported groupby column: {col}");
    }
}

static IQueryable<CustomerSatisfactionSurvey> ApplyOrderBy(
    string orderBy,
    IQueryable<CustomerSatisfactionSurvey> query)
{
    var parts = orderBy.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length == 0) return query;
    var field = parts[0].Trim().ToLowerInvariant();
    var desc = parts.Length > 1 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);

    return field switch
    {
        "campaign" => desc ? query.OrderByDescending(s => s.Campaign) : query.OrderBy(s => s.Campaign),
        "channel" => desc ? query.OrderByDescending(s => s.Channel) : query.OrderBy(s => s.Channel),
        "start_time" => desc ? query.OrderByDescending(s => s.StartTime) : query.OrderBy(s => s.StartTime),
        "completion_time" => desc ? query.OrderByDescending(s => s.CompletionTime) : query.OrderBy(s => s.CompletionTime),
        "survey_language" => desc ? query.OrderByDescending(s => s.SurveyLanguage) : query.OrderBy(s => s.SurveyLanguage),
        "survey_type" => desc ? query.OrderByDescending(s => s.SurveyType) : query.OrderBy(s => s.SurveyType),
        "wait_time" => desc ? query.OrderByDescending(s => s.WaitTime) : query.OrderBy(s => s.WaitTime),
        "overall_satisfaction" => desc ? query.OrderByDescending(s => s.OverallSatisfaction) : query.OrderBy(s => s.OverallSatisfaction),
        "agent_customer_service" => desc ? query.OrderByDescending(s => s.AgentCustomerService) : query.OrderBy(s => s.AgentCustomerService),
        "agent_job_knowledge" => desc ? query.OrderByDescending(s => s.AgentJobKnowledge) : query.OrderBy(s => s.AgentJobKnowledge),
        "answer_satisfaction" => desc ? query.OrderByDescending(s => s.AnswerSatisfaction) : query.OrderBy(s => s.AnswerSatisfaction),
        "year" => desc ? query.OrderByDescending(s => s.Year) : query.OrderBy(s => s.Year),
        "nps" => desc ? query.OrderByDescending(s => s.Nps) : query.OrderBy(s => s.Nps),
        "hbc_unique_key" => desc ? query.OrderByDescending(s => s.HbcUniqueKey) : query.OrderBy(s => s.HbcUniqueKey),
        _ => query
    };
}

static IEnumerable<object> ApplySelect(string? select, IEnumerable<CustomerSatisfactionSurvey> source)
{
    if (string.IsNullOrWhiteSpace(select)) return source;

    var fields = select.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (fields.Length == 0) return source;

    var lowerFields = new HashSet<string>(fields.Select(f => f.ToLowerInvariant()));

    return source.Select(s =>
    {
        var dict = new Dictionary<string, object?>();
        if (lowerFields.Contains("id")) dict["id"] = s.Id;
        if (lowerFields.Contains("hbc_unique_key")) dict["hbc_unique_key"] = s.HbcUniqueKey;
        if (lowerFields.Contains("year")) dict["year"] = s.Year;
        if (lowerFields.Contains("campaign")) dict["campaign"] = s.Campaign;
        if (lowerFields.Contains("channel")) dict["channel"] = s.Channel;
        if (lowerFields.Contains("survey_type")) dict["survey_type"] = s.SurveyType;
        if (lowerFields.Contains("start_time")) dict["start_time"] = s.StartTime;
        if (lowerFields.Contains("completion_time")) dict["completion_time"] = s.CompletionTime;
        if (lowerFields.Contains("survey_language")) dict["survey_language"] = s.SurveyLanguage;
        if (lowerFields.Contains("overall_satisfaction")) dict["overall_satisfaction"] = s.OverallSatisfaction;
        if (lowerFields.Contains("wait_time")) dict["wait_time"] = s.WaitTime;
        if (lowerFields.Contains("agent_customer_service")) dict["agent_customer_service"] = s.AgentCustomerService;
        if (lowerFields.Contains("agent_job_knowledge")) dict["agent_job_knowledge"] = s.AgentJobKnowledge;
        if (lowerFields.Contains("answer_satisfaction")) dict["answer_satisfaction"] = s.AnswerSatisfaction;
        if (lowerFields.Contains("nps")) dict["nps"] = s.Nps;
        return dict;
    });
}
