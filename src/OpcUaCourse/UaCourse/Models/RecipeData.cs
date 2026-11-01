using Opc.Ua;

namespace UaCourse.Models;

/// <summary>Contratto binario UA: l'ordine dei campi coincide con StructureDefinition.</summary>
public sealed class RecipeData : IEncodeable
{
    public string Name { get; set; } = "";
    public uint Revision { get; set; }
    public double Target { get; set; }
    public double[] Steps { get; set; } = [];
    public ExpandedNodeId TypeId => new("RecipeDataType", Lessons.NamespaceUri);
    public ExpandedNodeId BinaryEncodingId => new("RecipeDataType.Binary", Lessons.NamespaceUri);
    public ExpandedNodeId XmlEncodingId => ExpandedNodeId.Null;

    public void Encode(IEncoder encoder)
    {
        encoder.PushNamespace(Lessons.NamespaceUri);
        try
        {
            encoder.WriteString(nameof(Name), Name);
            encoder.WriteUInt32(nameof(Revision), Revision);
            encoder.WriteDouble(nameof(Target), Target);
            encoder.WriteDoubleArray(nameof(Steps), Steps);
        }
        finally { encoder.PopNamespace(); }
    }
    public void Decode(IDecoder decoder)
    {
        decoder.PushNamespace(Lessons.NamespaceUri);
        try
        {
            Name = decoder.ReadString(nameof(Name));
            Revision = decoder.ReadUInt32(nameof(Revision));
            Target = decoder.ReadDouble(nameof(Target));
            Steps = decoder.ReadDoubleArray(nameof(Steps))?.ToArray() ?? [];
        }
        finally { decoder.PopNamespace(); }
    }
    public bool IsEqual(IEncodeable other) => other is RecipeData recipe &&
        Name == recipe.Name && Revision == recipe.Revision && Target.Equals(recipe.Target) &&
        Steps.SequenceEqual(recipe.Steps);
    public object Clone() => new RecipeData
    { Name = Name, Revision = Revision, Target = Target, Steps = (double[])Steps.Clone() };

    public bool IsValid() => !string.IsNullOrWhiteSpace(Name) && Name.Length <= 128 &&
        Revision > 0 && ValidTarget(Target) && Steps is { Length: > 0 and <= 64 } && Steps.All(ValidTarget);
    private static bool ValidTarget(double value) => double.IsFinite(value) && value >= 20 && value <= 90;
    public override string ToString() => $"{Name} r{Revision}, target={Target}, steps=[{string.Join(", ", Steps)}]";
}
