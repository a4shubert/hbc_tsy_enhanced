using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace HbcRest.Data;

[Table("nyc_open_data_311_call_center_inquiry")]
public class NycOpenData311CallCenterInquiry
{
    [Key]
    [Column("id")]
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [Column("hbc_unique_key")]
    [JsonPropertyName("hbc_unique_key")]
    public string? HbcUniqueKey { get; set; }

    [Column("unique_id")]
    [JsonPropertyName("unique_id")]
    public string? UniqueId { get; set; }

    [Column("date")]
    [JsonPropertyName("date")]
    public DateTime? Date { get; set; }

    [Column("time")]
    [JsonPropertyName("time")]
    public string? Time { get; set; }

    [Column("date_time")]
    [JsonPropertyName("date_time")]
    public DateTime? DateTime { get; set; }

    [Column("agency")]
    [JsonPropertyName("agency")]
    public string? Agency { get; set; }

    [Column("agency_name")]
    [JsonPropertyName("agency_name")]
    public string? AgencyName { get; set; }

    [Column("inquiry_name")]
    [JsonPropertyName("inquiry_name")]
    public string? InquiryName { get; set; }

    [Column("brief_description")]
    [JsonPropertyName("brief_description")]
    public string? BriefDescription { get; set; }

    [Column("call_resolution")]
    [JsonPropertyName("call_resolution")]
    public string? CallResolution { get; set; }
}
