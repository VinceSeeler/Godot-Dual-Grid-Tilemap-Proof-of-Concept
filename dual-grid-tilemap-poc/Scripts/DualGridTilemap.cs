using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
namespace DualGridTilemaps
{
    [GlobalClass]
    public partial class DualGridTilemap : TileMapLayer
    {
        [Export] public bool clearEdges = false;
        [Export] public bool paced = true;
        [Export] public int drawRate = 128;
        [Export] TileDefResource[] TilesetResources;
        protected TileMapLayer displayTileMapLayer1;
        protected TileMapLayer displayTileMapLayer2;
        protected TileMapLayer displayTileMapLayer3;
        protected TileMapLayer displayTileMapLayer4;
        // Key<Tile source, placeholder tile atlas coords, Is a path entry>, Value<source ID, tilemap resource>
        protected Dictionary<Tuple<TileSetSource, Vector2I, ETileSubset>, Tuple<int, TileDefResource>> tilesetDict = new Dictionary<Tuple<TileSetSource, Vector2I, ETileSubset>, Tuple<int, TileDefResource>>();
        // Key<Tile which is processed> Value<List telling setter what tiles need to be drawn>
        protected Dictionary<Vector2I, List<TileSetArgs>> tileDict = new Dictionary<Vector2I, List<TileSetArgs>>();
        // Key<source ID of placeholder tileset, atlas position> Value<Tiles of that placeholder>
        public Dictionary<PlaceholderTileKey, HashSet<Vector2I>> placeholderTileHashDict = new Dictionary<PlaceholderTileKey, HashSet<Vector2I>>();
        protected List<TileMapLayer> displayTileMapLayers = new List<TileMapLayer>();
        protected TileSet displayTileset;
        protected static readonly object padLock = new object();
        protected static readonly object padLock2 = new object();
        protected static readonly object padLock3 = new object();

        protected List<List<TileSetArgs>> tilesSet = new List<List<TileSetArgs>>();

        protected bool drawn = false;
        public DualGridTilemap() { }
        public override void _EnterTree()
        {
            CollisionEnabled = false;
            NavigationEnabled = false;
            if (displayTileMapLayer1 == null)
            {
                displayTileMapLayer1 = new TileMapLayer();
                AddChild(displayTileMapLayer1);
            }
            displayTileMapLayer1.ZIndex = 4;
            displayTileMapLayer1.ZAsRelative = true;
            displayTileMapLayer1.CollisionEnabled = false;
            displayTileMapLayer1.NavigationEnabled = false;
            if (displayTileMapLayer2 == null)
            {
                displayTileMapLayer2 = new TileMapLayer();
                AddChild(displayTileMapLayer2);
            }
            displayTileMapLayer2.ZIndex = 3;
            displayTileMapLayer2.ZAsRelative = true;
            displayTileMapLayer2.CollisionEnabled = false;
            displayTileMapLayer2.NavigationEnabled = false;
            if (displayTileMapLayer3 == null)
            {
                displayTileMapLayer3 = new TileMapLayer();
                AddChild(displayTileMapLayer3);
            }
            displayTileMapLayer3.ZIndex = 2;
            displayTileMapLayer3.ZAsRelative = true;
            displayTileMapLayer3.CollisionEnabled = false;
            displayTileMapLayer3.NavigationEnabled = false;
            if (displayTileMapLayer4 == null)
            {
                displayTileMapLayer4 = new TileMapLayer();
                AddChild(displayTileMapLayer4);
            }
            displayTileMapLayer4.ZIndex = 1;
            displayTileMapLayer4.ZAsRelative = true;
            displayTileMapLayer4.CollisionEnabled = false;
            displayTileMapLayer4.NavigationEnabled = false;
        }
        public override void _Ready()
        {
            SetProcess(false);
            SetPhysicsProcess(false);
            Enabled = false;
            BuildDisplayTileset();
            ScanTiles();
        }

