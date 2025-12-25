using System;
using HbcRest.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

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

app.MapGet("/surveys", async (HbcContext db) =>
    await db.CustomerSatisfactionSurveys.AsNoTracking().ToListAsync());

app.MapGet("/surveys/{id}", async (long id, HbcContext db) =>
    await db.CustomerSatisfactionSurveys.FindAsync(id) is { } survey
        ? Results.Ok(survey)
        : Results.NotFound());

app.MapPost("/surveys", async (CustomerSatisfactionSurvey survey, HbcContext db) =>
{
    if (!string.IsNullOrWhiteSpace(survey.UniqueKey))
    {
        var existing = await db.CustomerSatisfactionSurveys
            .FirstOrDefaultAsync(s => s.UniqueKey == survey.UniqueKey);
        if (existing is not null)
        {
            CopyFields(existing, survey);
            var savedExisting = await db.SaveChangesAsync();
            Console.WriteLine($"[HbcRest] POST /surveys updated existing {savedExisting} row(s) by unique_key");
            return Results.Ok(existing);
        }
    }

    db.CustomerSatisfactionSurveys.Add(survey);
    var saved = await db.SaveChangesAsync();
    Console.WriteLine($"[HbcRest] POST /surveys saved {saved} row(s)");
    return Results.Created($"/surveys/{survey.Id}", survey);
});

app.MapPost("/surveys/batch", async ([FromBody] List<CustomerSatisfactionSurvey> surveys, HbcContext db) =>
{
    if (surveys is null || surveys.Count == 0) return Results.BadRequest("No surveys provided");

    int updated = 0;
    var toInsert = new List<CustomerSatisfactionSurvey>();

    foreach (var survey in surveys)
    {
        if (!string.IsNullOrWhiteSpace(survey.UniqueKey))
        {
            var existing = await db.CustomerSatisfactionSurveys
                .FirstOrDefaultAsync(s => s.UniqueKey == survey.UniqueKey);
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
    Console.WriteLine($"[HbcRest] POST /surveys/batch saved {saved} row(s), updated {updated} existing");
    return Results.Ok(surveys);
});

app.MapPut("/surveys/{id}", async (long id, CustomerSatisfactionSurvey input, HbcContext db) =>
{
    var survey = await db.CustomerSatisfactionSurveys.FindAsync(id);

    if (survey is null) return Results.NotFound();

    CopyFields(survey, input);
    var saved = await db.SaveChangesAsync();
    Console.WriteLine($"[HbcRest] PUT /surveys/{id} saved {saved} row(s)");
    return Results.NoContent();
});

app.MapDelete("/surveys/{id}", async (long id, HbcContext db) =>
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
