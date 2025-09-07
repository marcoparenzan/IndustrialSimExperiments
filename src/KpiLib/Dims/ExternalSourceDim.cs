using System.Diagnostics;

namespace KpiLib.Dims;

[DebuggerDisplay("{Id} {SourceName}")]
public class ExternalSourceDim
{
    public long Id { get; set; }
    public string SourceCode { get; set; } = "";
    public string SourceName { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
