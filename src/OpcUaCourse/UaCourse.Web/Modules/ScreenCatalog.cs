namespace UaCourse.Web.Modules;

public record SignalLabel(string Node, string Caption, string Unit, double X, double Y, string Format = "F1");
public record ScreenCommand(string Caption, string Method, double X, double Y);
public record ScreenDefinition(string Id, string Title, string Description, string Background, IReadOnlyList<SignalLabel> Labels)
{
    public IReadOnlyList<ScreenCommand> Commands { get; init; } = [];
}

public sealed class ScreenCatalog
{
    public IReadOnlyList<ScreenDefinition> Screens { get; } =
    [
        new("water", "Impianto idrico", "Filtri, pozzi e serbatoi · sinottico ricostruito dalla schermata di riferimento", "/screens/water.svg",
        [
            new("WaterPlant.Influent", "Ingresso", "MGD", 8, 13),
            new("WaterPlant.FilterLevel", "Livello filtro", "in", 8, 36),
            new("WaterPlant.Well1", "LT20", "ft", 26, 46),
            new("WaterPlant.Well2", "LT40", "ft", 47, 46),
            new("WaterPlant.GST1", "LT601", "ft", 66, 46),
            new("WaterPlant.GST2", "LT602", "ft", 78, 46),
            new("WaterPlant.GST3", "LT603", "ft", 90, 46),
            new("WaterPlant.Well4", "LT80", "ft", 23, 91),
            new("WaterPlant.Pressure", "PT130", "PSI", 72, 70),
            new("WaterPlant.LowPressure", "PT100", "PSI", 48, 91),
            new("WaterPlant.Distribution", "Distribuzione", "GPM", 87, 64, "F0"),
            new("WaterPlant.Totalizer", "Totalizzatore", "gal", 87, 94, "F0"),
            new("WaterPlant.Filter1", "Filtro 1", "GPM", 24, 5), new("WaterPlant.Filter2", "Filtro 2", "GPM", 34, 5),
            new("WaterPlant.Filter3", "Filtro 3", "GPM", 44, 5), new("WaterPlant.Filter4", "Filtro 4", "GPM", 54, 5),
            new("WaterPlant.Filter5", "Filtro 5", "GPM", 64, 5), new("WaterPlant.Filter6", "Filtro 6", "GPM", 74, 5),
            new("WaterPlant.Filter7", "Filtro 7", "GPM", 84, 5), new("WaterPlant.Filter8", "Filtro 8", "GPM", 94, 5)
        ]) { Commands = [new("Avvia processo", "Start", 48, 61), new("Arresta", "Stop", 65, 61)] },
        new("reactor", "Reattore e ricette", "ObjectType, metodi, qualità e dati strutturati · lezioni 05–10", "/screens/reactor.svg",
        [
            new("Reactor1.Temperature", "Temperatura", "°C", 50, 45),
            new("Reactor1.Setpoint", "Setpoint", "°C", 76, 25),
            new("Reactor1.Running", "In marcia", "", 24, 25)
        ]) { Commands = [new("Avvia", "Start", 24, 55), new("Arresta", "Stop", 76, 55)] }
    ];
}
