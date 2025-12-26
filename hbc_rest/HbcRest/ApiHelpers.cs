using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HbcRest.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HbcRest;

internal static class ApiHelpers
{
    public static IQueryable<CustomerSatisfactionSurvey> ApplyFilter(string filter, IQueryable<CustomerSatisfactionSurvey> query)
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

    public static IQueryable<CustomerSatisfactionSurvey> ApplyOrderBy(string orderBy, IQueryable<CustomerSatisfactionSurvey> query)
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

    public static IEnumerable<dynamic> ApplySelect(string? select, IEnumerable<CustomerSatisfactionSurvey> items)
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

    public static async Task<IResult> ApplyGroupBy(string apply, IQueryable<CustomerSatisfactionSurvey> query)
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

    public static IQueryable<CallCenterInquiry> ApplyFilterCall(string filter, IQueryable<CallCenterInquiry> query)
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

    public static IQueryable<CallCenterInquiry> ApplyOrderByCall(string orderBy, IQueryable<CallCenterInquiry> query)
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

    public static IEnumerable<dynamic> ApplySelectCall(string? select, IEnumerable<CallCenterInquiry> items)
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

    public static IQueryable<ServiceRequest> ApplyFilterService(string filter, IQueryable<ServiceRequest> query)
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

    public static IQueryable<ServiceRequest> ApplyOrderByService(string orderBy, IQueryable<ServiceRequest> query)
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

    public static IEnumerable<dynamic> ApplySelectService(string? select, IEnumerable<ServiceRequest> items)
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

    public static void CopyFields(CustomerSatisfactionSurvey target, CustomerSatisfactionSurvey source)
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

    public static void CopyFieldsCall(CallCenterInquiry target, CallCenterInquiry source)
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

    public static void CopyFieldsService(ServiceRequest target, ServiceRequest source)
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
