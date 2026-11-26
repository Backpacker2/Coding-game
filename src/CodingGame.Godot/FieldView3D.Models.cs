using Godot;

namespace CodingGame.Godot;

public partial class FieldView3D
{
    // Kenney trees are about 1.5 tiles tall; this keeps them the size of the earlier primitive trees.
    private const float TreeModelScale = 0.8f;

    private static readonly string[] _treeModels = ["tree_default", "tree_oak", "tree_cone", "tree_pineRoundA", "tree_fat"];
    private static readonly string[] _rockModels = ["stone_largeA", "stone_largeB", "stone_smallC", "stone_smallE"];

    /// <summary>
    /// Makes a copy of a model from the models folder, with every material replaced, so the model takes the
    /// colours of the palette instead of its own.
    /// </summary>
    /// <param name="name">The model's file name without <c>.glb</c>, for example <c>tree_oak</c>.</param>
    /// <param name="materialFor">Gives the material for each of the model's own material names.</param>
    /// <returns>The model, standing on its origin.</returns>
    private static Node3D Model(string name, Func<string, Material> materialFor)
    {
        Node3D model = GD.Load<PackedScene>($"res://models/{name}.glb").Instantiate<Node3D>();
        Recolour(model, materialFor);
        return model;
    }

    private static void Recolour(Node node, Func<string, Material> materialFor)
    {
        if (node is MeshInstance3D meshInstance)
        {
            for (int surface = 0; surface < meshInstance.Mesh.GetSurfaceCount(); surface++)
            {
                meshInstance.SetSurfaceOverrideMaterial(surface, materialFor(meshInstance.Mesh.SurfaceGetMaterial(surface).ResourceName));
            }
        }

        foreach (Node child in node.GetChildren())
        {
            Recolour(child, materialFor);
        }
    }

    private Node3D TreeModel(uint hash)
    {
        Node3D tree = Model(_treeModels[hash % _treeModels.Length], material => MaterialFor(material.StartsWith("wood", StringComparison.Ordinal) ? Palette.TreeTrunk : Palette.TreeCrown, false));
        tree.Scale = Vector3.One * TreeModelScale;
        return tree;
    }

    private static Node3D CropModel(string name, float scale, Func<string, Material> materialFor)
    {
        Node3D model = Model(name, materialFor);
        model.Scale = Vector3.One * scale;
        return model;
    }

    private Node3D RockModel(uint hash)
    {
        return Model(_rockModels[hash % _rockModels.Length], _ => MaterialFor(Palette.Rock, false));
    }
}
