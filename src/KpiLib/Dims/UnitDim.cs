using System.Diagnostics;

namespace KpiLib.Dims;

[DebuggerDisplay("{Id} {UnitName}")]
public class UnitDim
{
    public long Id { get; set; }
    public long? PrevId { get; set; }
    public string UnitCode { get; set; } = "";
    public string UnitName { get; set; } = "";
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool CurrentFlag { get; set; } = true;
}
