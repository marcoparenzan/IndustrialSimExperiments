using System.Diagnostics;

namespace KpiLib.Models;

[DebuggerDisplay("{FormulaId} {InputValueId} {InputAlias}")]
public class FormulaInput
{
    public long FormulaId { get; set; }
    public long InputValueId { get; set; }
    public string InputAlias { get; set; } = "";
}
