using System;
using System.Collections.Generic;
using System.Text;
using Guildleader.Entities.BasicEntities;

namespace Guildleader.Entities
{
    public class Entity
    {
        public Int3 worldPositon = Int3.Zero;
        public Int3 currentChunk; //used by the server to know when someone/something has shifted chunk; and, as such, allows us to know when to move us from one chunk to another

        public int id = int.MaxValue;

        public short buildVersion; //override this every time the method of serialization changes

        public enum EntityKey
        {
            Invalid,
            DefaultEntity,
            Actor,
            Pokemon
        }

        public static Dictionary<EntityKey, Type> EntityDictionary = new Dictionary<EntityKey, Type>
        {
            {EntityKey.DefaultEntity, typeof(Entity) },
            {EntityKey.Actor, typeof(Actors) },
            {EntityKey.Pokemon, typeof(Pokemon) },
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
        public virtual byte[] ConvertToBytesForClient()
        {
            return GenerateBytesBase();
        }

        byte[] GenerateBytesBase()
        {
            List<byte> data = new List<byte>();

            Type thisType = GetType();
            data.AddRange(Convert.ToByte((int)ReverseEntityDictionary[thisType]));
            data.AddRange(Convert.ToByte(buildVersion));

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



            Entity newEnt = Activator.CreateInstance(EntityDictionary[typeKey]) as Entity;
            newEnt.buildVersion = versionNumber;
            if (useClientGeneration)
            {
                newEnt.ReadEntityFromBytesClient(data);
            }
            else
            {
                newEnt.ReadEntityFromBytesServer(data);
            }
            newEnt.Initialize();
            return newEnt;
        }

        void EntityGenerationBase(List<byte> data)
        {
            const int numberofInts = 3;
            int[] extractedPos = new int[numberofInts];
            for (int i = 0; i < numberofInts; i++)
            {
                extractedPos[i] = Convert.ToInt(data.ToArray(), i * sizeof(int));
            }
            data.RemoveRange(0, numberofInts * sizeof(int));
            worldPositon = new Int3(extractedPos[0], extractedPos[1], extractedPos[2]);
        }

        //functions for an entity!
        static int lastAssignedEntityID;
        public void AssignID()
        {
            id = lastAssignedEntityID;
            lastAssignedEntityID++;
        }

        public void Initialize() //sets starting position of the entity
        {
            currentChunk = GetChunkPosition();
            Chunk c = WorldManager.currentWorld.GetChunkData(currentChunk);
            c.entitiesLocatedWithinChunk.Add(this);
        }
    }
}
