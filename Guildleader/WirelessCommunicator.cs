using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Linq;

namespace Guildleader
{
    public static class WirelessCommunicator
    {
        public static UdpClient UDPNode;

        public static void Initialize()
        {
            UDPNode = new UdpClient(44500, AddressFamily.InterNetwork);
            Console.WriteLine("Wireless communicator initialized.");
        }

        public static void Cleanup()
        {
            UDPNode.Close();
            UDPNode.Dispose();
        }
    }

    public class DataPacket
    {
        public enum packetType
        {

        }

        public IPAddress address;
        public short port;

        public packetType stowedPacketType;

        public byte[] contents;

        public DataPacket(IPAddress addressGiven, short portGiven, byte[] packetBytes)
        {
            address = addressGiven;
            port = portGiven;
            stowedPacketType = (packetType)contents[0];
            contents = packetBytes.Skip(sizeof(byte)).ToArray();
        }
    }

public class largeObjectByteHandler
    {
        public Dictionary<string, packetAssembler> recievedSegments = new Dictionary<string, packetAssembler> { };

        public Dictionary<short, sentPacket> recentlySegmentedPacket = new Dictionary<short, sentPacket> { };
        short lastSentPacket; //packet identifier
        public byte[][] breakBytesIntoSendableSegments(byte[] longValue, short breakSize)
        {
            List<byte[]> segments = new List<byte[]>();

            short counter = 0;
            while (counter * breakSize < longValue.Length)
            {
                int distanceToGrab = Math.Min(breakSize, longValue.Length - counter * breakSize);
                List<byte> subSegment = new List<byte>();
                subSegment.AddRange(longValue.Skip(counter * breakSize).Take(distanceToGrab));

                subSegment.InsertRange(0, BitConverter.GetBytes(counter));
                subSegment.InsertRange(0, BitConverter.GetBytes(lastSentPacket));
                segments.Add(subSegment.ToArray());

                counter++;
            }
            int length = segments.Count();

            List<byte> packetIdentifyingInformation = new List<byte>();
            packetIdentifyingInformation.AddRange(BitConverter.GetBytes(lastSentPacket));
            packetIdentifyingInformation.AddRange(BitConverter.GetBytes(short.MaxValue));
            packetIdentifyingInformation.AddRange(BitConverter.GetBytes(length));

            segments.Insert(0, packetIdentifyingInformation.ToArray());

            byte[][] array = segments.ToArray();
            recentlySegmentedPacket[lastSentPacket] = new sentPacket(array);

            lastSentPacket++;
            if (lastSentPacket > short.MaxValue - 10)
            {
                lastSentPacket = 0;
            }
            return array;
        }

        public void recieveSegments(string identifier, byte[] segment)
        {
            short packetID = BitConverter.ToInt16(segment, 0);
            short positionInPacket = BitConverter.ToInt16(segment, sizeof(short));

            string fullName = string.Concat(identifier, packetID);
            if (!recievedSegments.ContainsKey(fullName))
            {
                recievedSegments.Add(fullName, new packetAssembler());
            }
            recievedSegments[fullName].packetID = packetID;
            if (positionInPacket == short.MaxValue)
            {
                recievedSegments[fullName].maxSize = BitConverter.ToInt16(segment, sizeof(short) * 2);
                return;
            }

            byte[] actualData = segment.Skip(sizeof(short) * 2).Take(segment.Length - sizeof(short) * 2).ToArray();
            recievedSegments[fullName].addBytePacket(positionInPacket, actualData);
        }

        public void runCleanup()
        {
            removeOldRecievedSegments(15);
            removeOldSentPackets(30);
        }
        public void removeOldRecievedSegments(int maxAgeOfPacketsInSeconds)
        {
            DateTime now = DateTime.Now;
            List<string> toRemove = new List<string>();
            foreach (var kvp in recievedSegments)
            {
                if ((now - kvp.Value.dateOfLastRecievedPart).TotalSeconds >= maxAgeOfPacketsInSeconds)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (string s in toRemove)
            {
                recievedSegments.Remove(s);
            }
        }
        public void removeOldSentPackets(int maxAgeOfPacketsInSeconds)
        {
            DateTime now = DateTime.Now;
            List<short> toRemove = new List<short>();
            foreach (var kvp in recentlySegmentedPacket)
            {
                if ((now - kvp.Value.sent).TotalSeconds >= maxAgeOfPacketsInSeconds)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (short s in toRemove)
            {
                recentlySegmentedPacket.Remove(s);
            }
        }

        public byte[][] getPartsOfRecentlySentPacket(short segmentID, List<int> parts)
        {
            byte[][] toResend = new byte[parts.Count][];
            if (!recentlySegmentedPacket.ContainsKey(segmentID))
            {
                return null;
            }
            byte[][] sentTarget = recentlySegmentedPacket[segmentID].contents;
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i] >= sentTarget.Length)
                {
                    return null;
                }
                toResend[i] = sentTarget[parts[i]];
            }
            return toResend;
        }
    }

    public class packetAssembler
    {
        public DateTime dateOfLastRecievedPart;
        byte[][] allSegments = new byte[32768][];
        public short maxSize = short.MaxValue;
        public short packetID = short.MaxValue;

        public void addBytePacket(short segmentID, byte[] data)
        {
            if (segmentID >= allSegments.Length || segmentID < 0)
            {
                return;
            }
            if (allSegments[segmentID] == null)
            {
                dateOfLastRecievedPart = DateTime.Now;
            }
            allSegments[segmentID] = data;
        }

        public byte[] getAssembledPacket()
        {
            if (maxSize == short.MaxValue)
            {
                return null;
            }
            List<byte> holster = new List<byte> { };
            for (int i = 0; i < Math.Min(maxSize, allSegments.Length); i++)
            {
                if (allSegments[i] == null)
                {
                    return null;
                }
                holster.AddRange(allSegments[i]);
            }
            return holster.ToArray();
        }

        public List<int> getMissingParts()
        {
            List<int> holster = new List<int> { };
            if (maxSize == short.MaxValue)
            {
                holster.Add(0);
                return holster;
            }
            for (int i = 0; i < maxSize; i++)
            {
                if (allSegments[i] == null)
                {
                    holster.Add(i + 1);
                }
            }

            return holster;
        }
    }

    public class sentPacket
    {
        public DateTime sent;
        public byte[][] contents;

        public sentPacket(byte[][] info)
        {
            contents = info;
            sent = DateTime.Now;
        }
    }
}
