using System;
using Guildleader;
using System.Net;
using System.Net.Sockets;

namespace ServerResources
{
    public class WirelessServer : WirelessCommunicator
    {
        public override void Initialize()
        {
            UDPNode = new UdpClient(defaultPort, AddressFamily.InterNetwork);
            Console.WriteLine("Wireless server initialized.");
        }
    }
}
