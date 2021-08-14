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
}
