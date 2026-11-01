using UaCourse.Web.Modules;

namespace UaCourse.Web.Services;

/// <summary>Read side of a connection: what a synoptic needs to draw its labels.</summary>
public interface ISignalSource
{
    bool Connected { get; }
    string Status { get; }
    SignalValue? Get(string node);
}

/// <summary>Picks the connection that feeds a screen: the course server, or ForgeSimApp for screens with Source = forgesim.</summary>
public sealed class SignalSources(UaConnection course, ForgeSimConnection forgeSim)
{
    public ISignalSource For(ScreenDefinition screen) => screen.Source == ScreenSources.ForgeSim ? forgeSim : course;
}
