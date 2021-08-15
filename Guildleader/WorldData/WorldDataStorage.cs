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
    public class WorldDataStorageModuleGeneric //functions both the client and server will need to store world information
    {
        public int seed;

       // public static Dictionary<uint, Entity> allEntities = new Dictionary<uint, Entity> { };

        public Dictionary<int, Dictionary<int, Dictionary<int, chunk>>> allChunks = new Dictionary<int, Dictionary<int, Dictionary<int, chunk>>> { };
        public static int worldMinx = -100, worldMaxx = 100, worldMiny = -100, worldMaxy = 100, worldMinz = -50, worldMaxz = 50;
        const int worldStartSizeX = 97, worldStartSizeY = 97, worldStartSizeZ = 0;

        public const float worldTileSize = 0.1f;

        public bool worldLoaded;
        public float[] threadCoordinator;
        public string[] threadStates;
        Thread[] worldGenerationThreads;
        public int postUpdatesComplete;
        public virtual void InitializeAllChunks()
        {
            for (int i = -worldStartSizeX - 1; i <= worldStartSizeX + 1; i++)
            {
                if (!allChunks.ContainsKey(i))
                {
                    allChunks[i] = new Dictionary<int, Dictionary<int, chunk>>();
                }
                for (int j = -worldStartSizeY - 1; j <= worldStartSizeY + 1; j++)
                {
                    if (!allChunks[i].ContainsKey(j))
                    {
                        allChunks[i][j] = new Dictionary<int, chunk>();
                    }
                }
            }
            threadCoordinator = new float[3];
            threadStates = new string[3];
            worldGenerationThreads = new Thread[3];

            seed = new Random().Next();
            int[] positions = new int[] { -worldStartSizeX, -worldStartSizeX / 3, -worldStartSizeX / 3 + 1, worldStartSizeX / 3, worldStartSizeX / 3 + 1, worldStartSizeX };
            for (int thread = 0; thread < worldGenerationThreads.Length; thread += 1)
            {
                int startPos = positions[2 * thread];
                int endPos = positions[2 * thread + 1];
                int numb = thread;
                Thread builtThread = new Thread(() => InitializationSubThread(numb, startPos, endPos));
                worldGenerationThreads[thread] = builtThread;
                worldGenerationThreads[thread].Priority = ThreadPriority.AboveNormal;
                worldGenerationThreads[thread].Start();
            }

            restartWhile:
            foreach (float f in threadCoordinator)
            {
                if (f < 1)
                {
                    Thread.Sleep(50);
                    goto restartWhile;
                }
            }

            UpdateAllChunksThatNeedNeighborUpdates();

            worldLoaded = true;
        }
        void InitializationSubThread(int threadID, int xPosStart, int xPosEnd)
        {
            threadStates[threadID] = "Generating Terrain...";
            int xDistance = (xPosEnd - xPosStart) * 2 + 1;
            int yDistance = worldStartSizeY * 2 + 1;
            int zDistance = worldStartSizeZ * 2 + 1;
            for (int i = xPosStart; i <= xPosEnd; i++)
            {
                for (int j = -worldStartSizeY; j <= worldStartSizeY; j++)
                {
                    for (int k = -worldStartSizeZ; k <= worldStartSizeZ; k++)
                    {
                        threadStates[threadID] = $"On {i}, {j}, {k}";
                        Int3 pos = new Int3(i, j, k);
                        allChunks[i][j][k] = new chunk(pos);
                        allChunks[i][j][k].initializeNormally(0);
                        threadCoordinator[threadID] = (((i - xPosStart) * yDistance * zDistance) + (j + worldStartSizeY) * zDistance + k) / (xDistance * yDistance * zDistance);
                    }
                }
            }
            threadStates[threadID] = "Finished.";
            threadCoordinator[threadID] = 999;
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
    
        public chunk GetChunkData(int xPos, int yPos, int zPos)
        {
            InitializeChunkInfoPath();
            string fileName = GetNameBasedOnPosition(xPos, yPos, zPos);
            string chunkPath = FileAccess.ChunkStorageName + fileName;
            chunk chu = null;
            if (!FileAccess.FileExists(chunkPath))
            {
                chu = chunk.spawnNewChunk(xPos, yPos, zPos, 0);
            }
            else
            {
                byte[] chunkData = FileAccess.LoadFile(chunkPath);
                chu = chunk.getChunkFromBytes(chunkData, new Int3(xPos, yPos, zPos));
            }
            if (!allChunks.ContainsKey(xPos))
            {
                allChunks[xPos] = new Dictionary<int, Dictionary<int, chunk>>();
            }
            if (!allChunks[xPos].ContainsKey(yPos))
            {
                allChunks[xPos][yPos] = new Dictionary<int, chunk>();
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
        public void SaveChunkData(Int3 pos, chunk chu)
        {
            SaveChunkData(pos.x, pos.y, pos.z, chu);
        }
        public void SaveChunkData(int xPos, int yPos, int zPos, chunk chu)
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
                            allChunks[x] = new Dictionary<int, Dictionary<int, chunk>>();
                        }
                        if (!allChunks[x].ContainsKey(y))
                        {
                            allChunks[x][y] = new Dictionary<int, chunk>();
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
            return new Int3(chunkID.x * chunk.defaultx, chunkID.y * chunk.defaulty, chunkID.z * chunk.defaultz);
        }
        static string GetNameBasedOnPosition(int xPos, int yPos, int zPos)
        {
            return xPos + "v" + yPos + "v" + zPos;
        }
        public static string appPath;
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
            int spaceWithinChunkx = x % chunk.defaultx;
            int spaceWithinChunky = y % chunk.defaulty;
            int spaceWithinChunkz = z % chunk.defaultz;
            if (spaceWithinChunkx < 0)
            {
                spaceWithinChunkx += chunk.defaultx;
            }
            if (spaceWithinChunky < 0)
            {
                spaceWithinChunky += chunk.defaulty;
            }
            if (spaceWithinChunkz < 0)
            {
                spaceWithinChunkz += chunk.defaultz;
            }
            chunk chunkResult = null;
            Int3 chunkPos = GetChunkPositionBasedOnTilePosition(x, y, z);
            Dictionary<int, Dictionary<int, chunk>> holdera = new Dictionary<int, Dictionary<int, chunk>>();
            Dictionary<int, chunk> holderb = new Dictionary<int, chunk>();
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
            int chunkX = (int)Math.Floor(x / (float)chunk.defaultx);
            int chunkY = (int)Math.Floor(y / (float)chunk.defaulty);
            int chunkZ = (int)Math.Floor(z / (float)chunk.defaultz);
            return new Int3(chunkX, chunkY, chunkZ);
        }

        //world generation
        static int lastTakenThread;
        public void UpdateAllChunksThatNeedNeighborUpdates()
        {
            try
            {
                foreach (var xdic in allChunks)
                {
                    foreach (var ydic in xdic.Value)
                    {
                        foreach (chunk c in ydic.Value.Values)
                        {
                            worldGenerationThreads[lastTakenThread] = new Thread(c.neighborRequiringUpdate);
                            worldGenerationThreads[lastTakenThread].Start();

                            lastTakenThread++;
                            lastTakenThread = lastTakenThread % worldGenerationThreads.Length;

                            postUpdatesComplete++;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ErrorHandler.AddErrorToLog(e);
            }

        }
        bool NeighborChunksAreLoaded(Int3 chunk)
        {
            for (int i = chunk.x - 1; i <= chunk.x + 1; i++)
            {
                if (!allChunks.ContainsKey(i))
                {
                    return false;
                }
                for (int j = chunk.y - 1; j <= chunk.y + 1; j++)
                {
                    if (!allChunks[i].ContainsKey(j))
                    {
                        return false;
                    }
                    for (int k = chunk.z - 1; k <= chunk.z + 1; k++)
                    {
                        if (!allChunks[i][j].ContainsKey(k))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }

    public class chunk
    {
        public Int3 chunkPos;
        public bool hasCompletedPostProcessingThatRequiresNeighbors;
        int chunkVersion = 0;

        public const int defaultx = 8, defaulty = 8, defaultz = 3;
        public SingleWorldTile[,,] tiles = new SingleWorldTile[defaultx, defaulty, defaultz];

        public chunk(Int3 position)
        {
            chunkPos = position;
        }
        public static chunk spawnNewChunk(int x, int y, int z, int gereationType)
        {
            chunk temp = new chunk(new Int3(x, y, z));
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
                        tiles[i, j, k] = new SingleWorldTile(2, Int3.zero);
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

        public void neighborRequiringUpdate()
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

        public static chunk getChunkFromBytes(byte[] data, Int3 pos)
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
                    chunk temp = new chunk(new Int3(pos.x, pos.y, pos.z));
                    temp.initializeBlank();
                    return temp;
            }

        }
        static chunk getterV1(List<byte> data, Int3 pos)
        {
            chunk holster = new chunk(new Int3(pos.x, pos.y, pos.z));
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
