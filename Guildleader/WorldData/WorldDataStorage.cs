using System;
using System.Collections.Generic;
using System.Threading;
using Guildleader.Entities;
using Guildleader.Entities.BasicEntities;

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

        public Dictionary<int, Dictionary<int, Dictionary<int, Chunk>>> allChunks = new Dictionary<int, Dictionary<int, Dictionary<int, Chunk>>> { };
        public static int worldMinx = -5, worldMaxx = 5, worldMiny = -5, worldMaxy = 5, worldMinz = -1, worldMaxz = 5;
        public const int worldStartSizeX = 0, worldStartSizeY = 0, worldStartSizeZ = 1;

        public const float worldTileSize = 0.1f;

        public WorldDataStorageModuleGeneric()
        {
            Initialize();
        }

        public virtual void Initialize()
        {
            InitializeChunkInfoPath();
        }

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
        
        public bool CheckIfChunkIsLoaded(Int3 position)
        {
            return allChunks.ContainsKey(position.x) && allChunks[position.x].ContainsKey(position.y) && allChunks[position.x][position.y].ContainsKey(position.z);
        }

        public Chunk GetChunk(Int3 position)
        {
            return GetChunk(position.x, position.y, position.z);
        }
        public Chunk GetChunk(int xPos, int yPos, int zPos)
        {
            Chunk chu = null;

            bool hasXpos = allChunks.ContainsKey(xPos);
            bool hasYpos = hasXpos && allChunks[xPos].ContainsKey(yPos);

            if (hasXpos && hasYpos && allChunks[xPos][yPos].ContainsKey(zPos))
            {
                return allChunks[xPos][yPos][zPos];
            }

            chu = GetChunkNotYetLoaded(xPos, yPos, zPos);
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
                        inArea.Add(GetChunk(x,y,z));
                    }
                }
            }
            return inArea.ToArray();
        }
        public List<Chunk> GetAllChunksLoaded()
        {
            List<Chunk> holster = new List<Chunk>();
            try
            {
                foreach (var kvpxy in allChunks)
                {
                    foreach (var kvpyz in kvpxy.Value)
                    {
                        foreach (Chunk c in kvpyz.Value.Values)
                        {
                            holster.Add(c);
                        }
                    }
                }
                return holster;
            }
            catch (Exception e)
            {
                ErrorHandler.AddErrorToLog(e);
                return null;
            }

        }
        public void SaveChunkData(Int3 pos, Chunk chu)
        {
            SaveChunkData(pos.x, pos.y, pos.z, chu);
        }
        public void SaveChunkData(int xPos, int yPos, int zPos, Chunk chu)
        {
            InitializeChunkInfoPath();
            string fileName = GetNameBasedOnPosition(xPos, yPos, zPos);
            FileAccess.WriteBytesInsideCurrentDefaultDirectoryInSubfolder(chu.ConvertChunkToBytes(), fileName, FileAccess.ChunkStorageName);
        }

        public abstract Chunk GetChunkNotYetLoaded(int x, int y, int z);

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
                            allChunks[x][y][z] = GetChunk(x, y, z);
                        }
                    }
                }
            }
            NotifyAllNearbyPlayersOfUpdate(chunkPos, 1);
        }

        public static Int3 ChunkCenterPosition(Int3 chunkID)
        {
            return new Int3(chunkID.x * Chunk.defaultx, chunkID.y * Chunk.defaulty, chunkID.z * Chunk.defaultz);
        }
        public static string GetNameBasedOnPosition(int xPos, int yPos, int zPos)
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
            while (spaceWithinChunkx < 0)
            {
                spaceWithinChunkx += Chunk.defaultx;
            }
            while (spaceWithinChunky < 0)
            {
                spaceWithinChunky += Chunk.defaulty;
            }
            while (spaceWithinChunkz < 0)
            {
                spaceWithinChunkz += Chunk.defaultz;
            }
            Chunk chunkResult = GetChunk(GetChunkPositionBasedOnTilePosition(x, y, z));
            //Int3 chunkPos = GetChunkPositionBasedOnTilePosition(x, y, z);
            //Dictionary<int, Chunk> holderb = null;
            //bool success = allChunks.TryGetValue(chunkPos.x, out Dictionary<int, Dictionary<int, Chunk>> holdera);
            //if (success)
            //{
            //    success = holdera.TryGetValue(chunkPos.y, out holderb);
            //}
            //if (success)
            //{
            //    success = holderb.TryGetValue(chunkPos.z, out chunkResult);
            //}
            //if (!success)
            //{
            //    SingleWorldTile temp = new SingleWorldTile(5, new Int3(x, y, z));
            //    return temp;
            //}
            //if (!chunkResult.hasCompletedPostProcessingThatRequiresNeighbors)
            //{
            //    SingleWorldTile temp = new SingleWorldTile(5, new Int3(x, y, z));
            //    return temp;
            //}
            return chunkResult.GetTile(spaceWithinChunkx, spaceWithinChunky, spaceWithinChunkz);
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

        //below function marks all players within a range of chunks with a bool that lets them recieve an update. Do this every time a tile is updated
        public void NotifyAllNearbyPlayersOfUpdate(Int3 center, int range)
        {
            Chunk[] toTarget = GetChunksInArea(center.x, center.y, center.z, range, range, range);
            List<Entity> allNearbyEntities = new List<Entity>();
            foreach (Chunk c in toTarget)
            {
                allNearbyEntities.AddRange(c.containedEntities);
            }
            foreach (Entity e in allNearbyEntities)
            {
                PlayerPokemon asPlayer = e as PlayerPokemon;
                if (asPlayer != null)
                {
                    asPlayer.needsChunksResent = true;
                }
            }
        }
    }

    public class Chunk
    {
        public Int3 chunkPos;
        public bool hasCompletedPostProcessingThatRequiresNeighbors;
        readonly int chunkVersion = 0;

        public const int defaultx = 15, defaulty = 15, defaultz = 2;
        SingleWorldTile[,,] tiles = new SingleWorldTile[defaultx, defaulty, defaultz];

        public List<Entity> containedEntities = new List<Entity>();

        public Chunk(Int3 position)
        {
            chunkPos = position;
        }
        public static Chunk SpawnNewChunk(int x, int y, int z, int gereationType)
        {
            Chunk temp = new Chunk(new Int3(x, y, z));
            if (x < WorldDataStorageModuleGeneric.worldMaxx && x > WorldDataStorageModuleGeneric.worldMinx && y > WorldDataStorageModuleGeneric.worldMiny && y < WorldDataStorageModuleGeneric.worldMaxy)
            {
                temp.InitializeNormally(0);
            }
            else
            {
                temp.InitializeBlank();
            }
            return temp;
        }

        public void InitializeBlank()
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
        public void InitializeNotLoaded()
        {
            for (int i = 0; i < tiles.GetLength(0); i++)
            {
                for (int j = 0; j < tiles.GetLength(1); j++)
                {
                    for (int k = 0; k < tiles.GetLength(2); k++)
                    {
                        tiles[i, j, k] = new SingleWorldTile(5, new Int3(i,j,k));
                    }
                }
            }
        }
        public void InitializeNoise()
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
        public void InitializeNormally(int generationType)
        {
            short testChunkTileType = (short)((chunkPos.x + chunkPos.y) % 2 == 0 ? 4 : 1);
            for (int i = 0; i < tiles.GetLength(0); i++)
            {
                for (int j = 0; j < tiles.GetLength(1); j++)
                {
                    for (int k = 0; k < tiles.GetLength(2); k++)
                    {
                        Int3 tilePos = new Int3(chunkPos.x * defaultx + i, chunkPos.y * defaulty + j, chunkPos.z * defaultz + k);
                        short desiredID = 0;
                        if (chunkPos.z < 0)
                        {
                            desiredID = testChunkTileType;
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

        public SingleWorldTile GetTile(Int3 relativePos)
        {
            return GetTile(relativePos.x, relativePos.y, relativePos.z);
        }
        public SingleWorldTile GetTile(int x, int y, int z)
        {
            return tiles[x, y, z];
        }

        public SingleWorldTile[,,] GetAllTiles()
        {
            return tiles;
        }

        SingleWorldTile GetQuickTile(Int3 worldPos)
        {
            int localx = worldPos.x - chunkPos.x * defaultx;
            int localy = worldPos.y - chunkPos.y * defaulty;
            int localz = worldPos.z - chunkPos.z * defaultz;

            if (localx < 0 || localy < 0 || localz < 0 || localx >= defaultx || localy >= defaulty || localz >= defaultz)
            {
                return WorldManager.currentWorld.GetTileAtLocation(worldPos);
            }
            return tiles[localx, localy, localz];
        }
        SingleWorldTile GetTileFromNeighborArray(SingleWorldTile[,,] neighborArray, Int3 worldPositionOfTile, int xWithinArray, int yWithinArray, int zWithinArray)
        {
            if (neighborArray[xWithinArray, yWithinArray, zWithinArray] == null)
            {
                neighborArray[xWithinArray, yWithinArray, zWithinArray] = GetQuickTile(worldPositionOfTile);
            }
            return neighborArray[xWithinArray, yWithinArray, zWithinArray];
        }

        static void SwapTiles(SingleWorldTile a, SingleWorldTile b)
        {
            SingleWorldTile temp = a;
            a = b;
            b = temp;
        }

        public static Chunk ConvertBytesWithPositionInFrontToChunkSimple(byte[] byte_array)
        {
            int x = Convert.ToInt(byte_array, 0);
            int y = Convert.ToInt(byte_array, sizeof(int));
            int z = Convert.ToInt(byte_array, sizeof(int) * 2);
            byte[] bytes_without_position = new byte[byte_array.Length - (sizeof(int) * 3)];
            Array.Copy(byte_array, sizeof(int) * 3, bytes_without_position, 0, bytes_without_position.Length);
            return GetterV1Simple(new List<byte>(bytes_without_position), new Int3(x, y, z));
        }
        public static Chunk GetChunkFromBytes(byte[] data, Int3 pos)
        {
            List<byte> converted = new List<byte>(data);
            int version = Convert.ToInt(data, 0);
            converted.RemoveRange(0, sizeof(int));
            switch (version)
            {
                case 0:
                    return GetterV1(converted, pos);
                default:
                    ErrorHandler.AddErrorToLog("Unrecognized chunk version:" + version);
                    Chunk temp = new Chunk(new Int3(pos.x, pos.y, pos.z));
                    temp.InitializeBlank();
                    return temp;
            }

        }
        static Chunk GetterV1(List<byte> data, Int3 pos)
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

        public byte[] ConvertChunkToBytes()
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
        static Chunk GetterV1Simple(List<byte> data, Int3 pos)
        {
            Chunk holster = new Chunk(new Int3(pos.x, pos.y, pos.z));
            int chunkVersion = Convert.ExtractInts(data, 1)[0];
            bool[] info = Convert.ToBoolArray(data[0]);
            data.RemoveAt(0);
            holster.hasCompletedPostProcessingThatRequiresNeighbors = info[0];
            for (int i = 0; i < holster.tiles.GetLength(0); i++)
            {
                for (int j = 0; j < holster.tiles.GetLength(1); j++)
                {
                    for (int k = 0; k < holster.tiles.GetLength(2); k++)
                    {
                        holster.tiles[i, j, k] = SingleWorldTile.bytesToTileV1Simple(data, new Int3(pos.x * defaultx + i, pos.y * defaulty + j, pos.z * defaultz + k));
                    }
                }
            }
            return holster;
        }
        public byte[] ConvertChunkToBytesSimple()
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
                        temp.AddRange(tiles[i, j, k].getBytesSimpleV1());
                    }
                }
            }
            return temp.ToArray();
        }
        public byte[] ConvertChunkToBytesWithPositionInFront(Int3 pos)
        {
            List<byte> holster = new List<byte>(ConvertChunkToBytes());
            holster.InsertRange(0, Convert.ToByte(pos.z));
            holster.InsertRange(0, Convert.ToByte(pos.y));
            holster.InsertRange(0, Convert.ToByte(pos.x));
            return holster.ToArray();
        }
        public byte[] ConvertChunkToBytesWithPositionInFrontUsingSimples(Int3 pos)
        {
            List<byte> holster = new List<byte>(ConvertChunkToBytesSimple());
            holster.InsertRange(0, Convert.ToByte(pos.z));
            holster.InsertRange(0, Convert.ToByte(pos.y));
            holster.InsertRange(0, Convert.ToByte(pos.x));
            return holster.ToArray();
        }

        //server function for running the game below

        public void Update(float deltaTime, byte lastUpdatedFrameNumber) //frame number is to prevent an entity from updating twice if, say, it moves from one chunk to another
        {
            List<Entity> entitiesToUpdate = new List<Entity>(containedEntities);
            foreach (Entity e in entitiesToUpdate)
            {
                e.Update(deltaTime, lastUpdatedFrameNumber);
            }
        }
    }
}
