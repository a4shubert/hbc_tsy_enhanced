using System;
using HbcRest.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

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

// Reduce EF Core verbosity globally.
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Transaction", LogLevel.Warning);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

const string MonikerSurvey = "nyc_open_data_311_customer_satisfaction_survey";
const string MonikerCall = "nyc_open_data_311_call_center_inquiry";
const string MonikerService = "nyc_open_data_311_service_requests";
const int MaxTop = 100;

app.MapGet($"/{MonikerSurvey}", async (
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

    var hasOrder = false;
    if (!string.IsNullOrWhiteSpace(orderBy))
    {
        query = ApplyOrderBy(orderBy, query);
        hasOrder = true;
    }

    var totalCount = count == true ? await query.CountAsync() : (int?)null;

    if (!hasOrder)
    {
        query = query.OrderBy(s => s.Id);
    }

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

app.MapGet($"/{MonikerSurvey}/{{id}}", async (long id, HbcContext db) =>
    await db.CustomerSatisfactionSurveys.FindAsync(id) is { } survey
        ? Results.Ok(survey)
        : Results.NotFound());

app.MapPost($"/{MonikerSurvey}", async (CustomerSatisfactionSurvey survey, HbcContext db) =>
{
    if (!string.IsNullOrWhiteSpace(survey.HbcUniqueKey))
    {
        var existing = await db.CustomerSatisfactionSurveys
            .FirstOrDefaultAsync(s => s.HbcUniqueKey == survey.HbcUniqueKey);
        if (existing is not null)
        {
            CopyFields(existing, survey);
            var savedExisting = await db.SaveChangesAsync();
    Console.WriteLine($"[HbcRest] POST /{MonikerSurvey} updated existing {savedExisting} row(s) by hbc_unique_key");
    return Results.Ok(existing);
        }
    }

    db.CustomerSatisfactionSurveys.Add(survey);
    var saved = await db.SaveChangesAsync();
    Console.WriteLine($"[HbcRest] POST /{MonikerSurvey} saved {saved} row(s)");
    return Results.Created($"/{MonikerSurvey}/{survey.Id}", survey);
});

app.MapPost($"/{MonikerSurvey}/batch", async ([FromBody] List<CustomerSatisfactionSurvey> surveys, HbcContext db) =>
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
    Console.WriteLine($"[HbcRest] POST /{MonikerSurvey}/batch saved {saved} row(s), updated {updated} existing");
    return Results.Ok(surveys);
});

app.MapPut($"/{MonikerSurvey}/{{id}}", async (long id, CustomerSatisfactionSurvey input, HbcContext db) =>
{
    var survey = await db.CustomerSatisfactionSurveys.FindAsync(id);

    if (survey is null) return Results.NotFound();

    CopyFields(survey, input);
    var saved = await db.SaveChangesAsync();
    Console.WriteLine($"[HbcRest] PUT /{MonikerSurvey}/{id} saved {saved} row(s)");
    return Results.NoContent();
});

app.MapDelete($"/{MonikerSurvey}/{{id}}", async (long id, HbcContext db) =>
{
    var survey = await db.CustomerSatisfactionSurveys.FindAsync(id);
    if (survey is null) return Results.NotFound();
    db.CustomerSatisfactionSurveys.Remove(survey);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapGet($"/{MonikerCall}", async (
    [FromQuery(Name = "$top")] long? top,
    [FromQuery(Name = "$filter")] string? filter,
    [FromQuery(Name = "$orderby")] string? orderBy,
    [FromQuery(Name = "$skip")] int? skip,
    [FromQuery(Name = "$count")] bool? count,
    [FromQuery(Name = "$select")] string? select,
    HbcContext db) =>
{
    IQueryable<CallCenterInquiry> query = db.CallCenterInquiries.AsQueryable();

    if (!string.IsNullOrWhiteSpace(filter))
    {
        query = ApplyFilterCall(filter, query);
    }

    var hasOrder = false;
    if (!string.IsNullOrWhiteSpace(orderBy))
    {
        query = ApplyOrderByCall(orderBy, query);
        hasOrder = true;
    }

    var totalCount = count == true ? await query.CountAsync() : (int?)null;

    if (!hasOrder)
    {
        query = query.OrderBy(s => s.Id);
    }

    if (skip.HasValue && skip.Value > 0)
    {
        query = query.Skip(skip.Value);
    }

    if (top.HasValue)
    {
        if (top.Value > 0)
        {
            var take = (int)Math.Min(top.Value, MaxTop);
            query = query.Take(take);
        }
    }
    else if (string.IsNullOrWhiteSpace(filter) && count != true)
    {
        query = query.Take(10);
    }

    var results = await query.AsNoTracking().ToListAsync();
    var projected = ApplySelectCall(select, results);
    if (totalCount.HasValue)
    {
        return Results.Ok(new { count = totalCount.Value, value = projected });
    }
    return Results.Ok(projected);
});

app.MapPost($"/{MonikerCall}", async (CallCenterInquiry input, HbcContext db) =>
{
    if (!string.IsNullOrWhiteSpace(input.HbcUniqueKey))
    {
        var existing = await db.CallCenterInquiries
            .FirstOrDefaultAsync(s => s.HbcUniqueKey == input.HbcUniqueKey);
        if (existing is not null)
        {
            CopyFieldsCall(existing, input);
            var savedExisting = await db.SaveChangesAsync();
            Console.WriteLine($"[HbcRest] POST /{MonikerCall} updated existing {savedExisting} row(s) by hbc_unique_key");
            return Results.Ok(existing);
        }
    }

    db.CallCenterInquiries.Add(input);
    var saved = await db.SaveChangesAsync();
    Console.WriteLine($"[HbcRest] POST /{MonikerCall} saved {saved} row(s)");
    return Results.Created($"/{MonikerCall}/{input.Id}", input);
});

app.MapPost($"/{MonikerCall}/batch", async ([FromBody] List<CallCenterInquiry> inputs, HbcContext db) =>
{
    if (inputs is null || inputs.Count == 0) return Results.BadRequest("No call center inquiries provided");

    int updated = 0;
    var toInsert = new List<CallCenterInquiry>();

    foreach (var input in inputs)
    {
        if (!string.IsNullOrWhiteSpace(input.HbcUniqueKey))
        {
            var existing = await db.CallCenterInquiries
                .FirstOrDefaultAsync(s => s.HbcUniqueKey == input.HbcUniqueKey);
            if (existing is not null)
            {
                CopyFieldsCall(existing, input);
                updated++;
                continue;
            }
        }
        toInsert.Add(input);
    }

    if (toInsert.Count > 0)
    {
        await db.CallCenterInquiries.AddRangeAsync(toInsert);
    }

    var saved = await db.SaveChangesAsync();
    Console.WriteLine($"[HbcRest] POST /{MonikerCall}/batch saved {saved} row(s), updated {updated} existing");
    return Results.Ok(inputs);
});

app.MapGet($"/{MonikerService}", async (
    [FromQuery(Name = "$top")] long? top,
    [FromQuery(Name = "$filter")] string? filter,
    [FromQuery(Name = "$apply")] string? apply,
    [FromQuery(Name = "$orderby")] string? orderBy,
    [FromQuery(Name = "$skip")] int? skip,
    [FromQuery(Name = "$count")] bool? count,
    [FromQuery(Name = "$select")] string? select,
    HbcContext db) =>
{
    IQueryable<ServiceRequest> query = db.ServiceRequests.AsQueryable();

    if (!string.IsNullOrWhiteSpace(filter))
    {
        query = ApplyFilterService(filter, query);
    }

    if (!string.IsNullOrWhiteSpace(apply))
    {
        return Results.BadRequest("$apply is not supported for service requests");
    }

    var hasOrder = false;
    if (!string.IsNullOrWhiteSpace(orderBy))
    {
        query = ApplyOrderByService(orderBy, query);
        hasOrder = true;
    }

    var totalCount = count == true ? await query.CountAsync() : (int?)null;

    if (!hasOrder)
    {
        query = query.OrderBy(s => s.Id);
    }

    if (skip.HasValue && skip.Value > 0)
    {
        query = query.Skip(skip.Value);
    }

    if (top.HasValue)
    {
        if (top.Value > 0)
        {
            var take = (int)Math.Min(top.Value, MaxTop);
            query = query.Take(take);
        }
    }
    else if (string.IsNullOrWhiteSpace(filter) && count != true)
    {
        query = query.Take(10);
    }

    var results = await query.AsNoTracking().ToListAsync();
    var projected = ApplySelectService(select, results);
    if (totalCount.HasValue)
    {
        return Results.Ok(new { count = totalCount.Value, value = projected });
    }
    return Results.Ok(projected);
});

app.MapPost($"/{MonikerService}", async (ServiceRequest input, HbcContext db) =>
{
    if (!string.IsNullOrWhiteSpace(input.HbcUniqueKey))
    {
        var existing = await db.ServiceRequests
            .FirstOrDefaultAsync(s => s.HbcUniqueKey == input.HbcUniqueKey);
        if (existing is not null)
        {
            CopyFieldsService(existing, input);
            var savedExisting = await db.SaveChangesAsync();
            Console.WriteLine($"[HbcRest] POST /{MonikerService} updated existing {savedExisting} row(s) by hbc_unique_key");
            return Results.Ok(existing);
        }
    }

    db.ServiceRequests.Add(input);
    var saved = await db.SaveChangesAsync();
    Console.WriteLine($"[HbcRest] POST /{MonikerService} saved {saved} row(s)");
    return Results.Created($"/{MonikerService}/{input.Id}", input);
});

app.MapPost($"/{MonikerService}/batch", async ([FromBody] List<ServiceRequest> inputs, HbcContext db) =>
{
    if (inputs is null || inputs.Count == 0) return Results.BadRequest("No service requests provided");

    int updated = 0;
    var toInsert = new List<ServiceRequest>();

    foreach (var input in inputs)
    {
        if (!string.IsNullOrWhiteSpace(input.HbcUniqueKey))
        {
            var existing = await db.ServiceRequests
                .FirstOrDefaultAsync(s => s.HbcUniqueKey == input.HbcUniqueKey);
            if (existing is not null)
            {
                CopyFieldsService(existing, input);
                updated++;
                continue;
            }
        }
        toInsert.Add(input);
    }

    if (toInsert.Count > 0)
    {
        await db.ServiceRequests.AddRangeAsync(toInsert);
    }

    var saved = await db.SaveChangesAsync();
    Console.WriteLine($"[HbcRest] POST /{MonikerService}/batch saved {saved} row(s), updated {updated} existing");
    return Results.Ok(inputs);
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

static IQueryable<CallCenterInquiry> ApplyFilterCall(string filter, IQueryable<CallCenterInquiry> query)
{
    var parts = filter.Split(" eq ", 2, StringSplitOptions.TrimEntries);
    if (parts.Length != 2) return query;
    var field = parts[0].Trim().ToLowerInvariant();
    var value = parts[1].Trim().Trim('\'', '"');

    return field switch
    {
        "unique_id" => query.Where(s => s.UniqueId == value),
        "agency" => query.Where(s => s.Agency == value),
        "agency_name" => query.Where(s => s.AgencyName == value),
        "inquiry_name" => query.Where(s => s.InquiryName == value),
        "brief_description" => query.Where(s => s.BriefDescription == value),
        "call_resolution" => query.Where(s => s.CallResolution == value),
        "hbc_unique_key" => query.Where(s => s.HbcUniqueKey == value),
        "date" => DateTime.TryParse(value, out var dt) ? query.Where(s => s.Date == dt) : query,
        "date_time" => DateTime.TryParse(value, out var dt2) ? query.Where(s => s.DateTime == dt2) : query,
        _ => query
    };
}

static IQueryable<CallCenterInquiry> ApplyOrderByCall(string orderBy, IQueryable<CallCenterInquiry> query)
{
    var parts = orderBy.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length == 0) return query;
    var field = parts[0].Trim().ToLowerInvariant();
    var desc = parts.Length > 1 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);

    return field switch
    {
        "date" => desc ? query.OrderByDescending(s => s.Date) : query.OrderBy(s => s.Date),
        "date_time" => desc ? query.OrderByDescending(s => s.DateTime) : query.OrderBy(s => s.DateTime),
        "agency" => desc ? query.OrderByDescending(s => s.Agency) : query.OrderBy(s => s.Agency),
        "inquiry_name" => desc ? query.OrderByDescending(s => s.InquiryName) : query.OrderBy(s => s.InquiryName),
        "hbc_unique_key" => desc ? query.OrderByDescending(s => s.HbcUniqueKey) : query.OrderBy(s => s.HbcUniqueKey),
        _ => query
    };
}

static IEnumerable<object> ApplySelectCall(string? select, IEnumerable<CallCenterInquiry> source)
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
        if (lowerFields.Contains("unique_id")) dict["unique_id"] = s.UniqueId;
        if (lowerFields.Contains("date")) dict["date"] = s.Date;
        if (lowerFields.Contains("time")) dict["time"] = s.Time;
        if (lowerFields.Contains("date_time")) dict["date_time"] = s.DateTime;
        if (lowerFields.Contains("agency")) dict["agency"] = s.Agency;
        if (lowerFields.Contains("agency_name")) dict["agency_name"] = s.AgencyName;
        if (lowerFields.Contains("inquiry_name")) dict["inquiry_name"] = s.InquiryName;
        if (lowerFields.Contains("brief_description")) dict["brief_description"] = s.BriefDescription;
        if (lowerFields.Contains("call_resolution")) dict["call_resolution"] = s.CallResolution;
        return dict;
    });
}

