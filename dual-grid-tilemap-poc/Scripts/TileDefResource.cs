using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
namespace DualGridTilemaps
{
    [GlobalClass]
    public partial class TileDefResource : Resource
    {
        // lower is less important
        [Export] public int hierarchy = 0;
        [Export] public TileSet tileSet;
        [Export] public AtlasTileData[] tileSubsets;
        [Export] public EPlaceholderSources placeholderSource;
        public TileSetSource tileSetSource { get { return getSource(); } }
        private bool initialized = false;

        public TileDefResource() { }

        public TileSetSource getSource()
        {
            TileSetSource orig = tileSet.GetSource(0);
            return (TileSetSource)orig.Duplicate(false);
        }
    }
}