using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace Guildleader
{
    public static class WirelessCommunicator
    {
        public static void Initialize()
        {

        }
    }

    public class DataPacket
    {
        public enum dataTypes
        {

        }

        public IPAddress address;
        public short port;

        public byte[] contents;

        public DataPacket(IPAddress addressGiven, short portGiven, byte[] packetBytes)
        {
            address = addressGiven;
            port = portGiven;
            contents = packetBytes;
        }
    }
}
