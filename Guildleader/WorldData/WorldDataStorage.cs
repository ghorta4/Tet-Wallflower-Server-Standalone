using System;
using System.Collections.Generic;
using System.Threading;

namespace Guildleader
{
    public static class WorldManager
    {
        public static WorldDataStorageModuleGeneric currentWorld;

    }

    //foreword: This module was adapted from an older project of mine intended to be used for a 3-d world; and, as such, z layers exist here.
    //in practice for this project, Z layers in individual chunks refer to the floor/wall/ceiling layer, whereas Z layers
    //in the chunk world storage module refer to different "zones" (IE, Z layer 0 is the overworld, -1 can be saved for dungeons, etc.
    public abstract class WorldDataStorageModuleGeneric //functions both the client and server will need to store world information
    {
        public int seed;

       // public static Dictionary<uint, Entity> allEntities = new Dictionary<uint, Entity> { };

        public Dictionary<int, Dictionary<int, Dictionary<int, Chunk>>> allChunks = new Dictionary<int, Dictionary<int, Dictionary<int, Chunk>>> { };
        public static int worldMinx = -500, worldMaxx = 500, worldMiny = -500, worldMaxy = 500, worldMinz = -100, worldMaxz = 50;
        public const int worldStartSizeX = 10, worldStartSizeY = 10, worldStartSizeZ = 0;

        public const float worldTileSize = 0.1f;


        public void SaveAllChunks()
        {
            foreach (var xdic in allChunks)
            {
                foreach (var ydic in xdic.Value)
                {
                    foreach (var zdic in ydic.Value)
                    {
                        SaveChunkData(xdic.Key, ydic.Key, zdic.Key, zdic.Value);
                    }
                }
            }
        }
    
