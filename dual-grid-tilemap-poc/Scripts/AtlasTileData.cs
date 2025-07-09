using Godot;
using System;
namespace DualGridTilemaps
{
    [GlobalClass]
    public partial class AtlasTileData : Resource
    {
        [Export] public Vector2I atlasPosition;
        [Export] public ETileSubset subset;
    }
}