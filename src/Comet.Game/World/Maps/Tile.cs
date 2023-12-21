// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Tile.cs
// Description:
// 
// Creator: FELIPEVIEIRAVENDRAMI [FELIPE VIEIRA VENDRAMINI]
// 
// Developed by:
// Felipe Vieira Vendramini <felipevendramini@live.com>
// 
// Programming today is a race between software engineers striving to build bigger and better
// idiot-proof programs, and the Universe trying to produce bigger and better idiots.
// So far, the Universe is winning.
// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Runtime.InteropServices;

namespace Comet.Game.World.Maps
{
    /// <summary>
    ///     This structure encapsulates a tile from the floor's coordinate grid. It contains the tile access information
    ///     and the elevation of the tile. The map's coordinate grid is composed of these tiles. The tile structure
    ///     is not optimized by C#, and thus takes up 48 bits of memory (or 6 bytes).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public struct Tile
    {
        public short Access; // The access type for processing the tile.
        public short Elevation; // The elevation of the tile on the map.
        public short Surface;

        /// <summary>
        ///     This structure encapsulates a tile from the floor's coordinate grid. It contains the tile access information
        ///     and the elevation of the tile. The map's coordinate grid is composed of these tiles. The tile structure
        ///     is not optimized by C#, and thus takes up 24 bits of memory (or 3 bytes).
        /// </summary>
        /// <param name="type">The access type for processing the tile.</param>
        /// <param name="elevation">The elevation of the tile on the map.</param>
        /// <param name="surface"></param>
        public Tile(short elevation, short access, short surface)
        {
            Access = access;
            Elevation = elevation;
            Surface = surface;
        }

        public bool IsAccessible()
        {
            return Access != 1;
        }

        public bool IsBoothEnable()
        {
            return Surface == 16;
        }

        public short GetAltitude()
        {
            return Elevation;
        }
    }

    /// <summary> This enumeration type defines the access types for tiles. </summary>
    public enum TileType : byte
    {
        Terrain,
        Npc,
        Monster,
        Portal,
        Item,
        MarketSpot,
        Available
    }

    /// <summary> This enumeration type defines the types of scenery files used by the client. </summary>
    public enum SceneryType
    {
        SceneryObject = 1,
        DdsCover = 4,
        Effect = 10,
        Sound = 15
    }
}