        public Chunk GetChunkData(int xPos, int yPos, int zPos)
        {
            InitializeChunkInfoPath();
            string fileName = GetNameBasedOnPosition(xPos, yPos, zPos);
            string chunkPath = FileAccess.ChunkStorageName + fileName;
            Chunk chu = null;

            bool hasXpos = allChunks.ContainsKey(xPos);
            bool hasYpos = hasXpos && allChunks[xPos].ContainsKey(yPos);

            if (hasXpos && hasYpos && allChunks[xPos][yPos].ContainsKey(zPos))
            {
                return allChunks[xPos][yPos][zPos];
            }

            if (!FileAccess.FileExists(chunkPath))
            {
                chu = Chunk.SpawnNewChunk(xPos, yPos, zPos, 0);
            }
            else
            {
                byte[] chunkData = FileAccess.LoadFile(chunkPath);
                chu = Chunk.getChunkFromBytes(chunkData, new Int3(xPos, yPos, zPos));
            }
            if (!hasXpos)
            {
                allChunks[xPos] = new Dictionary<int, Dictionary<int, Chunk>>();
            }
            if (!hasYpos)
            {
                allChunks[xPos][yPos] = new Dictionary<int, Chunk>();
            }
            try
            {
                allChunks[xPos][yPos][zPos] = chu;
            }
            catch (Exception e)
            {
                ErrorHandler.AddErrorToLog(e);
            }

            return chu;
        }
        public Chunk[] GetChunksInArea(int xPos, int yPos, int zPos, int xSearch, int ySearch, int zSearch)
        {
            List<Chunk> inArea = new List<Chunk>();
            for (int x = xPos - xSearch; x <= xPos + xSearch; x++)
            {
                for (int y = yPos - ySearch; y <= yPos + ySearch; y++)
                {
                    for (int z = zPos - zSearch; z <= zPos + zSearch; z++)
                    {
                        inArea.Add(GetChunkData(x,y,z));
                    }
                }
            }
            return inArea.ToArray();
        }
        public void SaveChunkData(Int3 pos, Chunk chu)
        {
            SaveChunkData(pos.x, pos.y, pos.z, chu);
        }
        public void SaveChunkData(int xPos, int yPos, int zPos, Chunk chu)
        {
            InitializeChunkInfoPath();
            string fileName = GetNameBasedOnPosition(xPos, yPos, zPos);
            FileAccess.WriteBytesInsideCurrentDefaultDirectoryInSubfolder(chu.convertChunkToBytes(), fileName, FileAccess.ChunkStorageName);
        }
        public void UnloadDistantChunkData(Int3 chunkPos, int cutoffRange)
        {
            List<Int3> chunksToRemove = new List<Int3>();
            foreach (var xdic in allChunks)
            {
                foreach (var ydic in xdic.Value)
                {
                    foreach (var zdic in ydic.Value)
                    {
                        int dist = Math.Min(Math.Min(Math.Abs(chunkPos.x - xdic.Key), Math.Abs(chunkPos.y - ydic.Key)), Math.Abs(chunkPos.z - zdic.Key) - 1);
                        if (dist > cutoffRange)
                        {
                            chunksToRemove.Add(new Int3(xdic.Key, ydic.Key, zdic.Key));
                        }
                    }
                }
            }

            foreach (Int3 toRemove in chunksToRemove)
            {
                SaveChunkData(toRemove, allChunks[toRemove.x][toRemove.y][toRemove.z]);
                allChunks[toRemove.x][toRemove.y].Remove(toRemove.z);
            }

            List<int> subDictsToRemovea = new List<int>();
            List<int> subDictsToRemoveb = new List<int>();
            foreach (var xdic in allChunks)
            {
                foreach (var ydic in xdic.Value)
                {
                    if (ydic.Value.Count == 0)
                    {
                        subDictsToRemovea.Add(ydic.Key);
                    }
                }
                foreach (int key in subDictsToRemovea)
                {
                    xdic.Value.Remove(key);
                }
                subDictsToRemovea.Clear();
                if (xdic.Value.Count == 0)
                {
                    subDictsToRemoveb.Add(xdic.Key);
                }
            }
            foreach (int key in subDictsToRemoveb)
            {
                allChunks.Remove(key);
            }
        }
        public void LoadNearbyChunkData(Int3 chunkPos, int loadDistance)
        {
            for (int x = chunkPos.x - loadDistance; x <= chunkPos.x + loadDistance; x++)
            {
                for (int y = chunkPos.y - loadDistance; y <= chunkPos.y + loadDistance; y++)
                {
                    for (int z = chunkPos.z - loadDistance - 1; z <= chunkPos.z + loadDistance + 1; z++)
                    {
                        Int3 pos = new Int3(x, y, z);
                        if (!allChunks.ContainsKey(x))
                        {
                            allChunks[x] = new Dictionary<int, Dictionary<int, Chunk>>();
                        }
                        if (!allChunks[x].ContainsKey(y))
                        {
                            allChunks[x][y] = new Dictionary<int, Chunk>();
                        }
                        if (!allChunks[x][y].ContainsKey(z))
                        {
                            allChunks[x][y][z] = GetChunkData(x, y, z);
                        }
                    }
                }
            }
        }

        public static Int3 ChunkCenterPosition(Int3 chunkID)
        {
            return new Int3(chunkID.x * Chunk.defaultx, chunkID.y * Chunk.defaulty, chunkID.z * Chunk.defaultz);
        }
        static string GetNameBasedOnPosition(int xPos, int yPos, int zPos)
        {
            return xPos + "v" + yPos + "v" + zPos;
        }
        void InitializeChunkInfoPath()
        {
            FileAccess.PokeDirectoryIntoCurrentDefaultDirectory(FileAccess.ChunkStorageName);
        }