static void CopyFieldsCall(CallCenterInquiry target, CallCenterInquiry source)
{
    target.HbcUniqueKey = source.HbcUniqueKey;
    target.UniqueId = source.UniqueId;
    target.Date = source.Date;
    target.Time = source.Time;
    target.DateTime = source.DateTime;
    target.Agency = source.Agency;
    target.AgencyName = source.AgencyName;
    target.InquiryName = source.InquiryName;
    target.BriefDescription = source.BriefDescription;
    target.CallResolution = source.CallResolution;
}

static IQueryable<ServiceRequest> ApplyFilterService(string filter, IQueryable<ServiceRequest> query)
{
    var parts = filter.Split(" eq ", 2, StringSplitOptions.TrimEntries);
    if (parts.Length != 2) return query;
    var field = parts[0].Trim().ToLowerInvariant();
    var value = parts[1].Trim().Trim('\'', '"');

    return field switch
    {
        "unique_key" => query.Where(s => s.UniqueKey == value),
        "agency" => query.Where(s => s.Agency == value),
        "agency_name" => query.Where(s => s.AgencyName == value),
        "borough" => query.Where(s => s.Borough == value),
        "complaint_type" => query.Where(s => s.ComplaintType == value),
        "descriptor" => query.Where(s => s.Descriptor == value),
        "status" => query.Where(s => s.Status == value),
        "incident_zip" => query.Where(s => s.IncidentZip == value),
        "incident_address" => query.Where(s => s.IncidentAddress == value),
        "created_date" => DateTime.TryParse(value, out var created) ? query.Where(s => s.CreatedDate == created) : query,
        "closed_date" => DateTime.TryParse(value, out var closed) ? query.Where(s => s.ClosedDate == closed) : query,
        "hbc_unique_key" => query.Where(s => s.HbcUniqueKey == value),
        _ => query
    };
}

