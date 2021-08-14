using System;
using Guildleader;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

namespace ServerResources
{
    public class WirelessServer : WirelessCommunicator
    {
        List<ClientInfo> clients;

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
            ClientInfo target = clients.Find(x => x.address == address && x.port == port);
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
        public Dictionary<WirelessCommunicator.packetType, int> sentDataRecords = new Dictionary<WirelessCommunicator.packetType, int> { };

        //this value stores what the latest processed request ID was
        public Dictionary<WirelessCommunicator.packetType, int> dataSequencingDictionary = new Dictionary<WirelessCommunicator.packetType, int> { };
    }
}