        public SingleWorldTile GetTileAtLocation(Int3 pos)
        {
            return GetTileAtLocation(pos.x, pos.y, pos.z);
        }
        public SingleWorldTile GetTileAtLocation(int x, int y, int z)
        {
            int spaceWithinChunkx = x % Chunk.defaultx;
            int spaceWithinChunky = y % Chunk.defaulty;
            int spaceWithinChunkz = z % Chunk.defaultz;
            if (spaceWithinChunkx < 0)
            {
                spaceWithinChunkx += Chunk.defaultx;
            }
            if (spaceWithinChunky < 0)
            {
                spaceWithinChunky += Chunk.defaulty;
            }
            if (spaceWithinChunkz < 0)
            {
                spaceWithinChunkz += Chunk.defaultz;
            }
            Chunk chunkResult = null;
            Int3 chunkPos = GetChunkPositionBasedOnTilePosition(x, y, z);
            Dictionary<int, Dictionary<int, Chunk>> holdera = new Dictionary<int, Dictionary<int, Chunk>>();
            Dictionary<int, Chunk> holderb = new Dictionary<int, Chunk>();
            bool success = allChunks.TryGetValue(chunkPos.x, out holdera);
            if (success)
            {
                success = holdera.TryGetValue(chunkPos.y, out holderb);
            }
            if (success)
            {
                success = holderb.TryGetValue(chunkPos.z, out chunkResult);
            }
            if (!success)
            {
                SingleWorldTile temp = new SingleWorldTile(5, new Int3(x, y, z));
                return temp;
                // chunkResult = getChunkData(chunkPos.x, chunkPos.y, chunkPos.z);
            }
            if (!chunkResult.hasCompletedPostProcessingThatRequiresNeighbors)
            {
                SingleWorldTile temp = new SingleWorldTile(5, new Int3(x, y, z));
                return temp;
            }
            return chunkResult.tiles[spaceWithinChunkx, spaceWithinChunky, spaceWithinChunkz];
        }
        public SingleWorldTile[,,] GetAllTilesInArea(Int3 corner, Int3 size)
        {
            SingleWorldTile[,,] holster = new SingleWorldTile[size.x, size.y, size.z];
            for (int x = corner.x; x < corner.x + size.x; x++)
            {
                for (int y = corner.y; y < corner.y + size.y; y++)
                {
                    for (int z = corner.z; z < corner.z + size.z; z++)
                    {
                        holster[x - corner.x, y - corner.y, z - corner.z] = GetTileAtLocation(x, y, z);
                    }
                }
            }
            return holster;
        }

        public static Int3 GetChunkPositionBasedOnTilePosition(int x, int y, int z)
        {
            int chunkX = (int)Math.Floor(x / (float)Chunk.defaultx);
            int chunkY = (int)Math.Floor(y / (float)Chunk.defaulty);
            int chunkZ = (int)Math.Floor(z / (float)Chunk.defaultz);
            return new Int3(chunkX, chunkY, chunkZ);
        }
    }

    public class Chunk
    {
        public Int3 chunkPos;
        public bool hasCompletedPostProcessingThatRequiresNeighbors;
        readonly int chunkVersion = 0;

        public const int defaultx = 14, defaulty = 14, defaultz = 3;
        public SingleWorldTile[,,] tiles = new SingleWorldTile[defaultx, defaulty, defaultz];

        public Chunk(Int3 position)
        {
            chunkPos = position;
        }
        public static Chunk SpawnNewChunk(int x, int y, int z, int gereationType)
        {
            Chunk temp = new Chunk(new Int3(x, y, z));
            if (z == 0 && x < WorldDataStorageModuleGeneric.worldMaxx && x > WorldDataStorageModuleGeneric.worldMinx && y > WorldDataStorageModuleGeneric.worldMiny && y < WorldDataStorageModuleGeneric.worldMaxy)
            {
                temp.initializeNormally(0);
            }
            else
            {
                temp.initializeBlank();
            }
            return temp;
        }

        public void initializeBlank()
        {
            for (int i = 0; i < tiles.GetLength(0); i++)
            {
                for (int j = 0; j < tiles.GetLength(1); j++)
                {
                    for (int k = 0; k < tiles.GetLength(2); k++)
                    {
                        tiles[i, j, k] = new SingleWorldTile(2, Int3.Zero);
                    }
                }
            }
        }
        public void initializeNoise()
        {
            for (int i = 0; i < tiles.GetLength(0); i++)
            {
                for (int j = 0; j < tiles.GetLength(1); j++)
                {
                    for (int k = 0; k < tiles.GetLength(2); k++)
                    {
                        short id = (short)((i + j / 2) % 16);
                        if (k >= 1 && i == 2)
                        {
                            id = 3;
                        }
                        else if (k > 1)
                        {
                            id = 1;
                        }
                        tiles[i, j, k] = new SingleWorldTile(id, new Int3(i,j,k));
                    }
                }
            }
        }
        public void initializeNormally(int generationType)
        {
            for (int i = 0; i < tiles.GetLength(0); i++)
            {
                for (int j = 0; j < tiles.GetLength(1); j++)
                {
                    for (int k = 0; k < tiles.GetLength(2); k++)
                    {
                        Int3 tilePos = new Int3(chunkPos.x * defaultx + i, chunkPos.y * defaulty + j, k);
                        short desiredID = 0;
                        switch (k)
                        {
                            case 0:
                                desiredID = 1;
                                break;
                            case 1:
                            case 2:
                                desiredID = 0;
                                break;
                        }
                        tiles[i, j, k] = new SingleWorldTile(desiredID, tilePos);

                        SingleWorldTile swt = tiles[i, j, k];
                        TileProperties tp = TileLibrary.tileLib[swt.tileID];
                    }
                }
            }
        }

