namespace TTS.Domain.Models.Ai;

public sealed class AiQueryPlan
{
    public QueryIntent Intent { get; set; } = QueryIntent.Unknown;

    public List<string> TargetTables { get; set; } = [];

    public List<string> TargetColumns { get; set; } = [];

    public List<QueryFilter> Filters { get; set; } = [];

    public List<AggregationDefinition> Aggregations { get; set; } = [];

    public List<SortDefinition> Sort { get; set; } = [];

    public int? Limit { get; set; }

    public string GeneratedSql { get; set; } = string.Empty;

    public double Confidence { get; set; }

    public List<string> Warnings { get; set; } = [];
}