        public override void _Process(double delta)
        {
            SetTilesPaced(drawRate);
        }
        /// <summary>
        /// Fully recalculate and redraw the tiles of the tilemap
        /// </summary>
        /// <param name="clearEdges">Whether the edges of the tilemap should get cleared (needed for chunks to allow seemless transitions</param>
        /// <param name="paced">If true, the tiles are drawn at given rate per frame, otherwise draws all tiles at once(causes significant lag for large maps)</param>
        /// <param name="rate">The rate to use for paced tile drawing (tiles per game frame)</param>
        public void FullRedraw(bool clearEdges = true, bool paced = true, int rate = 16)
        {
            this.clearEdges = clearEdges;
            this.paced = paced;
            this.drawRate = rate;
            SetProcess(false);
            Task scan = Task.Run(ScanTiles);
        }
        /// <summary>
        /// Builds the tileset so that the draw tilemaps can draw tiles
        /// </summary>
        protected void BuildDisplayTileset()
        {
            lock (padLock2)
            {
                displayTileMapLayers.Add(displayTileMapLayer1);
                displayTileMapLayers.Add(displayTileMapLayer2);
                displayTileMapLayers.Add(displayTileMapLayer3);
                displayTileMapLayers.Add(displayTileMapLayer4);
                // New tileset and dictionary to clear data
                displayTileset = new TileSet();
                List<TileDefResource> tilemaps = new List<TileDefResource>(TilesetResources);
                foreach (TileDefResource map in tilemaps)
                {
                    int sceID = displayTileset.GetNextSourceId();
                    displayTileset.AddSource(map.tileSetSource, sceID);
                    Tuple<int, TileDefResource> vE = new Tuple<int, TileDefResource>(sceID, map);
                    int sourceInt = DualGridTileSourceManager.GetAtlasSource(map.placeholderSource);
                    TileSetSource sce = TileSet.GetSource(sourceInt);
                    foreach (AtlasTileData d in map.tileSubsets)
                    {
                        Tuple<TileSetSource, Vector2I, ETileSubset> ke = new(sce, d.atlasPosition, d.subset);
                        if (tilesetDict.ContainsKey(ke))
                        {
                            GD.Print("DualGridTilemap: Key already present: " + ke);
                            continue;
                        }
                        tilesetDict[ke] = vE;
                    }
                }
                displayTileMapLayer1.TileSet = displayTileset;
                displayTileMapLayer2.TileSet = displayTileset;
                displayTileMapLayer3.TileSet = displayTileset;
                displayTileMapLayer4.TileSet = displayTileset;
            }
        }

        public virtual void ClearTilemapEdges()
        {
            Vector2I cell;
            Rect2I usedTiles = GetUsedRect();
            int StartX = usedTiles.Position.X;
            int StartY = usedTiles.Position.Y;
            int SizeX = usedTiles.Size.X;
            int SizeY = usedTiles.Size.Y;
            GD.Print("Start X: " + StartX + " Start Y: " + StartY);
            GD.Print("Size X: " + SizeX + " Size Y: " + SizeY);
            // loop through left edge
            for (int i = -2; i < usedTiles.Size.Y+2; i++)
            {
                cell = new(StartX, i+StartY);
                GD.Print(cell);
                displayTileMapLayer1.EraseCell(cell);
                displayTileMapLayer2.EraseCell(cell);
                displayTileMapLayer3.EraseCell(cell);
                displayTileMapLayer4.EraseCell(cell);
            }
            // loop through bottom edge
            for (int i = -2; i < usedTiles.Size.X+2; i++)
            {
                cell = new(StartX+i, StartY+SizeY);
                displayTileMapLayer1.EraseCell(cell);
                displayTileMapLayer2.EraseCell(cell);
                displayTileMapLayer3.EraseCell(cell);
                displayTileMapLayer4.EraseCell(cell);
            }
            // loop through right edge
            for (int i = -2; i < usedTiles.Size.Y+2; i++)
            {
                cell = new(StartX + SizeX, StartY+i);
                displayTileMapLayer1.EraseCell(cell);
                displayTileMapLayer2.EraseCell(cell);
                displayTileMapLayer3.EraseCell(cell);
                displayTileMapLayer4.EraseCell(cell);
            }
            // loop through top edge
            for (int i = -2; i < usedTiles.Size.X+2; i++)
            {
                cell = new(StartX+i, StartY);
                displayTileMapLayer1.EraseCell(cell);
                displayTileMapLayer2.EraseCell(cell);
                displayTileMapLayer3.EraseCell(cell);
                displayTileMapLayer4.EraseCell(cell);
            }
        }
        
