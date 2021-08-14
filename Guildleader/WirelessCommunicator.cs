using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Threading;
using System.Net.NetworkInformation;

namespace Guildleader
{
    public abstract class WirelessCommunicator
    {

        public largeObjectByteHandler lobh = new largeObjectByteHandler();

        public bool abortWirelessCommunications;
        public bool WirelessThreadActive { get { return !abortWirelessCommunications; } }

        public UdpClient UDPNode;

        public Queue<DataPacket> packets = new Queue<DataPacket> { };

        public enum packetType
        {
            invalid,
            testCall,
        }

        public const int defaultPort = 44500;

        public abstract void Initialize();

        Thread wirelessThread;
        public void StartListeningThread()
        {
            wirelessThread = new Thread(UDPListeningThread);
            wirelessThread.Start();
        }

        public void FindVariablePort()
        {
            List<IPEndPoint> allUsedListeners = new List<IPEndPoint>(IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners());
            int targetPort = defaultPort;
            for (int i = 0; i < 100; i++)
            {
                bool skipPort = false;
                targetPort = defaultPort + i;
                foreach (IPEndPoint ipe in allUsedListeners)
                {
                    if (ipe.Port == targetPort)
                    {
                        skipPort = true;
                        break;
                    }
                }
                if (skipPort)
                {
                    continue;
                }
                UDPNode = new UdpClient(targetPort);
                break;

            }
        }

        void UDPListeningThread()
        {
            while (!abortWirelessCommunications)
            {
                IAsyncResult listen = UDPNode.BeginReceive(null, null);
                int tries = 0;
                while (!listen.IsCompleted && WirelessThreadActive && tries <= 1000)
                {
                    Thread.Sleep(1);
                    tries++;
                }
                IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, 0);
                if (!(listen.IsCompleted && WirelessThreadActive))
                {
                    try
                    {
                        UDPNode.EndReceive(listen, ref endpoint);
                    }
                    catch (Exception e)
                    {
                        ErrorHandler.AddErrorToLog(e);
                    }

                    continue;
                }

                try
                {
                    byte[] buffer = UDPNode.EndReceive(listen, ref endpoint);
                    RecievePacket(endpoint.Address, endpoint.Port, buffer);
                }
                catch (Exception e)
                {
                    ErrorHandler.AddErrorToLog(e);
                }
            }
        }

        public abstract void RecievePacket(IPAddress address, int port, byte[] data);

        public void Cleanup()
        {
            abortWirelessCommunications = true;
            UDPNode.Close();
            UDPNode.Dispose();
        }

        public byte[] GenerateProperDataPacket(byte[] information, packetType dataType, Dictionary<packetType, int> lastSentMessageIDRecords)
        {
            if (!lastSentMessageIDRecords.ContainsKey(dataType))
            {
                lastSentMessageIDRecords.Add(dataType, int.MinValue);
            }
            List<byte> holster = new List<byte>();
            holster.Add((byte)dataType);
            holster.AddRange(Convert.ToByte(lastSentMessageIDRecords[dataType]));
            holster.AddRange(information);
            lastSentMessageIDRecords[dataType]++;

            return holster.ToArray();
        }
        public void SendPacketToGivenEndpoint(IPEndPoint target, byte[] packet)
        {
            try
            {
                UDPNode.Send(packet, packet.Length, target);
            }
            catch (Exception e)
            {
                ErrorHandler.AddErrorToLog(e);
            }
        }
    }

    public class DataPacket
    {
        public IPAddress address;
        public int port;

        public WirelessCommunicator.packetType stowedPacketType;

        public byte[] contents;

        public static DataPacket GetDataPacket(IPAddress addressGiven, int portGiven, byte[] packetBytes, Dictionary<WirelessCommunicator.packetType, int> packetDictionary)
        {
            const int packetHeaderSize = sizeof(byte) + sizeof(int);
            if (packetBytes.Length < packetHeaderSize)
            {
                return null;
            }
            DataPacket dp = new DataPacket();
            dp.address = addressGiven;
            dp.port = portGiven;
            dp.stowedPacketType = (WirelessCommunicator.packetType)packetBytes[0];
            int packetNumber = Convert.ToInt(packetBytes, 1);

            if (!Enum.IsDefined(typeof(WirelessCommunicator.packetType), dp.stowedPacketType))
            {
                return null;
            }
            if (!packetDictionary.ContainsKey(dp.stowedPacketType))
            {
                packetDictionary.Add(dp.stowedPacketType, packetNumber - 1);
            }
            if (packetDictionary[dp.stowedPacketType] >= packetNumber)
            {
                return null;
            }
            packetDictionary[dp.stowedPacketType] = packetNumber;

            dp.contents = packetBytes.Skip(packetHeaderSize).ToArray();
            return dp;
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
