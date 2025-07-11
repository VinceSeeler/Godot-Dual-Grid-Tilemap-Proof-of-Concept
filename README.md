# Godot-Dual-Grid-Tilemap-Proof-of-Concept
This project was made based on what was shown in jess::codes's video on the topic:
https://www.youtube.com/watch?v=jEWFSv3ivTg&t=146s

In Jess's video she only shows drawing grass and dirt tiles, this implementation uses four "Display TileMaps" allowing for any number of different TileSets to be used. 

The other main extension shown in this project is an alternate pattern system. This allows the definition of alternate TileSets which are drawn together with their base TileSet. The most obvious use of this is the ability to define square-cornered and diagonal or rounded cornered TileSets which work with each other on the grid. 

The final feature of the project is that TileMaps can trim their edges. In the project I am working on I am using a chunk system and needed to be able to split up TileMaps and have seamless transitions. By making a chunk 1 tile larger in all directions and trimming it, you can achieve this smooth transition. 

The only two resources which must be configured are the Placeholder TileSet, and the TileDefResource. 

The Placeholder TileSet is the TileSet which you use to draw your levels which the TileDefResources reference to decide if a tile should be drawn. 

In the TileDefResource you must specify the Atlas you are using (PlaceholderSource Enum), a TileSet containing your 15-Tile map, and your Tile Subsets. The Tile Subsets are where you define what Tile on the placeholder map is used to identify your TileSet. This is also where you identify your alternate patterns such as Paths, Roofs, etc. 

The main limitation of this system is that it is quite expensive to determine the draw tiles of large TileMaps. If anybody has recommendations about how to improve  the system I would be glad to hear it. 
A system for creating Dual-Grid Tilemaps in the Godot Game Engine

Edit 2025-07-11
In translating this from the original project I forgot to re-add the offset of the display grid. This has been fixed
