using System;
using System.Collections.Generic;
using System.Text;
using Guildleader.Entities.BasicEntities;

namespace Guildleader.Entities
{
    public class Entity
    {
        public Int3 worldPositon;
        public Int3 GetCurrentChunk { get { return new Int3((int)Math.Floor((float)worldPositon.x / Chunk.defaultx), (int)Math.Floor((float)worldPositon.y / Chunk.defaulty), (int)Math.Floor((float)worldPositon.z / Chunk.defaultz)); } }

        public Entity() {

        }

        public short buildVersion; //override this every time the method of serialization changes

        public enum EntityKey
        {
            Invalid,
            DefaultEntity,
            PhysicalObject
        }

        public static Dictionary<EntityKey, Type> EntityDictionary = new Dictionary<EntityKey, Type>
        {
            {EntityKey.DefaultEntity, typeof(Entity) },
            {EntityKey.PhysicalObject, typeof(PhysicalObjects) },
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
    }
}