        protected void SetTilesPaced(int tilesPerFrame)
        {
            if (tilesSet.Count == 0)
            {
                //GD.Print("DualGridTilemap: Finished setting tiles: killing process loop");
                SetProcess(false);
                DrawComplete();
                return;
            }
            int indexer = 0;
            while (indexer < tilesSet.Count() && indexer < tilesPerFrame)
            {
                drawn = false;
                SetTile(tilesSet[indexer]);
                indexer++;
            }
            tilesSet.RemoveRange(0, indexer);
        }
        /// <summary>
        /// Iterates over the placeholder tiles and calls <seealso cref="SetTile(List{TileSetArgs})"/>.<br/>
        /// Calls <seealso cref="DrawComplete"/> when complete.
        /// </summary>
        protected void SetTiles()
        {
            foreach (KeyValuePair<Vector2I, List<TileSetArgs>> entry in tileDict)
            {
                SetTile(entry.Value);
            }
            DrawComplete();
        }
        /// <summary>
        /// Tells the <seealso cref="ChunkManager"/> that the tilemap is done drawing so the next one may be started.<br/>
        /// Also clears the tile edges if required.
        /// </summary>
        protected void DrawComplete()
        {
            if (clearEdges) { ClearTilemapEdges(); }
        }
        /// <summary>
        /// Gets the <seealso cref="TileDefResource"/> of given <seealso cref="TileSetSource"/> and placeholder tile
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        protected Tuple<int, TileDefResource, ETileSubset> GetTileSetFromDict(Tuple<TileSetSource, Vector2I> key)
        {
            for (int i = 0; i < Enum.GetNames(typeof(ETileSubset)).Length; i++)
            {
                if (tilesetDict.TryGetValue(new(key.Item1, key.Item2, (ETileSubset)i), out Tuple<int, TileDefResource> v))
                {
                    return new(v.Item1, v.Item2, (ETileSubset)i);
                }
            }
            return null;
        }
        /// <summary>
        /// Sets the visuals of a given tile
        /// </summary>
        /// <param name="tile"> The tile(map coordinates) which is getting set</param>
        /// <param name="searchTiles"> The tiles(map coordinates) which are valid neighbors</param>
        /// <param name="resourceTilemap">The tile resource which the tile gets pulled from</param>
        /// <param name="sourceID">The source ID for the tilemap draw source</param>
        /// <param name="tileSubset">The tile subset to determine tile interactivity</param>
        protected void ProcessTile(Vector2I tile, GridTileNeighbors neighbors, TileDefResource resourceTilemap, int sourceID, ETileSubset tileSubset)
        {
            lock (padLock)
            {
                Tuple<GridTileNeighbors, ETileSubset> key = new Tuple<GridTileNeighbors, ETileSubset>(neighbors, tileSubset);
                List<TileSetArgs> tileSetArgs = new List<TileSetArgs>();
                if (tileDict.ContainsKey(tile)) { tileSetArgs = tileDict[tile]; }
                Vector2I drawTile = MasterAtlasCoordLookup(key);
                {
                    TileSetArgs newTile = new TileSetArgs(resourceTilemap.hierarchy, sourceID, drawTile, tile, neighbors);
                    //GD.Print("Result: " + drawTile + " from subset: " + tileSubset);
                    foreach (TileSetArgs t in tileSetArgs) { if (t.ISADuplicate(newTile.neighbors)) return; }
                    tileSetArgs.Add(newTile);
                }
                tileDict[tile] = tileSetArgs;
            }
        }
        /// <summary>
        /// Scans the tilemap for placeholder tiles and creates the HashSets of tiles which get iterated for drawing<br/>
        /// Calls <seealso cref="DetermineDrawTiles"/> when complete
        /// </summary>
        protected virtual void ScanTiles()
        {
            int sources = TileSet.GetSourceCount();
            for (int j = 0; j < sources; j++)
            {
                TileSetSource sce = TileSet.GetSource(j);
                int tileCt = sce.GetTilesCount();
                for (int i = 0; i < tileCt; i++)
                {
                    Vector2I currentTile = sce.GetTileId(i);
                    Godot.Collections.Array<Vector2I> placeholderTiles = GetUsedCellsById(j, currentTile);
                    if (placeholderTiles.Count > 0)
                    {
                        HashSet<Vector2I> th = new HashSet<Vector2I>(placeholderTiles.Count);
                        foreach (Vector2I t in placeholderTiles)
                        {
                            th.Add(t);
                        }
                        lock (padLock2)
                        {
                            PlaceholderTileKey key = new PlaceholderTileKey(j, currentTile);
                            if (placeholderTileHashDict.ContainsKey(key))
                            {
                                th.UnionWith(placeholderTileHashDict[key]);
                            } 
                            placeholderTileHashDict[key] = th;
                        }
                    }
                }
            }
            DetermineDrawTiles();
        }
        /// <summary>
        /// Iterates over tiles and calculates the required visual tiles to draw.<br/>
        /// Calls <seealso cref="DrawTiles(bool, int)"/> when complete.
        /// </summary>
        protected void DetermineDrawTiles()
        {
            Stopwatch sw = Stopwatch.StartNew();
            lock (padLock)
            {
                tileDict.Clear();
                // Iterate over each type of placeholder tile
                foreach (KeyValuePair<PlaceholderTileKey, HashSet<Vector2I>> entry in placeholderTileHashDict)
                {
                    int atlasSourceID = entry.Key.source;
                    TileSetSource tileAtlasSource = TileSet.GetSource(atlasSourceID);
                    Tuple<int, TileDefResource, ETileSubset> tilemapValue = GetTileSetFromDict(new Tuple<TileSetSource, Vector2I>(tileAtlasSource, entry.Key.tile));
                    if (tilemapValue != null)
                    {
                        int drawSource = tilemapValue.Item1;
                        TileDefResource resourceTilemap = tilemapValue.Item2;
                        HashSet<Vector2I> setTiles = entry.Value;
                        HashSet<Vector2I> searchTiles = new HashSet<Vector2I>();
                        // Combine setTiles with any subtiles from the tilemap resource
                        foreach (AtlasTileData d in resourceTilemap.tileSubsets) { if (placeholderTileHashDict.TryGetValue(new(atlasSourceID, d.atlasPosition), out HashSet<Vector2I> tiles)) { searchTiles.UnionWith(tiles); } }
                        foreach (Vector2I tile in setTiles)
                        {
                            Vector2I BRCell = tile;
                            Vector2I TRCell = tile + NEIGHBOURS[2];
                            Vector2I TLCell = tile + NEIGHBOURS[3];
                            Vector2I BLCell = tile + NEIGHBOURS[1];
                            GridTileNeighbors BRN = GetTileNeighbors(BRCell, searchTiles);
                            ProcessTile(BRCell, BRN, resourceTilemap, drawSource, tilemapValue.Item3);
                            // Only process tiles if they are not part of the search tiles. Improves speed significantly and reduces duplicate tiles
                            if (!searchTiles.Contains(TLCell))
                            {
                                GridTileNeighbors TLN = GetTileNeighbors(TLCell, searchTiles);
                                ProcessTile(TLCell, TLN, resourceTilemap, drawSource, tilemapValue.Item3);
                            }
                            if (!searchTiles.Contains(TRCell))
                            {
                                GridTileNeighbors TRN = GetTileNeighbors(TRCell, searchTiles);
                                ProcessTile(TRCell, TRN, resourceTilemap, drawSource, tilemapValue.Item3);
                            }
                            if (!searchTiles.Contains(BLCell))
                            {
                                GridTileNeighbors BLN = GetTileNeighbors(BLCell, searchTiles);
                                ProcessTile(BLCell, BLN, resourceTilemap, drawSource, tilemapValue.Item3);
                            }
                        }
                    }
                    tilesSet.AddRange(tileDict.Values);
                }
                CallDeferred("DrawTiles", paced, drawRate);
                sw.Stop();
            }
            GD.Print("DualGridTilemap: Time to determine draw tiles: " + sw.ElapsedMilliseconds + "ms");
        }
        /// <summary>
        /// Draw the currently calculated tiles.<br/>
        /// Calls <seealso cref="SetTiles"/> when complete.<br/>
        /// </summary>
        /// <param name="paced">If true, the tiles are drawn at given rate per frame, otherwise draws all tiles at once(causes significant lag)</param>
        /// <param name="rate">The rate to use for paced tile drawing (tiles per frame)</param>
        protected void DrawTiles(bool paced = true, int rate = 16)
        {
            if (paced)
            {
                SetProcess(true);
                drawRate = rate;
                return;
            }
            SetTiles();
        }
        protected GridTileNeighbors GetTileNeighbors(Vector2I tileCell, HashSet<Vector2I> tiles)
        {
            Vector2I TRCell = tileCell - NEIGHBOURS[2];
            Vector2I TLCell = tileCell - NEIGHBOURS[3];
            Vector2I BRCell = tileCell + NEIGHBOURS[0];
            Vector2I BLCell = tileCell - NEIGHBOURS[1];
            bool TL = tiles.Contains(TLCell);
            bool TR = tiles.Contains(TRCell);
            bool BL = tiles.Contains(BLCell);
            bool BR = tiles.Contains(BRCell);
            //GD.Print("Processing: ",tileCell, BLCell, TRCell, TLCell, BR, BL, TR, TL);
            return new GridTileNeighbors(TL, TR, BL, BR);
        }
        protected void SetTile(List<TileSetArgs> tiles)
        {
            lock (padLock)
            {
                int indexer = 0;
                tiles = tiles.OrderByDescending(o => o.sortOrder).ToList();
                foreach (TileSetArgs args in tiles)
                {
                    TileMapLayer layer = displayTileMapLayers[indexer];
                    layer.SetCell(args.TilePosition, args.source, args.AtlasPosition);
                    indexer++;
                }
            }
        }
        /// <summary>
        /// Returns the atlas tile coordinates of the tile which needs to be drawn based on the given neighbors and subset<br/>
        /// <seealso cref="GridTileNeighbors!="/><br/>
        /// <seealso cref="ETileSubset!="/><br/>
        /// </summary>
        protected static Vector2I MasterAtlasCoordLookup(Tuple<GridTileNeighbors, ETileSubset> key)
        {
            switch (key.Item2)
            {
                case ETileSubset.Base:
                    return StandardTileDict[key];
                case ETileSubset.Path:
                    return PathTileDict[key];
                case ETileSubset.WallTopper:
                    return WallTopperDict[key];
                case ETileSubset.WallFilledL:
                    return WallFilledLDict[key];
                case ETileSubset.WallFilledR:
                    return WallFilledRDict[key];
            }
            return new Vector2I(0, 0);
        }
        /// <summary>
        /// The tile set source and atlas tile position of a placeholder tile
        /// <code>
        /// int source;     the tileSet source ID of the placeholder tile
        /// Vector2I tile;  the tileSet atlas coordinates
        /// </code>
        /// </summary>
        public struct PlaceholderTileKey
        {
            public int source;
            public Vector2I tile;
            public PlaceholderTileKey(int source, Vector2I tile)
            {
                this.source = source;
                this.tile = tile;
            }
        }
        protected static readonly Vector2I[] NEIGHBOURS = new Vector2I[]
        {
        new Vector2I(0, 0),
        new Vector2I(1, 0),
        new Vector2I(0, 1),
        new Vector2I(1, 1)
        };
        #region Tile subset definitions: each dictionary corresponds to a tile subset and how those tiles are selected. Use this to decide how tiles interlace
        private static readonly Dictionary<Tuple<GridTileNeighbors, ETileSubset>, Vector2I> StandardTileDict = new Dictionary<Tuple<GridTileNeighbors, ETileSubset>, Vector2I>
    {
        {new (new (true,true,true,true) ,ETileSubset.Base), new Vector2I(2,1)},   // FULL
        {new (new (false,false,false,true) ,ETileSubset.Base), new Vector2I (1, 3)}, // OUTER_BOTTOM_RIGHT
        {new (new (false, false, true, false) ,ETileSubset.Base), new Vector2I (0, 0)}, // OUTER_BOTTOM_LEFT
        {new (new (false,true,false,false) ,ETileSubset.Base), new Vector2I (0, 2)}, // OUTER_TOP_RIGHT
        {new (new (true,false,false,false) ,ETileSubset.Base), new Vector2I (3, 3)}, // OUTER_TOP_LEFT
        {new (new (false,true,false,true) ,ETileSubset.Base), new Vector2I (1, 0)}, // EDGE_RIGHT
        {new (new (true,false,true,false) ,ETileSubset.Base), new Vector2I (3, 2)}, // EDGE_LEFT
        {new (new (false,false,true,true) ,ETileSubset.Base), new Vector2I (3, 0)}, // EDGE_BOTTOM
        {new (new (true,true,false,false) ,ETileSubset.Base), new Vector2I (1, 2)}, // EDGE_TOP
        {new (new (false, true, true, true) ,ETileSubset.Base), new Vector2I (1, 1)}, // INNER_BOTTOM_RIGHT
        {new (new (true,false,true,true) ,ETileSubset.Base), new Vector2I (2, 0)}, // INNER_BOTTOM_LEFT
        {new (new (true,true,false,true) ,ETileSubset.Base), new Vector2I (2, 2)}, // INNER_TOP_RIGHT
        {new (new (true,true,true,false) ,ETileSubset.Base), new Vector2I (3, 1)}, // INNER_TOP_LEFT
        {new (new (false,true,true,false) ,ETileSubset.Base), new Vector2I (2, 3)}, // DUAL_UP_RIGHT
        {new (new(true, false, false, true),ETileSubset.Base), new Vector2I (0, 1)}, // DUAL_DOWN_RIGHT
    };