        public void NeighborRequiringUpdate()
        {
            hasCompletedPostProcessingThatRequiresNeighbors = true;
        }

        SingleWorldTile getQuickTile(Int3 worldPos)
        {
            int localx = worldPos.x - chunkPos.x * defaultx;
            int localy = worldPos.y - chunkPos.y * defaulty;
            int localz = worldPos.z;

            if (localx < 0 || localy < 0 || localz < 0 || localx >= defaultx || localy >= defaulty || localz >= defaultz)
            {
                return WorldManager.currentWorld.GetTileAtLocation(worldPos);
            }
            return tiles[localx, localy, localz];
        }
        SingleWorldTile getTileFromNeighborArray(SingleWorldTile[,,] neighborArray, Int3 worldPositionOfTile, int xWithinArray, int yWithinArray, int zWithinArray)
        {
            if (neighborArray[xWithinArray, yWithinArray, zWithinArray] == null)
            {
                neighborArray[xWithinArray, yWithinArray, zWithinArray] = getQuickTile(worldPositionOfTile);
            }
            return neighborArray[xWithinArray, yWithinArray, zWithinArray];
        }

        static void swapTiles(SingleWorldTile a, SingleWorldTile b)
        {
            SingleWorldTile temp = a;
            a = b;
            b = temp;
        }

        public static Chunk getChunkFromBytes(byte[] data, Int3 pos)
        {
            List<byte> converted = new List<byte>(data);
            int version = Convert.ToInt(data, 0);
            converted.RemoveRange(0, sizeof(uint));
            switch (version)
            {
                case 0:
                    return getterV1(converted, pos);
                default:
                    ErrorHandler.AddErrorToLog("Unrecognized chunk version:" + version);
                    Chunk temp = new Chunk(new Int3(pos.x, pos.y, pos.z));
                    temp.initializeBlank();
                    return temp;
            }

        }
        static Chunk getterV1(List<byte> data, Int3 pos)
        {
            Chunk holster = new Chunk(new Int3(pos.x, pos.y, pos.z));
            bool[] info = Convert.ToBoolArray(data[0]);
            data.RemoveAt(0);
            holster.hasCompletedPostProcessingThatRequiresNeighbors = info[0];
            for (int i = 0; i < holster.tiles.GetLength(0); i++)
            {
                for (int j = 0; j < holster.tiles.GetLength(1); j++)
                {
                    for (int k = 0; k < holster.tiles.GetLength(2); k++)
                    {
                        holster.tiles[i, j, k] = SingleWorldTile.bytesToTileV1(data, new Int3(pos.x * defaultx + i, pos.y * defaulty + j, pos.z * defaultz + k));
                    }
                }
            }
            return holster;
        }

        public byte[] convertChunkToBytes()
        {
            List<byte> temp = new List<byte>();
            temp.AddRange(Convert.ToByte(chunkVersion));
            byte bools = Convert.ToByte(new bool[] { hasCompletedPostProcessingThatRequiresNeighbors, false, false, false, false, false, false, false });
            temp.Add(bools);
            for (int i = 0; i < tiles.GetLength(0); i++)
            {
                for (int j = 0; j < tiles.GetLength(1); j++)
                {
                    for (int k = 0; k < tiles.GetLength(2); k++)
                    {
                        temp.AddRange(tiles[i, j, k].getBytesV1());
                    }
                }
            }
            return temp.ToArray();
        }
    }
}
