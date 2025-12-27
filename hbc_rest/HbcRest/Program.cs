using System;
using HbcRest.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Connections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static HbcRest.ApiHelpers;

var builder = WebApplication.CreateBuilder(args);

var envDbPath = Environment.GetEnvironmentVariable("HBC_DB_PATH");

var connString = builder.Configuration.GetConnectionString("HbcSqlite");
if (!string.IsNullOrWhiteSpace(envDbPath))
{
    var absPath = Path.GetFullPath(envDbPath);
    connString = $"Data Source={absPath}";
}
Console.WriteLine($"[HbcRest] Using SQLite connection: {connString}");

// Configure URLs strictly from env; fail fast if missing.
var urlsToUse = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
if (string.IsNullOrWhiteSpace(urlsToUse))
{
    const string urlError = "[HbcRest] ASPNETCORE_URLS not set. Please set ASPNETCORE_URLS to the binding URL(s).";
    Console.Error.WriteLine(urlError);
    throw new InvalidOperationException(urlError);
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

// Simple request logging
app.Use(async (context, next) =>
{
    Console.WriteLine($"[HbcRest] {context.Request.Method} {context.Request.Path}{context.Request.QueryString}");
    await next.Invoke();
});


app.UseSwagger();
app.UseSwaggerUI(c =>
{
    // Group by tag and sort for scanability.
    c.ConfigObject.AdditionalItems["tagsSorter"] = "alpha";
    c.ConfigObject.AdditionalItems["operationsSorter"] = "alpha";
});


const string MonikerSurvey = "nyc_open_data_311_customer_satisfaction_survey";
const string MonikerCall = "nyc_open_data_311_call_center_inquiry";
const string MonikerService = "nyc_open_data_311_service_requests";
const int MaxTop = 100;
const int DefaultTopWhenNoFilter = MaxTop;

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

    try
    {
        if (!string.IsNullOrWhiteSpace(filter))
        {
            query = ApplyFilterGeneric(filter, query);
        }

        if (!string.IsNullOrWhiteSpace(apply))
        {
            return await ApplyGroupByGeneric(apply, query);
        }
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
    catch (NotSupportedException ex)
    {
        return Results.BadRequest(ex.Message);
    }

    if (!string.IsNullOrWhiteSpace(expand))
    {
        return Results.BadRequest("$expand is not supported.");
    }

    var hasOrder = false;
    if (!string.IsNullOrWhiteSpace(orderBy))
    {
        try
        {
            query = ApplyOrderByGeneric(orderBy, query);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (NotSupportedException ex)
        {
            return Results.BadRequest(ex.Message);
        }
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
        // top <= 0 means no limit
    }
    else
    {
        // Consistent default paging across all monikers unless caller explicitly sets $top.
        if (count != true)
        {
            query = query.Take(MaxTop);
        }
    }

    var results = await query.AsNoTracking().ToListAsync();
    IEnumerable<dynamic> projected;
    try
    {
        projected = ApplySelectGeneric(select, results);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
    if (totalCount.HasValue)
    {
        return Results.Ok(new { count = totalCount.Value, value = projected });
    }
    return Results.Ok(projected);
}).WithTags(MonikerSurvey);

app.MapGet($"/{MonikerSurvey}/{{id}}", async (string id, HbcContext db) =>
    await db.CustomerSatisfactionSurveys.FirstOrDefaultAsync(s => s.HbcUniqueKey == id) is { } survey
        ? Results.Ok(survey)
        : Results.NotFound()).WithTags(MonikerSurvey);

app.MapGet($"/{MonikerCall}/{{id}}", async (string id, HbcContext db) =>
    await db.CallCenterInquiries.FirstOrDefaultAsync(s => s.HbcUniqueKey == id) is { } row
        ? Results.Ok(row)
        : Results.NotFound()).WithTags(MonikerCall);

app.MapGet($"/{MonikerService}/{{id}}", async (string id, HbcContext db) =>
    await db.ServiceRequests.FirstOrDefaultAsync(s => s.HbcUniqueKey == id) is { } row
        ? Results.Ok(row)
        : Results.NotFound()).WithTags(MonikerService);

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
}).WithTags(MonikerSurvey);

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
}).WithTags(MonikerSurvey);

app.MapPut($"/{MonikerSurvey}/{{id}}", async (string id, CustomerSatisfactionSurvey input, HbcContext db) =>
{
    var survey = await db.CustomerSatisfactionSurveys.FirstOrDefaultAsync(s => s.HbcUniqueKey == id);

    if (survey is null) return Results.NotFound();

    CopyFields(survey, input);
    var saved = await db.SaveChangesAsync();
    Console.WriteLine($"[HbcRest] PUT /{MonikerSurvey}/{id} saved {saved} row(s)");
    return Results.NoContent();
}).WithTags(MonikerSurvey);

app.MapPut($"/{MonikerCall}/{{id}}", async (string id, CallCenterInquiry input, HbcContext db) =>
{
    var row = await db.CallCenterInquiries.FirstOrDefaultAsync(s => s.HbcUniqueKey == id);

    if (row is null) return Results.NotFound();

    CopyFieldsCall(row, input);
    var saved = await db.SaveChangesAsync();
    Console.WriteLine($"[HbcRest] PUT /{MonikerCall}/{id} saved {saved} row(s)");
    return Results.NoContent();
}).WithTags(MonikerCall);

