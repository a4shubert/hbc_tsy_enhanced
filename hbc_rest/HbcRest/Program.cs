using HbcRest.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Load .env for HBC_DB_PATH if present at repo root
var envFile = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".env"));
var envVars = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
if (File.Exists(envFile))
{
    foreach (var line in File.ReadAllLines(envFile))
    {
        var trimmed = line.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#")) continue;
        var parts = trimmed.Split('=', 2);
        if (parts.Length == 2)
        {
            envVars[parts[0]] = parts[1];
        }
    }
}

var envDbPath = envVars.TryGetValue("HBC_DB_PATH", out var p) ? p : null;
var connString = builder.Configuration.GetConnectionString("HbcSqlite");
if (!string.IsNullOrWhiteSpace(envDbPath))
{
    connString = $"Data Source={envDbPath}";
}
Console.WriteLine($"[HbcRest] Using SQLite connection: {connString}");

// Configure URLs from env if provided
var urlsToUse = envVars.TryGetValue("ASPNETCORE_URLS", out var urls) &&
                !string.IsNullOrWhiteSpace(urls)
    ? urls
    : "http://localhost:5047";
builder.WebHost.UseUrls(urlsToUse);
Console.WriteLine($"[HbcRest] Binding URLs: {urlsToUse}");

builder.Services.AddDbContext<HbcContext>(options =>
    options.UseSqlite(connString));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Expose Swagger UI in all environments for ease of testing.
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/surveys", async (HbcContext db) =>
    await db.CustomerSatisfactionSurveys.AsNoTracking().ToListAsync());

app.MapGet("/surveys/{id}", async (long id, HbcContext db) =>
    await db.CustomerSatisfactionSurveys.FindAsync(id) is { } survey
        ? Results.Ok(survey)
        : Results.NotFound());

app.MapPost("/surveys", async (CustomerSatisfactionSurvey survey, HbcContext db) =>
{
    db.CustomerSatisfactionSurveys.Add(survey);
    var saved = await db.SaveChangesAsync();
    Console.WriteLine($"[HbcRest] POST /surveys saved {saved} row(s)");
    return Results.Created($"/surveys/{survey.Id}", survey);
});

app.MapPost("/surveys/batch", async ([FromBody] List<CustomerSatisfactionSurvey> surveys, HbcContext db) =>
{
    if (surveys is null || surveys.Count == 0) return Results.BadRequest("No surveys provided");

    await db.CustomerSatisfactionSurveys.AddRangeAsync(surveys);
    var saved = await db.SaveChangesAsync();
    Console.WriteLine($"[HbcRest] POST /surveys/batch saved {saved} row(s)");
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

app.MapPut("/surveys/batch", async (IEnumerable<CustomerSatisfactionSurvey> inputs, HbcContext db) =>
{
    if (inputs is null) return Results.BadRequest();

    using var txn = await db.Database.BeginTransactionAsync();
    foreach (var input in inputs)
    {
        if (input.Id != 0)
        {
            var survey = await db.CustomerSatisfactionSurveys.FindAsync(input.Id);
            if (survey is not null)
            {
                CopyFields(survey, input);
                continue;
            }
        }

        db.CustomerSatisfactionSurveys.Add(input);
    }

    var saved = await db.SaveChangesAsync();
    await txn.CommitAsync();
    Console.WriteLine($"[HbcRest] PUT /surveys/batch saved {saved} row(s)");
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