static IQueryable<ServiceRequest> ApplyOrderByService(string orderBy, IQueryable<ServiceRequest> query)
{
    var parts = orderBy.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length == 0) return query;
    var field = parts[0].Trim().ToLowerInvariant();
    var desc = parts.Length > 1 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);

    return field switch
    {
        "created_date" => desc ? query.OrderByDescending(s => s.CreatedDate) : query.OrderBy(s => s.CreatedDate),
        "closed_date" => desc ? query.OrderByDescending(s => s.ClosedDate) : query.OrderBy(s => s.ClosedDate),
        "agency" => desc ? query.OrderByDescending(s => s.Agency) : query.OrderBy(s => s.Agency),
        "borough" => desc ? query.OrderByDescending(s => s.Borough) : query.OrderBy(s => s.Borough),
        "complaint_type" => desc ? query.OrderByDescending(s => s.ComplaintType) : query.OrderBy(s => s.ComplaintType),
        "descriptor" => desc ? query.OrderByDescending(s => s.Descriptor) : query.OrderBy(s => s.Descriptor),
        "status" => desc ? query.OrderByDescending(s => s.Status) : query.OrderBy(s => s.Status),
        "hbc_unique_key" => desc ? query.OrderByDescending(s => s.HbcUniqueKey) : query.OrderBy(s => s.HbcUniqueKey),
        _ => query
    };
}