app.MapPut($"/{MonikerService}/{{id}}", async (string id, ServiceRequest input, HbcContext db) =>
{
    var row = await db.ServiceRequests.FirstOrDefaultAsync(s => s.HbcUniqueKey == id);

    if (row is null) return Results.NotFound();

    CopyFieldsService(row, input);
    var saved = await db.SaveChangesAsync();
    Console.WriteLine($"[HbcRest] PUT /{MonikerService}/{id} saved {saved} row(s)");
    return Results.NoContent();
}).WithTags(MonikerService);

app.MapDelete($"/{MonikerSurvey}/{{id}}", async (string id, HbcContext db) =>
{
    var survey = await db.CustomerSatisfactionSurveys.FirstOrDefaultAsync(s => s.HbcUniqueKey == id);
    if (survey is null) return Results.NotFound();
    db.CustomerSatisfactionSurveys.Remove(survey);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).WithTags(MonikerSurvey);

app.MapDelete($"/{MonikerCall}/{{id}}", async (string id, HbcContext db) =>
{
    var row = await db.CallCenterInquiries.FirstOrDefaultAsync(s => s.HbcUniqueKey == id);
    if (row is null) return Results.NotFound();
    db.CallCenterInquiries.Remove(row);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).WithTags(MonikerCall);

app.MapDelete($"/{MonikerService}/{{id}}", async (string id, HbcContext db) =>
{
    var row = await db.ServiceRequests.FirstOrDefaultAsync(s => s.HbcUniqueKey == id);
    if (row is null) return Results.NotFound();
    db.ServiceRequests.Remove(row);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).WithTags(MonikerService);

app.MapGet($"/{MonikerCall}", async (
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
    IQueryable<CallCenterInquiry> query = db.CallCenterInquiries.AsQueryable();

    try
    {
        if (!string.IsNullOrWhiteSpace(filter))
        {
            query = ApplyFilterGeneric(filter, query);
        }

        if (!string.IsNullOrWhiteSpace(apply))
        {
            return await ApplyGroupByGeneric(apply, query);
        }
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
    catch (NotSupportedException ex)
    {
        return Results.BadRequest(ex.Message);
    }

    if (!string.IsNullOrWhiteSpace(expand))
    {
        return Results.BadRequest("$expand is not supported.");
    }

    var hasOrder = false;
    if (!string.IsNullOrWhiteSpace(orderBy))
    {
        try
        {
            query = ApplyOrderByGeneric(orderBy, query);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (NotSupportedException ex)
        {
            return Results.BadRequest(ex.Message);
        }
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
        // top <= 0 means no limit
    }
    else
    {
        if (count != true)
        {
            query = query.Take(DefaultTopWhenNoFilter);
        }
    }

    var results = await query.AsNoTracking().ToListAsync();
    IEnumerable<dynamic> projected;
    try
    {
        projected = ApplySelectGeneric(select, results);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
    if (totalCount.HasValue)
    {
        return Results.Ok(new { count = totalCount.Value, value = projected });
    }
    return Results.Ok(projected);
}).WithTags(MonikerCall);

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
}).WithTags(MonikerCall);

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
}).WithTags(MonikerCall);

app.MapGet($"/{MonikerService}", async (
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
    IQueryable<ServiceRequest> query = db.ServiceRequests.AsQueryable();

    try
    {
        if (!string.IsNullOrWhiteSpace(filter))
        {
            query = ApplyFilterGeneric(filter, query);
        }

        if (!string.IsNullOrWhiteSpace(apply))
        {
            return await ApplyGroupByGeneric(apply, query);
        }
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
    catch (NotSupportedException ex)
    {
        return Results.BadRequest(ex.Message);
    }

    if (!string.IsNullOrWhiteSpace(expand))
    {
        return Results.BadRequest("$expand is not supported.");
    }

    var hasOrder = false;
    if (!string.IsNullOrWhiteSpace(orderBy))
    {
        try
        {
            query = ApplyOrderByGeneric(orderBy, query);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (NotSupportedException ex)
        {
            return Results.BadRequest(ex.Message);
        }
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
        // top <= 0 means no limit
    }
    else
    {
        if (count != true)
        {
            query = query.Take(DefaultTopWhenNoFilter);
        }
    }

    var results = await query.AsNoTracking().ToListAsync();
    IEnumerable<dynamic> projected;
    try
    {
        projected = ApplySelectGeneric(select, results);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
    if (totalCount.HasValue)
    {
        return Results.Ok(new { count = totalCount.Value, value = projected });
    }
    return Results.Ok(projected);
}).WithTags(MonikerService);

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
}).WithTags(MonikerService);

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
}).WithTags(MonikerService);

// Try to open Swagger in the default browser when the app starts.
app.Lifetime.ApplicationStarted.Register(() =>
{
    var swaggerUrl = $"{urlsToUse.TrimEnd('/')}/swagger/index.html";
    try
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start(new ProcessStartInfo("cmd", $"/c start {swaggerUrl}")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", swaggerUrl);
        }
        else
        {
            Process.Start("xdg-open", swaggerUrl);
        }
    }
    catch
    {
        // Best-effort only; ignore if not available (headless/server, etc.)
    }
});

try
{
    app.Run();
}
catch (AddressInUseException)
{
    Console.Error.WriteLine("[HbcRest] Failed to start: address/port already in use. Check ASPNETCORE_URLS or stop the other process.");
}
catch (IOException ioEx) when (ioEx.InnerException is AddressInUseException)
{
    Console.Error.WriteLine("[HbcRest] Failed to start: address/port already in use. Check ASPNETCORE_URLS or stop the other process.");
}

// Helper methods live in ApiHelpers.cs
public partial class Program;
