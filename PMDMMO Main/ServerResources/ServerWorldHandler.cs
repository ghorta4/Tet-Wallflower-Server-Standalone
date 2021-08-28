using System;
using System.Collections.Generic;
using System.Threading;
using Guildleader;
using Guildleader.Entities;

namespace ServerResources
{
    public class ServerWorldHandler : WorldDataStorageModuleGeneric
    {
        public bool worldLoaded;
        public float[] threadCoordinator;
        public string[] threadStates;
        Thread[] worldGenerationThreads;
        public int postUpdatesComplete;
        public void InitializeAllChunks()
        {
            for (int i = -worldStartSizeX - 1; i <= worldStartSizeX + 1; i++)
            {
                if (!allChunks.ContainsKey(i))
                {
                    allChunks[i] = new Dictionary<int, Dictionary<int, Chunk>>();
                }
                for (int j = -worldStartSizeY - 1; j <= worldStartSizeY + 1; j++)
                {
                    if (!allChunks[i].ContainsKey(j))
                    {
                        allChunks[i][j] = new Dictionary<int, Chunk>();
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
            threadCoordinator[0] = -10;
            threadStates[0] = "Working on post-generation updates.";
            threadStates[2] = "...";
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
                        allChunks[i][j][k] = new Chunk(pos);
                        allChunks[i][j][k].InitializeNormally(0);
                        threadCoordinator[threadID] = (((i - xPosStart) * yDistance * zDistance) + (j + worldStartSizeY) * zDistance + k) / (xDistance * yDistance * zDistance);
                    }
                }
            }
            threadStates[threadID] = "Finished.";
            threadCoordinator[threadID] = 999;
        }

        public Dictionary<int, Dictionary<int, Dictionary<int, Entity>>> EntitiesByChunk = new Dictionary<int, Dictionary<int, Dictionary<int, Entity>>> { };
        //world generation
        static int lastTakenThread;
        public void UpdateAllChunksThatNeedNeighborUpdates()
        {
            try
            {
                int totalCount = allChunks.Count * allChunks[0].Count * allChunks[0][0].Count;
                foreach (var xdic in allChunks)
                {
                    foreach (var ydic in xdic.Value)
                    {
                        foreach (Chunk c in ydic.Value.Values)
                        {
                            threadCoordinator[0] = (float)postUpdatesComplete / totalCount;
                            threadStates[1] = "Progress: " + threadCoordinator[0].ToString("P");
                            while (worldGenerationThreads[lastTakenThread].IsAlive)
                            {
                                Thread.Sleep(0);
                            }
                            worldGenerationThreads[lastTakenThread] = new Thread(c.NeighborRequiringUpdate);
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

        public void UpdateAllChunks(float deltaTime, byte currentFrameNumber)
        {
            List<Chunk> allChunksToUpdate = GetAllChunksLoaded();
            foreach (Chunk c in allChunksToUpdate)
            {
                c.Update(deltaTime, currentFrameNumber);
            }
        }

        public override Chunk GetChunkNotYetLoaded(int xPos, int yPos, int zPos)
        {
            string fileName = GetNameBasedOnPosition(xPos, yPos, zPos);
            string chunkPath = FileAccess.ChunkStorageName + fileName;
            Chunk chu = null;
            if (!FileAccess.FileExists(chunkPath))
            {
                chu = Chunk.SpawnNewChunk(xPos, yPos, zPos, 0);
            }
            else
            {
                byte[] chunkData = FileAccess.LoadFile(chunkPath);
                chu = Chunk.GetChunkFromBytes(chunkData, new Int3(xPos, yPos, zPos));
            }

            return chu;
        }
    }
}
