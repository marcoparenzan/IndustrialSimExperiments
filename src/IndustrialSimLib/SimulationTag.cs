namespace IndustrialSimLib;

public sealed record SimulationTag(string Path, Type DataType, Func<object?> Read)
{
    public object? Value => Read();

    public static SimulationTag Create<T>(string path, Func<T> read) =>
        new(path, typeof(T), () => read());
}
