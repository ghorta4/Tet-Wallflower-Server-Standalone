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

        public bool abortWirelessCommunications;
        public bool WirelessThreadActive { get { return !abortWirelessCommunications; } }

        public UdpClient UDPNode;

        public Queue<DataPacket> packets = new Queue<DataPacket> { };

        public enum PacketType
        {
            invalid,
            heartbeatPing,
            requestPingback,
            largeObjectPacket,
            largePacketRepairRequest,
            credentials, //IE logging in information, when that becomes relevant
            gameStateDataNotOrdered, //Anonymous placeholder - IE entities, chunks, etc.
            gameStateData, //Anonymous placeholder - IE recieving a previous packet would be really bad!
            chunkInfo,
            nearbyEntityInfo,
            entityIDToTrack, //helps clients adjust their cameras the right way
            requestIDToTrack //clients requesting the above function; asking for an entity to track
        }

        public const int defaultPort = 44500;

        public abstract void Initialize();

        Thread wirelessThread;
        public void StartListeningThread()
        {
            wirelessThread = new Thread(UDPListeningThread);
            const int SIO_UDP_CONNRESET = -1744830452;
            byte[] inValue = new byte[] { 0 }; // == false
            UDPNode.Client.IOControl(SIO_UDP_CONNRESET, inValue, null);
            wirelessThread.Start();
        }

        public void FindVariablePort()
        {
            List<IPEndPoint> allUsedListeners = new List<IPEndPoint>(IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners());
            int targetPort = 0;
            for (int i = 0; i < 100; i++)
            {
                bool skipPort = false;
                targetPort = defaultPort + i + 100;
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

        public byte[] GenerateProperDataPacket(byte[] information, PacketType dataType, Dictionary<PacketType, int> lastSentMessageIDRecords)
        {
            List<byte> holster = new List<byte>();
            holster.Add((byte)dataType);
            if (!lastSentMessageIDRecords.ContainsKey(dataType))
            {
                lastSentMessageIDRecords.Add(dataType, int.MinValue);
            }
            holster.AddRange(Convert.ToByte(lastSentMessageIDRecords[dataType]));
            holster.AddRange(information);
            if (!lastSentMessageIDRecords.ContainsKey(dataType))
            {
                lastSentMessageIDRecords.Add(dataType, int.MinValue);
            }
            lastSentMessageIDRecords[dataType]++;

            return holster.ToArray();
        }
        public void SendPacketToGivenEndpoint(IPEndPoint target, byte[] packet)
        {
            try
            {
                UDPNode.SendAsync(packet, packet.Length, target);
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

        public WirelessCommunicator.PacketType stowedPacketType;

        static WirelessCommunicator.PacketType[] PacketsAllowedOutOfOrder = new WirelessCommunicator.PacketType[] {
            WirelessCommunicator.PacketType.largeObjectPacket,
        WirelessCommunicator.PacketType.gameStateDataNotOrdered,
        WirelessCommunicator.PacketType.chunkInfo
        };

        public byte[] contents;

        public DataPacket() { }
        public DataPacket(DataPacket toCopy)
        {
            address = toCopy.address;
            port = toCopy.port;
            stowedPacketType = toCopy.stowedPacketType;
            contents = toCopy.contents;
        }

        public static DataPacket GetDataPacket(IPAddress addressGiven, int portGiven, byte[] packetBytes, Dictionary<WirelessCommunicator.PacketType, int> packetDictionary)
        {
            const int packetHeaderSize = sizeof(byte) + sizeof(int);
            if (packetBytes.Length < packetHeaderSize)
            {
                return null;
            }
            DataPacket dp = new DataPacket();
            dp.address = addressGiven;
            dp.port = portGiven;
            int packetNumber = Convert.ToInt(packetBytes, 1);
            dp.stowedPacketType = (WirelessCommunicator.PacketType)packetBytes[0];
            if (!Enum.IsDefined(typeof(WirelessCommunicator.PacketType), dp.stowedPacketType))
            {
                return null;
            }
            bool NeedToBeInOrder = !PacketsAllowedOutOfOrder.Contains(dp.stowedPacketType);
            if (NeedToBeInOrder && !packetDictionary.ContainsKey(dp.stowedPacketType))
            {
                packetDictionary.Add(dp.stowedPacketType, packetNumber - 1);
            }
            if (NeedToBeInOrder && packetDictionary[dp.stowedPacketType] >= packetNumber)
            {
                return null;
            }
            packetDictionary[dp.stowedPacketType] = packetNumber;

            dp.contents = packetBytes.Skip(packetHeaderSize).ToArray();
            return dp;
        }
    }

    public class LargeObjectByteHandler
    {

        public Dictionary<string, PacketAssembler> recievedSegments = new Dictionary<string, PacketAssembler> { };

        public Dictionary<short, SentPacket> recentlySegmentedPacket = new Dictionary<short, SentPacket> { };
        short lastSentPacket; //packet identifier
        public byte[][] BreakBytesIntoSendableSegments(byte[] longValue, short breakSize)
        {
            List<byte[]> segments = new List<byte[]>();

            short counter = 0;
            while (counter * breakSize < longValue.Length)
            {
                int distanceToGrab = Math.Min(breakSize, longValue.Length - counter * breakSize);
                List<byte> subSegment = new List<byte>();
                subSegment.AddRange(longValue.Skip(counter * breakSize).Take(distanceToGrab));

                subSegment.InsertRange(0, Convert.ToByte(counter));
                subSegment.InsertRange(0, Convert.ToByte(lastSentPacket));
                segments.Add(subSegment.ToArray());

                counter++;
            }

            int length = segments.Count();

            List<byte> packetIdentifyingInformation = new List<byte>();
            packetIdentifyingInformation.AddRange(Convert.ToByte(lastSentPacket));
            packetIdentifyingInformation.AddRange(Convert.ToByte(short.MaxValue));
            packetIdentifyingInformation.AddRange(Convert.ToByte((short)length));

            segments.Insert(0, packetIdentifyingInformation.ToArray());

            byte[][] array = segments.ToArray();
            recentlySegmentedPacket.Add(lastSentPacket, new SentPacket(array));

            lastSentPacket++;
            if (lastSentPacket > short.MaxValue - 10)
            {
                lastSentPacket = 0;
            }

            return array;
        }

        public void RecieveSegments(string identifier, byte[] segment)
        {
            if (segment.Length < sizeof(short) * 2)
            {
                ErrorHandler.AddErrorToLog("Warning: LOBH segment recieved was too short to be valid.");
                return;
            }
            short packetID = Convert.ToShort(segment, 0);
            short positionInPacket = Convert.ToShort(segment, sizeof(short));

            string fullName = string.Concat(identifier, packetID);
            if (!recievedSegments.ContainsKey(fullName))
            {
                recievedSegments.Add(fullName, new PacketAssembler());
            }
            recievedSegments[fullName].packetID = packetID;
            
            if (positionInPacket == short.MaxValue)
            {
                if (segment.Length < sizeof(short) * 3)
                {
                    ErrorHandler.AddErrorToLog("Warning: LOBH segment recieved was too short to be valid.");
                    return;
                }
                recievedSegments[fullName].maxSize = Convert.ToShort(segment, sizeof(short) * 2);
                return;
            }

            byte[] actualData = segment.Skip(sizeof(short) * 2).Take(segment.Length - sizeof(short) * 2).ToArray();

            recievedSegments[fullName].AddBytePacket(positionInPacket, actualData);
        }

        public void RunCleanup()
        {
            RemoveOldRecievedSegments(10);
            RemoveOldSentPackets(15);
        }
        public void RemoveOldRecievedSegments(int maxAgeOfPacketsInSeconds)
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
        public void RemoveOldSentPackets(int maxAgeOfPacketsInSeconds)
        {
            if (recentlySegmentedPacket.Count <= 0)
            {
                return;
            }
            DateTime now = DateTime.Now;
            List<short> toRemove = new List<short>();
            short[] toCheck = new short[recentlySegmentedPacket.Count + 100]; //extra space just in case the array grows
            recentlySegmentedPacket.Keys.CopyTo(toCheck, 0);
            foreach (short s in toCheck)
            {
                if (!recentlySegmentedPacket.ContainsKey(s))
                {
                    return;
                }
                SentPacket sp = recentlySegmentedPacket[s];
                if ((now - sp.sent).TotalSeconds >= maxAgeOfPacketsInSeconds)
                {
                    recentlySegmentedPacket.Remove(s);
                }
            }
        }

        public byte[][] GetPartsOfRecentlySentPacket(short segmentID, List<int> parts)
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

        public byte[][] GrabAllCompletedPackets()
        {
            string[] packsToCheck = recievedSegments.Keys.ToArray();
            List<byte[]> completed = new List<byte[]>();
            foreach (string s in packsToCheck)
            {
                if (recievedSegments[s].fullPacketAlreadyAcknowledged)
                {
                    continue;
                }
                byte[] pack = recievedSegments[s].GetAssembledPacket();
                if (pack != null)
                {
                    completed.Add(pack);
                    recievedSegments[s].fullPacketAlreadyAcknowledged = true;
                }
            }
            return completed.ToArray();
        }
    }

    public class PacketAssembler
    {
        public const int maxPacketSegments = 32768;

        public bool fullPacketAlreadyAcknowledged;

        public DateTime dateOfLastRecievedPart;
        byte[][] allSegments = new byte[maxPacketSegments][];
        public short maxSize = short.MaxValue;
        public short packetID = short.MaxValue;

        public void AddBytePacket(short segmentID, byte[] data)
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

        public byte[] GetAssembledPacket()
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

        public List<int> GetMissingParts()
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

    public class SentPacket
    {
        public DateTime sent;
        public byte[][] contents;

        public SentPacket(byte[][] info)
        {
            contents = info;
            sent = DateTime.Now;
        }
    }
}
