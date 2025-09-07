namespace KpiLib.Models;

public class ExternalValueMap
{
    public long Id { get; set; }
    public long SourceId { get; set; }
    public string ExtValueCode { get; set; } = "";
    public string ValueCode { get; set; } = "";
    public string ValueKind { get; set; } = "PARAM";
    public string? ExtContextCode { get; set; }
    public string ContextCode { get; set; } = "";
    public string UnitCode { get; set; } = "";
    public bool IsActive { get; set; } = true;
}