        private static readonly Dictionary<Tuple<GridTileNeighbors, ETileSubset>, Vector2I> PathTileDict = new Dictionary<Tuple<GridTileNeighbors, ETileSubset>, Vector2I>
    {
        {new (new (true,true,true,true), ETileSubset.Path), new Vector2I(2,1)},  // FULL
        {new (new(false, false, false, true), ETileSubset.Path), new Vector2I (0, 4)}, // OUTER_BOTTOM_RIGHT
        {new (new(false, false, true, false), ETileSubset.Path), new Vector2I (1, 4)}, // OUTER_BOTTOM_LEFT
        {new (new(false, true, false, false), ETileSubset.Path), new Vector2I (0, 5)}, // OUTER_TOP_RIGHT
        {new (new(true, false, false, false), ETileSubset.Path), new Vector2I (1, 5)}, // OUTER_TOP_LEFT
        {new (new(false, true, true, true), ETileSubset.Path), new Vector2I (3, 5)}, // INNER_BOTTOM_RIGHT
        {new (new(true, false, true, true), ETileSubset.Path), new Vector2I (2, 5)}, // INNER_BOTTOM_LEFT
        {new (new(true, true, false, true), ETileSubset.Path), new Vector2I (3, 4)}, // INNER_TOP_RIGHT
        {new (new(true, true, true, false), ETileSubset.Path), new Vector2I (2, 4)}, // INNER_TOP_LEFT
        {new (new(false, true, true, false), ETileSubset.Path), new Vector2I (1, 6)}, // DUAL_UP_RIGHT
        {new (new(true, false, false, true), ETileSubset.Path), new Vector2I (0, 6)}, // DUAL_DOWN_RIGHT
        {new (new(false, true, false, true), ETileSubset.Path), new Vector2I (1, 0)}, // EDGE_RIGHT
        {new (new(true, false, true, false), ETileSubset.Path), new Vector2I (3, 2)}, // EDGE_LEFT
        {new (new(false, false, true, true), ETileSubset.Path), new Vector2I (3, 0)}, // EDGE_BOTTOM
        {new (new(true, true, false, false), ETileSubset.Path), new Vector2I (1, 2)}, // EDGE_TOP
    };

