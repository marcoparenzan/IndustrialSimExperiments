using Opc.Ua;
using UaCourse.Models;

namespace UaCourse;

internal sealed partial class PlantNodeManager
{
    private void CreateRecipeModel(IDictionary<NodeId, IList<IReference>> externalReferences)
    {
        var definition = new StructureDefinition
        {
            BaseDataType = DataTypeIds.Structure,
            DefaultEncodingId = Id("RecipeDataType.Binary"),
            StructureType = StructureType.Structure,
            Fields = new StructureFieldCollection
            {
                Field("Name", DataTypeIds.String), Field("Revision", DataTypeIds.UInt32),
                Field("Target", DataTypeIds.Double), Field("Steps", DataTypeIds.Double, ValueRanks.OneDimension)
            }
        };
        var type = new DataTypeState
        {
            NodeId = Id("RecipeDataType"), BrowseName = Q("RecipeDataType"), DisplayName = "RecipeDataType",
            IsAbstract = false, SuperTypeId = DataTypeIds.Structure,
            DataTypeDefinition = new ExtensionObject(definition)
        };
        if (!externalReferences.TryGetValue(DataTypeIds.Structure, out var references))
            externalReferences[DataTypeIds.Structure] = references = new List<IReference>();
        references.Add(new NodeStateReference(ReferenceTypeIds.HasSubtype, false, type.NodeId));
        var encoding = new BaseObjectState(null)
        {
            NodeId = definition.DefaultEncodingId, BrowseName = new QualifiedName("Default Binary"),
            DisplayName = "Default Binary", ReferenceTypeId = ReferenceTypeIds.HasEncoding,
            TypeDefinitionId = ObjectTypeIds.DataTypeEncodingType
        };
        type.AddReference(ReferenceTypeIds.HasEncoding, false, encoding.NodeId);
        encoding.AddReference(ReferenceTypeIds.HasEncoding, true, type.NodeId);
        AddPredefinedNode(SystemContext, encoding);
        AddPredefinedNode(SystemContext, type);
        var recipe = Variable(reactor, "Reactor1.BatchRecipe", "BatchRecipe", type.NodeId,
            new ExtensionObject(new RecipeData { Name = "Standard", Revision = 1, Target = 60, Steps = [20, 40, 60] }));
        var method = Method("LoadRecipe", [Arg("Recipe", type.NodeId)], [Arg("AcceptedRevision", DataTypeIds.UInt32)]);
        method.OnCallMethod = (ISystemContext context, MethodState state, IList<object> input, IList<object> output) =>
        {
            lock (Lock)
            {
                var decoded = input[0] is RecipeData direct ? direct :
                    ExtensionObject.ToEncodeable(input[0] as ExtensionObject) as RecipeData;
                if (decoded is null) return StatusCodes.BadTypeMismatch;
                if (!decoded.IsValid()) return StatusCodes.BadOutOfRange;
                Set(recipe, new ExtensionObject((RecipeData)decoded.Clone()));
                Set(setpoint, decoded.Target);
                output[0] = decoded.Revision;
                Emit($"Ricetta caricata: {decoded.Name} r{decoded.Revision}", 300);
                return ServiceResult.Good;
            }
        };
        // Modello esportato dalla stessa definizione usata dal server, per evitare drift.
        var nodeSet = new Opc.Ua.Export.UANodeSet();
        nodeSet.Export(SystemContext, type, true);
        nodeSet.Export(SystemContext, encoding, true);
        var directory = Path.Combine(Configuration.ArtifactsRoot, "models");
        Directory.CreateDirectory(directory);
        using var stream = File.Create(Path.Combine(directory, "Recipe.NodeSet2.xml"));
        nodeSet.Write(stream);
    }
    private static StructureField Field(string name, NodeId type, int rank = ValueRanks.Scalar) => new()
    { Name = name, DataType = type, ValueRank = rank, Description = name, IsOptional = false };
}
