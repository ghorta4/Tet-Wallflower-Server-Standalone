using System;
using System.Collections.Generic;
using System.Text;
using Guildleader.Entities.BasicEntities;

namespace Guildleader.Entities
{
    public class Entity
    {
        public int EntityID = -1;

        public Int3 worldPositon;

        public Int3 currentChunk;

        static int lastAssignedID;

        Chunk chunkImIn;
        public Entity() {
            EntityID = lastAssignedID;
            lastAssignedID++;
        }

        public short buildVersion; //override this every time the method of serialization changes

        public enum EntityKey
        {
            Invalid,
            DefaultEntity,
            Actor,
            Pokemon,
            PlayerPokemon
        }

        public static Dictionary<EntityKey, Type> EntityDictionary = new Dictionary<EntityKey, Type>
        {
            {EntityKey.DefaultEntity, typeof(Entity) },
            {EntityKey.Actor, typeof(Actors) },
            {EntityKey.Pokemon, typeof(Pokemon) },
            {EntityKey.PlayerPokemon, typeof(PlayerPokemon) },
        };
        public static Dictionary<Type, EntityKey> ReverseEntityDictionary;

        //creates the reverse entity dictionary
        public static void InitializeEntities()
        {
            ReverseEntityDictionary = new Dictionary<Type, EntityKey>();
            foreach (var kvp in EntityDictionary)
            {
                ReverseEntityDictionary.Add(kvp.Value, kvp.Key);
                if (!typeof(Entity).IsAssignableFrom(kvp.Value))
                {
                    throw new Exception("Warning: The class '" + kvp.Value.ToString() + "' does not derive from Entity!");
                }
            }
        }

        public virtual byte[] ConvertToBytesForDataStorage()
        {
            return GenerateBytesBase();
        }
        public virtual byte[] ConvertToBytesForClient(Entity observer) //do not show entities by default. requires an override to do so. Null values are never shared to the client, and are hence hidden. IE for invisible objects. Observer is passed in the case that, say, someone has invisi-sight or something.
        {
            return null;
        }

        public byte[] GenerateBytesBase()
        {
            List<byte> data = new List<byte>();

            Type thisType = GetType();
            data.AddRange(Convert.ToByte((int)ReverseEntityDictionary[thisType]));
            data.AddRange(Convert.ToByte(buildVersion));
            data.AddRange(Convert.ToByte(EntityID));

            data.AddRange(Convert.ToByte(worldPositon.x));
            data.AddRange(Convert.ToByte(worldPositon.y));
            data.AddRange(Convert.ToByte(worldPositon.z));
            return data.ToArray();
        }

        //puts the entity into the world's entity library chunk sorter
        public Int3 GetChunkPosition()
        {
            return WorldDataStorageModuleGeneric.GetChunkPositionBasedOnTilePosition(worldPositon.x, worldPositon.y, worldPositon.z);
        }

        //Note: When overriding this function, always remove the bytes you used to make daisy chaining easier down the line.
        public virtual void ReadEntityFromBytesServer(List<byte> data)
        {
            EntityGenerationBase(data);
        }
        public virtual void ReadEntityFromBytesClient(List<byte> data)
        {
            EntityGenerationBase(data);
        }

        public static Entity GenerateEntity(List<byte> data, bool useClientGeneration)
        {
            int typeID = Convert.ToInt(data.ToArray(),0);
            EntityKey typeKey = (EntityKey)typeID;

            data.RemoveRange(0, sizeof(int));

            short versionNumber = Convert.ToShort(data.ToArray());

            data.RemoveRange(0, sizeof(short));

            int entityID = Convert.ExtractInts(data, 1)[0];

            Entity newEnt = Activator.CreateInstance(EntityDictionary[typeKey]) as Entity;
            newEnt.buildVersion = versionNumber;
            newEnt.EntityID = entityID;
            if (useClientGeneration)
            {
                newEnt.ReadEntityFromBytesClient(data);
            }
            else
            {
                newEnt.ReadEntityFromBytesServer(data);
            }
            return newEnt;
        }

        void EntityGenerationBase(List<byte> data)
        {
            int[] extractedPos = Convert.ExtractInts(data, 3);
            worldPositon = new Int3(extractedPos[0], extractedPos[1], extractedPos[2]);
        }

        public virtual void UpdateEntityUsingHarvestedData(Entity harvestedData)
        {
            worldPositon = harvestedData.worldPositon;
        }
        //and now, functions for our entities
        public void Initialize(Int3 startingPosition)
        {
            worldPositon = startingPosition;
            currentChunk = GetChunkPosition();
            chunkImIn = WorldManager.currentWorld.GetChunk(currentChunk);
            chunkImIn.containedEntities.Add(this);
        }

        byte currentFrameNumber = 255;
        public virtual void Update(float deltaTime, byte lastUpdatedFrameNumber)
        {
            if (currentFrameNumber == lastUpdatedFrameNumber)
            {
                return;
            }
            currentFrameNumber = lastUpdatedFrameNumber;
            HandleChunkAssignment();
        }

        void HandleChunkAssignment()
        {
            Int3 newChunkPos = GetChunkPosition();
            if (newChunkPos != currentChunk)
            {
                currentChunk = newChunkPos;
                chunkImIn.containedEntities.Remove(this);
                chunkImIn = WorldManager.currentWorld.GetChunk(currentChunk);
                chunkImIn.containedEntities.Add(this);
             //   WorldManager.currentWorld.LoadNearbyChunkData(currentChunk,2);
                ActionsOnChunkChange();
            }
        }

        public virtual void ActionsOnChunkChange() { }
    }
}
