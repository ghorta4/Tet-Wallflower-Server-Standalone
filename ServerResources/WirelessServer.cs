using System;
using Guildleader;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

namespace ServerResources
{
    public class WirelessServer : WirelessCommunicator
    {
        const int maxPacketSize = 508;
        public List<ClientInfo> clients = new List<ClientInfo>();

        public override void Initialize()
        {
            UDPNode = new UdpClient(defaultPort, AddressFamily.InterNetwork);
            ErrorHandler.AddMessageToLog("Wireless server initialized.");
        }

        public void Update()
        {
            for (int i = 0; i < clients.Count; i++)
            {
                clients[i]?.Update();
            }
            while (packets.Count > 0)
            {
                ProcessLatestPacket();
            }

            List<ClientInfo> clientsToRemove = new List<ClientInfo>();
            DateTime now = DateTime.Now;
            clientsToRemove.AddRange(clients.FindAll(x => x != null && (now - x.lastRecievedMessage).TotalSeconds > 10 && (now - x.creationDate).TotalMilliseconds > 900));
            foreach (ClientInfo client in clientsToRemove)
            {
                ErrorHandler.AddMessageToLog($"Client at {client.address} has left the game.");
                clients.Remove(client);
            }
        }

        public override void RecievePacket(IPAddress address, int port, byte[] data)
        {
            ClientInfo target = null;
            foreach (ClientInfo ci in clients)
            {
                if (ci.address.ToString() == address.ToString() && ci.port == port)
                {
                    target = ci;
                    break;
                }
            }
            if (target == null)
            {
                target = new ClientInfo(address, port);
                clients.Add(target);
                ErrorHandler.AddMessageToLog("New client connected! :D");
            }
            DataPacket dp = DataPacket.GetDataPacket(address, port, data, target.dataSequencingDictionary);
            if (dp == null)
            {
                return;
            }
            ClientDataPacket cdp = new ClientDataPacket(dp);
            cdp.relevantClient = target;

            packets.Enqueue(cdp);
        }

        public void ProcessLatestPacket()
        {
            ClientDataPacket dp = packets.Dequeue() as ClientDataPacket;
            dp.relevantClient.lastRecievedMessage = DateTime.Now;
            switch (dp.stowedPacketType)
            {
                case PacketType.heartbeatPing:
                    break;
                case PacketType.requestPingback:
                    SendDataToOneClient(dp.relevantClient, PacketType.heartbeatPing, new byte[0], 1);
                    break;
                case PacketType.largePacketRepairRequest:
                    RespondToLargePacketRepairRequest(dp.relevantClient, dp.contents);
                    break;
                default:
                    ErrorHandler.AddErrorToLog(new Exception("Unhandled packet type: " + dp.stowedPacketType));
                    break;
            }
        }

        public void SendDataToOneClient(ClientInfo client, PacketType type, byte[] message, int repeats)
        {
            byte[] assembledPacket = GenerateProperDataPacket(message, type, client.sentDataRecords);
            if (assembledPacket.Length > maxPacketSize)
            {
                byte[][] split = client.lobh.BreakBytesIntoSendableSegments(assembledPacket, maxPacketSize - 12);
                SendManyDatasToOneClient(client, PacketType.largeObjectPacket, client.sentDataRecords, split, repeats);
                return;
            }

            SendPacketToGivenEndpoint(client.GetIPEndpoint, assembledPacket);
        }
        public void SendManyDatasToOneClient(ClientInfo client, PacketType type, Dictionary<PacketType, int> records, byte[][] messages, int repeats)
        {
            foreach (byte[] message in messages)
            {
                byte[] fullPack = GenerateProperDataPacket(message, type, records);
                for (int i = 0; i < repeats; i++)
                {
                    SendPacketToGivenEndpoint(client.GetIPEndpoint, fullPack);
                }
            }
        }
        public void SendDataToAllClients(PacketType type, byte[] contents, int repeats)
        {
            for (int i = 0; i < clients.Count; i++) //handled this way in case clients are removed mid-process
            {
                SendDataToOneClient(clients[i], type, contents, repeats);
            }
        }

        //server responses
        public void RespondToLargePacketRepairRequest(ClientInfo client, byte[] message)
        {
            if (message.Length <= sizeof(short))
            {
                return;
            }
            List<byte> toList = new List<byte>(message);
            short id = Guildleader.Convert.ToShort(message);
            toList.RemoveRange(0, sizeof(short));
            List<int> partsToRecover = new List<int>();
            while (toList.Count >= sizeof(int))
            {
                partsToRecover.Add(Guildleader.Convert.ToInt(toList.ToArray()));
                toList.RemoveRange(0, sizeof(int));
            }

            byte[][] toResend = client.lobh.GetPartsOfRecentlySentPacket(id, partsToRecover);

            foreach (byte[] resend in toResend)
            {
                SendDataToOneClient(client, PacketType.largeObjectPacket, resend, 2);
            }
        }
    }

    public class ClientDataPacket : DataPacket
    {
        public ClientInfo relevantClient;
        public ClientDataPacket(DataPacket toCopy) : base(toCopy) { }
    }

    public class ClientInfo
    {
        //Each client has its own largeObjectByteHandler. If the server only used 1 for all clients, a number of issues could arise.
        //1. The LOBH can only handle short.max number of messages at a time. Exceeding this may be rare for only a couple of clients, but
        //it could reasonably happen if a large library exists for the game AND many clients try to download it at once.
        //2. There is a security risk- since a client can ask for pieces to be recovered based on the message ID, they could post
        //an ID that another client asked for to get confidential information from them.
        public LargeObjectByteHandler lobh = new LargeObjectByteHandler();
        public IPAddress address; public int port;
        public IPEndPoint GetIPEndpoint { get { return new IPEndPoint(address, port); } }

        public readonly DateTime creationDate = DateTime.Now;

        public DateTime lastRecievedMessage;

        public ClientInformationGrantingCooldowns cooldowns = new ClientInformationGrantingCooldowns();

        public void Update()
        {
            lobh.RunCleanup();
        }

        public ClientInfo(IPAddress addressGiven, int portGiven)
        {
            address = addressGiven; port = portGiven;
        }

        //keep track of what kind of data type is read/the associated ID and don't use out of order packets
        public Dictionary<WirelessCommunicator.PacketType, int> sentDataRecords = new Dictionary<WirelessCommunicator.PacketType, int> { };

        //this value stores what the latest processed request ID was
        public Dictionary<WirelessCommunicator.PacketType, int> dataSequencingDictionary = new Dictionary<WirelessCommunicator.PacketType, int> { };
    }

    public class ClientInformationGrantingCooldowns
    {
        public DateTime lastChunkUpdateGiven;
    }
}
