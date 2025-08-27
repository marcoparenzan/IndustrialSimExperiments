namespace IndustrialSimLib;

public interface IBindable<T>
{
    object Bounded { get; set; }
    T Value { get; }
    void Reset() => Set(default!);
    void Set(T value);
    void Add(T value);
    string ToString();
}