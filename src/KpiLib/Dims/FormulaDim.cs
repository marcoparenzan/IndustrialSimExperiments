using System.Diagnostics;

namespace KpiLib.Dims;

[DebuggerDisplay("{Id} {Expression}")]
public class FormulaDim
{
    public long Id { get; set; }
    public long? PrevId { get; set; }
    public long ValueId { get; set; }      // KPI this formula computes
    public string Engine { get; set; } = "expr";
    public string Expression { get; set; } = ""; // use [ALIAS]
    public string? Notes { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool CurrentFlag { get; set; } = true;
}