        public static readonly Dictionary<Tuple<GridTileNeighbors, ETileSubset>, Vector2I> WallTopperDict = new Dictionary<Tuple<GridTileNeighbors, ETileSubset>, Vector2I>
    {
        {new (new (true,true,true,true) ,ETileSubset.WallTopper), new Vector2I(2,5)},   // FULL
        {new (new (false,false,false,true) ,ETileSubset.WallTopper), new Vector2I (1, 3)}, // OUTER_BOTTOM_RIGHT
        {new (new (false, false, true, false) ,ETileSubset.WallTopper), new Vector2I (0, 0)}, // OUTER_BOTTOM_LEFT
        {new (new (false,true,false,false) ,ETileSubset.WallTopper), new Vector2I (0, 2)}, // OUTER_TOP_RIGHT
        {new (new (true,false,false,false) ,ETileSubset.WallTopper), new Vector2I (3, 3)}, // OUTER_TOP_LEFT
        {new (new (false,true,false,true) ,ETileSubset.WallTopper), new Vector2I (1, 4)}, // EDGE_RIGHT
        {new (new (true,false,true,false) ,ETileSubset.WallTopper), new Vector2I (3, 6)}, // EDGE_LEFT
        {new (new (false,false,true,true) ,ETileSubset.WallTopper), new Vector2I (3, 0)}, // EDGE_BOTTOM
        {new (new (true,true,false,false) ,ETileSubset.WallTopper), new Vector2I (1, 6)}, // EDGE_TOP
        {new (new (false, true, true, true) ,ETileSubset.WallTopper), new Vector2I (1, 5)}, // INNER_BOTTOM_RIGHT
        {new (new (true,false,true,true) ,ETileSubset.WallTopper), new Vector2I (2, 4)}, // INNER_BOTTOM_LEFT
        {new (new (true,true,false,true) ,ETileSubset.WallTopper), new Vector2I (2, 6)}, // INNER_TOP_RIGHT
        {new (new (true,true,true,false) ,ETileSubset.WallTopper), new Vector2I (3, 5)}, // INNER_TOP_LEFT
        {new (new (false,true,true,false) ,ETileSubset.WallTopper), new Vector2I (2, 3)}, // DUAL_UP_RIGHT
        {new (new(true, false, false, true),ETileSubset.WallTopper), new Vector2I (0, 1)}, // DUAL_DOWN_RIGHT
    };

