namespace IndustrialSimLib;

public interface IBindable<T>
{
    object Bounded { get; set; }
    T Value { get; set; }

    string ToString();
}