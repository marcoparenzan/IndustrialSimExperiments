namespace KpiLib.Models;

public class ParamFeedStg
{
    public long Id { get; set; }
    public long SourceId { get; set; }
    public string ExtValueCode { get; set; } = "";
    public string ContextCode { get; set; } = "";
    public string UnitCode { get; set; } = "";
    public double ValueNum { get; set; }
    public DateTime TsUtc { get; set; }
    public string? BatchId { get; set; }
    public DateTime InsertedAt { get; set; } = DateTime.UtcNow;
    public bool ProcessedFlag { get; set; } = false;
    public string? ErrorMsg { get; set; }
}
