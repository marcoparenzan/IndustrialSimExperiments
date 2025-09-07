using System.Diagnostics;

namespace KpiLib.Dims;

[DebuggerDisplay("{Id} {ContextName}")]
public class ContextDim
{
    public long Id { get; set; }
    public long? PrevId { get; set; }
    public string ContextCode { get; set; } = "";
    public string? ContextName { get; set; }
    public string? Plant { get; set; }
    public string? Area { get; set; }
    public string? AssetId { get; set; }
    public string? ShiftCode { get; set; }
    public string? ProductCode { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool CurrentFlag { get; set; } = true;
}
