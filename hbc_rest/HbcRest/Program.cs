using System;
using HbcRest.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

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
const int MaxTop = 100000;

app.MapGet($"/{Moniker}", async (
    [FromQuery(Name = "$top")] long? top,
    [FromQuery(Name = "$filter")] string? filter,
    HbcContext db) =>
{
    IQueryable<CustomerSatisfactionSurvey> query = db.CustomerSatisfactionSurveys.AsQueryable();

    if (!string.IsNullOrWhiteSpace(filter))
    {
        query = ApplyFilter(filter, query);
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
        if (string.IsNullOrWhiteSpace(filter))
        {
            query = query.Take(10); // default limit when not specified and no filter
        }
    }

    var results = await query.AsNoTracking().ToListAsync();
    return Results.Ok(results);
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
