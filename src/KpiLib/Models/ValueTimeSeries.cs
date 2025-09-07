using System.Diagnostics;

namespace KpiLib.Models;

[DebuggerDisplay("{Id} {UnitId} {DoubleValue}")]
public class ValueTimeSeries
{
    public long Id { get; set; }
    public long? PrevId { get; set; }
    public long ValueId { get; set; }
    public long ContextId { get; set; }
    public long UnitId { get; set; }
    public double DoubleValue { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public string QualityFlag { get; set; } = "ok";
    public string? CalcMethod { get; set; }
    public string? SourceRunId { get; set; }
    public DateTime InsertedAt { get; set; } = DateTime.UtcNow;
}