        public static readonly Dictionary<Tuple<GridTileNeighbors, ETileSubset>, Vector2I> WallFilledLDict = new Dictionary<Tuple<GridTileNeighbors, ETileSubset>, Vector2I>
    {
        {new (new (true,true,true,true) ,ETileSubset.WallFilledL), new Vector2I(0,4)},   // FULL
        {new (new (false,false,false,true) ,ETileSubset.WallFilledL), new Vector2I (1, 3)}, // OUTER_BOTTOM_RIGHT
        {new (new (false, false, true, false) ,ETileSubset.WallFilledL), new Vector2I (0, 0)}, // OUTER_BOTTOM_LEFT
        {new (new (false,true,false,false) ,ETileSubset.WallFilledL), new Vector2I (0, 2)}, // OUTER_TOP_RIGHT
        {new (new (true,false,false,false) ,ETileSubset.WallFilledL), new Vector2I (3, 3)}, // OUTER_TOP_LEFT
        {new (new (false,true,false,true) ,ETileSubset.WallFilledL), new Vector2I (1, 4)}, // EDGE_RIGHT
        {new (new (true,false,true,false) ,ETileSubset.WallFilledL), new Vector2I (3, 6)}, // EDGE_LEFT
        {new (new (false,false,true,true) ,ETileSubset.WallFilledL), new Vector2I (3, 0)}, // EDGE_BOTTOM
        {new (new (true,true,false,false) ,ETileSubset.WallFilledL), new Vector2I (1, 2)}, // EDGE_TOP
        {new (new (false, true, true, true) ,ETileSubset.WallFilledL), new Vector2I (1, 5)}, // INNER_BOTTOM_RIGHT
        {new (new (true,false,true,true) ,ETileSubset.WallFilledL), new Vector2I (2, 4)}, // INNER_BOTTOM_LEFT
        {new (new (true,true,false,true) ,ETileSubset.WallFilledL), new Vector2I (2, 6)}, // INNER_TOP_RIGHT
        {new (new (true,true,true,false) ,ETileSubset.WallFilledL), new Vector2I (3, 5)}, // INNER_TOP_LEFT
        {new (new (false,true,true,false) ,ETileSubset.WallFilledL), new Vector2I (2, 3)}, // DUAL_UP_RIGHT
        {new (new(true, false, false, true),ETileSubset.WallFilledL), new Vector2I (0, 1)}, // DUAL_DOWN_RIGHT
    };

