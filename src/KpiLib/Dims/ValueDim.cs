using System.Diagnostics;

namespace KpiLib.Dims;

[DebuggerDisplay("{Id} {ValueName}")]
public class ValueDim
{
    public long Id { get; set; }
    public long? PrevId { get; set; }
    public string ValueCode { get; set; } = "";
    public string ValueName { get; set; } = "";
    public string ValueKind { get; set; } = "KPI"; // KPI | PARAM
    public long? DefaultUnitId { get; set; }
    public string? Category { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool CurrentFlag { get; set; } = true;
}
