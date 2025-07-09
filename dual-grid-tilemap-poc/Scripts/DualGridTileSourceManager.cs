using Godot;
using System;
using System.Collections.Generic;
namespace DualGridTilemaps
{
    [GlobalClass]
    public partial class DualGridTileSourceManager : Node
    {
        private static readonly DualGridTileSourceManager instance = new DualGridTileSourceManager();
        static DualGridTileSourceManager() { }
        private DualGridTileSourceManager() { }
        public static DualGridTileSourceManager Instance { get { return instance; } }
        private static readonly Dictionary<EPlaceholderSources, int> AtlasSourceDict = new Dictionary<EPlaceholderSources, int>
    {
        { EPlaceholderSources.Natural,0},
        { EPlaceholderSources.Stone,1},
        { EPlaceholderSources.Windows,2}
    };

        public static int GetAtlasSource(EPlaceholderSources type)
        {
            if (!AtlasSourceDict.ContainsKey(type)) { GD.Print("DualGridTileSourceManager: Key not found"); return -1; }
            return AtlasSourceDict[type];
        }
    }
}