        public static readonly Dictionary<Tuple<GridTileNeighbors, ETileSubset>, Vector2I> WallFilledRDict = new Dictionary<Tuple<GridTileNeighbors, ETileSubset>, Vector2I>
    {
        {new (new (true,true,true,true) ,ETileSubset.WallFilledR), new Vector2I(0,5)},   // FULL
        {new (new (false,false,false,true) ,ETileSubset.WallFilledR), new Vector2I (1, 3)}, // OUTER_BOTTOM_RIGHT
        {new (new (false, false, true, false) ,ETileSubset.WallFilledR), new Vector2I (0, 0)}, // OUTER_BOTTOM_LEFT
        {new (new (false,true,false,false) ,ETileSubset.WallFilledR), new Vector2I (0, 2)}, // OUTER_TOP_RIGHT
        {new (new (true,false,false,false) ,ETileSubset.WallFilledR), new Vector2I (3, 3)}, // OUTER_TOP_LEFT
        {new (new (false,true,false,true) ,ETileSubset.WallFilledR), new Vector2I (1, 4)}, // EDGE_RIGHT
        {new (new (true,false,true,false) ,ETileSubset.WallFilledR), new Vector2I (3, 6)}, // EDGE_LEFT
        {new (new (false,false,true,true) ,ETileSubset.WallFilledR), new Vector2I (3, 0)}, // EDGE_BOTTOM
        {new (new (true,true,false,false) ,ETileSubset.WallFilledR), new Vector2I (1, 2)}, // EDGE_TOP
        {new (new (false, true, true, true) ,ETileSubset.WallFilledR), new Vector2I (1, 5)}, // INNER_BOTTOM_RIGHT
        {new (new (true,false,true,true) ,ETileSubset.WallFilledR), new Vector2I (2, 4)}, // INNER_BOTTOM_LEFT
        {new (new (true,true,false,true) ,ETileSubset.WallFilledR), new Vector2I (2, 6)}, // INNER_TOP_RIGHT
        {new (new (true,true,true,false) ,ETileSubset.WallFilledR), new Vector2I (3, 5)}, // INNER_TOP_LEFT
        {new (new (false,true,true,false) ,ETileSubset.WallFilledR), new Vector2I (2, 3)}, // DUAL_UP_RIGHT
        {new (new(true, false, false, true),ETileSubset.WallFilledR), new Vector2I (0, 1)}, // DUAL_DOWN_RIGHT
    };
        #endregion
    }