static IEnumerable<object> ApplySelectService(string? select, IEnumerable<ServiceRequest> source)
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
        if (lowerFields.Contains("unique_key")) dict["unique_key"] = s.UniqueKey;
        if (lowerFields.Contains("agency")) dict["agency"] = s.Agency;
        if (lowerFields.Contains("agency_name")) dict["agency_name"] = s.AgencyName;
        if (lowerFields.Contains("borough")) dict["borough"] = s.Borough;
        if (lowerFields.Contains("complaint_type")) dict["complaint_type"] = s.ComplaintType;
        if (lowerFields.Contains("descriptor")) dict["descriptor"] = s.Descriptor;
        if (lowerFields.Contains("status")) dict["status"] = s.Status;
        if (lowerFields.Contains("created_date")) dict["created_date"] = s.CreatedDate;
        if (lowerFields.Contains("closed_date")) dict["closed_date"] = s.ClosedDate;
        if (lowerFields.Contains("incident_address")) dict["incident_address"] = s.IncidentAddress;
        if (lowerFields.Contains("incident_zip")) dict["incident_zip"] = s.IncidentZip;
        if (lowerFields.Contains("city")) dict["city"] = s.City;
        if (lowerFields.Contains("latitude")) dict["latitude"] = s.Latitude;
        if (lowerFields.Contains("longitude")) dict["longitude"] = s.Longitude;
        if (lowerFields.Contains("location")) dict["location"] = s.Location;
        return dict;
    });
}

static void CopyFieldsService(ServiceRequest target, ServiceRequest source)
{
    target.HbcUniqueKey = source.HbcUniqueKey;
    target.AddressType = source.AddressType;
    target.Agency = source.Agency;
    target.AgencyName = source.AgencyName;
    target.Borough = source.Borough;
    target.BridgeHighwayDirection = source.BridgeHighwayDirection;
    target.BridgeHighwayName = source.BridgeHighwayName;
    target.BridgeHighwaySegment = source.BridgeHighwaySegment;
    target.City = source.City;
    target.ClosedDate = source.ClosedDate;
    target.CommunityBoard = source.CommunityBoard;
    target.ComplaintType = source.ComplaintType;
    target.CreatedDate = source.CreatedDate;
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
