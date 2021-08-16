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
        List<ClientInfo> clients = new List<ClientInfo>();

        public override void Initialize()
        {
            UDPNode = new UdpClient(defaultPort, AddressFamily.InterNetwork);
            ErrorHandler.AddMessageToLog("Wireless server initialized.");
        }

        public void Update()
        {
            foreach (ClientInfo ci in clients)
            {
                ci.Update();
            }
            while (packets.Count > 0)
            {
                ProcessLatestPacket();
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
                ErrorHandler.AddMessageToLog("New client connected.");
            }
            DataPacket dp = DataPacket.GetDataPacket(address, port, data, target.dataSequencingDictionary);
            if (dp == null)
            {
                ErrorHandler.AddErrorToLog(new Exception("Invalid packet recieved."));
                return;
            }
            packets.Enqueue(dp);
        }

        public void ProcessLatestPacket()
        {
            DataPacket dp = packets.Dequeue();
            switch (dp.stowedPacketType)
            {
                default:
                    ErrorHandler.AddErrorToLog(new Exception("Unhandled packet type: " + dp.stowedPacketType));
                    break;
            }
        }

        public void sendDataToOneClient(ClientInfo client, PacketType type, byte[] message)
        {
            byte[] assembledPacket = GenerateProperDataPacket(message, type, client.sentDataRecords);

            if (assembledPacket.Length > maxPacketSize)
            {
                byte[][] split = client.lobh.breakBytesIntoSendableSegments(assembledPacket, maxPacketSize - 12);
                sendManyDatasToOneClient(client, PacketType.largeObjectPacket, client.sentDataRecords, split);
                return;
            }

            SendPacketToGivenEndpoint(client.GetIPEndpoint, assembledPacket);
        }
        public void sendManyDatasToOneClient(ClientInfo client, PacketType type, Dictionary<PacketType, int> records, byte[][] messages)
        {
            foreach (byte[] message in messages)
            {
                byte[] fullPack = GenerateProperDataPacket(message, type, records);
                SendPacketToGivenEndpoint(client.GetIPEndpoint, fullPack);
            }
        }
        public void sendDataToAllClients(PacketType type, byte[] contents)
        {
            for (int i = 0; i < clients.Count; i++) //handled this way in case clients are removed mid-process
            {
                sendDataToOneClient(clients[i], type, contents);
            }
        }
    }

    public class ClientInfo
    {
        //Each client has its own largeObjectByteHandler. If the server only used 1 for all clients, a number of issues could arise.
        //1. The LOBH can only handle short.max number of messages at a time. Exceeding this may be rare for only a couple of clients, but
        //it could reasonably happen if a large library exists for the game AND many clients try to download it at once.
        //2. There is a security risk- since a client can ask for pieces to be recovered based on the message ID, they could post
        //an ID that another client asked for to get confidential information from them.
        public largeObjectByteHandler lobh = new largeObjectByteHandler();
        public IPAddress address; public int port;
        public IPEndPoint GetIPEndpoint { get { return new IPEndPoint(address, port); } }

        public void Update()
        {
            lobh.runCleanup();
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
}
