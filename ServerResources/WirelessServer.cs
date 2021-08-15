using System;
using Guildleader;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

namespace ServerResources
{
    public class WirelessServer : WirelessCommunicator
    {
        List<ClientInfo> clients = new List<ClientInfo>();

        public override void Initialize()
        {
            UDPNode = new UdpClient(defaultPort, AddressFamily.InterNetwork);
            Console.WriteLine("Wireless server initialized.");
        }

        public void Update()
        {
            lobh.runCleanup();
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
                Console.WriteLine("New client connected.");
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
            SendPacketToGivenEndpoint(client.GetIPEndpoint, assembledPacket);
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
        public IPAddress address; public int port;
        public IPEndPoint GetIPEndpoint { get { return new IPEndPoint(address, port); } }

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
