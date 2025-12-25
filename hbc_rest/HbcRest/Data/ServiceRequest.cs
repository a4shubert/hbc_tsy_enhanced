using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace HbcRest.Data;

[Table("nyc_open_data_311_service_requests")]
public class ServiceRequest
{
    [Key]
    [Column("id")]
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [Column("hbc_unique_key")]
    [JsonPropertyName("hbc_unique_key")]
    public string? HbcUniqueKey { get; set; }

    [Column("address_type")]
    [JsonPropertyName("address_type")]
    public string? AddressType { get; set; }

    [Column("agency")]
    [JsonPropertyName("agency")]
    public string? Agency { get; set; }

    [Column("agency_name")]
    [JsonPropertyName("agency_name")]
    public string? AgencyName { get; set; }

    [Column("borough")]
    [JsonPropertyName("borough")]
    public string? Borough { get; set; }

    [Column("bridge_highway_direction")]
    [JsonPropertyName("bridge_highway_direction")]
    public string? BridgeHighwayDirection { get; set; }

    [Column("bridge_highway_name")]
    [JsonPropertyName("bridge_highway_name")]
    public string? BridgeHighwayName { get; set; }

    [Column("bridge_highway_segment")]
    [JsonPropertyName("bridge_highway_segment")]
    public string? BridgeHighwaySegment { get; set; }

    [Column("city")]
    [JsonPropertyName("city")]
    public string? City { get; set; }

    [Column("closed_date")]
    [JsonPropertyName("closed_date")]
    public DateTime? ClosedDate { get; set; }

    [Column("community_board")]
    [JsonPropertyName("community_board")]
    public string? CommunityBoard { get; set; }

    [Column("complaint_type")]
    [JsonPropertyName("complaint_type")]
    public string? ComplaintType { get; set; }

    [Column("created_date")]
    [JsonPropertyName("created_date")]
    public DateTime? CreatedDate { get; set; }

    [Column("cross_street_1")]
    [JsonPropertyName("cross_street_1")]
    public string? CrossStreet1 { get; set; }

    [Column("cross_street_2")]
    [JsonPropertyName("cross_street_2")]
    public string? CrossStreet2 { get; set; }

    [Column("descriptor")]
    [JsonPropertyName("descriptor")]
    public string? Descriptor { get; set; }

    [Column("due_date")]
    [JsonPropertyName("due_date")]
    public DateTime? DueDate { get; set; }

    [Column("facility_type")]
    [JsonPropertyName("facility_type")]
    public string? FacilityType { get; set; }

    [Column("ferry_direction")]
    [JsonPropertyName("ferry_direction")]
    public string? FerryDirection { get; set; }

    [Column("ferry_terminal_name")]
    [JsonPropertyName("ferry_terminal_name")]
    public string? FerryTerminalName { get; set; }

    [Column("garage_lot_name")]
    [JsonPropertyName("garage_lot_name")]
    public string? GarageLotName { get; set; }

    [Column("incident_address")]
    [JsonPropertyName("incident_address")]
    public string? IncidentAddress { get; set; }

    [Column("incident_zip")]
    [JsonPropertyName("incident_zip")]
    public string? IncidentZip { get; set; }

    [Column("intersection_street_1")]
    [JsonPropertyName("intersection_street_1")]
    public string? IntersectionStreet1 { get; set; }

    [Column("intersection_street_2")]
    [JsonPropertyName("intersection_street_2")]
    public string? IntersectionStreet2 { get; set; }

    [Column("landmark")]
    [JsonPropertyName("landmark")]
    public string? Landmark { get; set; }

    [Column("latitude")]
    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    [Column("location")]
    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [Column("location_type")]
    [JsonPropertyName("location_type")]
    public string? LocationType { get; set; }

    [Column("longitude")]
    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }

    [Column("park_borough")]
    [JsonPropertyName("park_borough")]
    public string? ParkBorough { get; set; }

    [Column("park_facility_name")]
    [JsonPropertyName("park_facility_name")]
    public string? ParkFacilityName { get; set; }

    [Column("resolution_action_updated_date")]
    [JsonPropertyName("resolution_action_updated_date")]
    public DateTime? ResolutionActionUpdatedDate { get; set; }

    [Column("road_ramp")]
    [JsonPropertyName("road_ramp")]
    public string? RoadRamp { get; set; }

    [Column("school_address")]
    [JsonPropertyName("school_address")]
    public string? SchoolAddress { get; set; }

    [Column("school_city")]
    [JsonPropertyName("school_city")]
    public string? SchoolCity { get; set; }

    [Column("school_code")]
    [JsonPropertyName("school_code")]
    public string? SchoolCode { get; set; }

    [Column("school_name")]
    [JsonPropertyName("school_name")]
    public string? SchoolName { get; set; }

    [Column("school_not_found")]
    [JsonPropertyName("school_not_found")]
    public string? SchoolNotFound { get; set; }

    [Column("school_number")]
    [JsonPropertyName("school_number")]
    public string? SchoolNumber { get; set; }

    [Column("school_or_citywide_complaint")]
    [JsonPropertyName("school_or_citywide_complaint")]
    public string? SchoolOrCitywideComplaint { get; set; }

    [Column("school_phone_number")]
    [JsonPropertyName("school_phone_number")]
    public string? SchoolPhoneNumber { get; set; }

    [Column("school_region")]
    [JsonPropertyName("school_region")]
    public string? SchoolRegion { get; set; }

    [Column("school_state")]
    [JsonPropertyName("school_state")]
    public string? SchoolState { get; set; }

    [Column("school_zip")]
    [JsonPropertyName("school_zip")]
    public string? SchoolZip { get; set; }

    [Column("status")]
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [Column("street_name")]
    [JsonPropertyName("street_name")]
    public string? StreetName { get; set; }

    [Column("taxi_company_borough")]
    [JsonPropertyName("taxi_company_borough")]
    public string? TaxiCompanyBorough { get; set; }

    [Column("taxi_pick_up_location")]
    [JsonPropertyName("taxi_pick_up_location")]
    public string? TaxiPickUpLocation { get; set; }

    [Column("unique_key")]
    [JsonPropertyName("unique_key")]
    public string? UniqueKey { get; set; }

    [Column("vehicle_type")]
    [JsonPropertyName("vehicle_type")]
    public string? VehicleType { get; set; }

    [Column("x_coordinate_state_plane_")]
    [JsonPropertyName("x_coordinate_state_plane_")]
    public double? XCoordinateStatePlane { get; set; }

    [Column("y_coordinate_state_plane_")]
    [JsonPropertyName("y_coordinate_state_plane_")]
    public double? YCoordinateStatePlane { get; set; }
}
