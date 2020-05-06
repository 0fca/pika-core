using System;
using System.Net.Sockets;

namespace PikaCore.Areas.Api.v1.Services.Utils
{
    public static class Pinger
    {
        public static bool Ping(string address, int port)
        {
            var pingable = false;
            try
            {
                TcpClient client = new TcpClient(address, port);
                pingable = client.Connected;
                client.Dispose();
            }
            catch (Exception e)
            {
                // ignore
            }
            return pingable;
        }
    }
}