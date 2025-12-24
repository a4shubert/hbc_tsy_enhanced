using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace HbcRest.Data;

[Table("nyc_open_data_311_customer_satisfaction_survey")]
public class CustomerSatisfactionSurvey
{
    [Key]
    [Column("id")]
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [Column("year")]
    [JsonPropertyName("year")]
    public string? Year { get; set; } // schema: text

    [Column("campaign")]
    [JsonPropertyName("campaign")]
    public string? Campaign { get; set; }

    [Column("channel")]
    [JsonPropertyName("channel")]
    public string? Channel { get; set; }

    [Column("survey_type")]
    [JsonPropertyName("survey_type")]
    public string? SurveyType { get; set; }

    [Column("start_time")]
    [JsonPropertyName("start_time")]
    public DateTime? StartTime { get; set; }

    [Column("completion_time")]
    [JsonPropertyName("completion_time")]
    public DateTime? CompletionTime { get; set; }

    [Column("survey_language")]
    [JsonPropertyName("survey_language")]
    public string? SurveyLanguage { get; set; }

    [Column("overall_satisfaction")]
    [JsonPropertyName("overall_satisfaction")]
    public string? OverallSatisfaction { get; set; }

    [Column("wait_time")]
    [JsonPropertyName("wait_time")]
    public string? WaitTime { get; set; }

    [Column("agent_customer_service")]
    [JsonPropertyName("agent_customer_service")]
    public string? AgentCustomerService { get; set; }

    [Column("agent_job_knowledge")]
    [JsonPropertyName("agent_job_knowledge")]
    public string? AgentJobKnowledge { get; set; }

    [Column("answer_satisfaction")]
    [JsonPropertyName("answer_satisfaction")]
    public string? AnswerSatisfaction { get; set; }

    [Column("nps")]
    [JsonPropertyName("nps")]
    public int? Nps { get; set; }
}