    /// <summary>
    /// Tile set source, atlas position, tile position, and sort order of a tile used when setting the tile on a tilemaplayer.
    /// <code>
    /// int source;
    /// Vector2I AtlasPosition;
    /// Vector2I TilePosition;
    /// int sortOrder;
    /// GridTileNeighbors neighbors;
    /// 
    /// bool ISADuplicate(GridTileNeighbors neighbors)
    /// </code>
    /// <seealso cref="GridTileNeighbors=="/>
    /// </summary>
    public struct TileSetArgs
    {
        public int source;
        public Vector2I AtlasPosition;
        public Vector2I TilePosition;
        public int sortOrder;
        public GridTileNeighbors neighbors;
        public TileSetArgs(int ord, int sce, Vector2I Apos, Vector2I Tpos, GridTileNeighbors neighbors)
        {
            sortOrder = ord;
            source = sce;
            AtlasPosition = Apos;
            TilePosition = Tpos;
            this.neighbors = neighbors;
        }
        public bool ISADuplicate(GridTileNeighbors neighbors)
        {
            if (neighbors.TL != this.neighbors.TL) { return false; }
            if (neighbors.BL != this.neighbors.BL) { return false; }
            if (neighbors.TR != this.neighbors.TR) { return false; }
            if (neighbors.BR != this.neighbors.BR) { return false; }
            return true;
        }
    }

    /// <summary>
    /// The corner neighbors of a tile to determine what tile gets set
    /// <code>
    /// bool TL;    top left neighbor exists
    /// bool TR;    top right neighbor exists
    /// bool BL;    bottom left neighbor exists
    /// bool BR;    bottom right neighbor exists
    /// </code>
    /// </summary>
    public struct GridTileNeighbors
    {
        public bool TL;
        public bool TR;
        public bool BL;
        public bool BR;
        public GridTileNeighbors(bool TL, bool TR, bool BL, bool BR)
        {
            this.TL = TL;
            this.TR = TR;
            this.BL = BL;
            this.BR = BR;
        }
    }
    public enum EPlaceholderSources
    {
        Natural,
        Stone,
        Windows,
    }
    public enum ETileSubset
    {
        Base,
        Path,
        WallTopper,
        WallFilledL,
        WallFilledR
    }
}