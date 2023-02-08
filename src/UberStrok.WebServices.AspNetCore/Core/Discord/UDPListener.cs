using log4net;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace UberStrok.WebServices.AspNetCore.Core.Discord
{
    public class UDPListener
    {
        public readonly ILog Log = LogManager.GetLogger(typeof(UDPListener));

        public UdpClient udpClient = new UdpClient(5070);

        private readonly CoreDiscord coreDiscord;
        public UDPListener(CoreDiscord coreDiscord)
        {
            this.coreDiscord = coreDiscord;
            Initialise();
            Console.WriteLine("UDP Listener Started.\n");
        }

        public void BeginListen()
        {
            try
            {
                _ = udpClient.BeginReceive(onReceive, null);
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
        }

        private void onReceive(IAsyncResult res)
        {
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 5070);
            byte[] received = udpClient.EndReceive(res, ref RemoteIpEndPoint);
            try
            {
                string returnData = Encoding.UTF8.GetString(received);
                if (!string.IsNullOrEmpty(returnData) && returnData.StartsWith("comm:"))
                {
                    Log.Info("Received trigger from comm server");
                    SendDiscord(returnData[5..]);
                }
                else if (!string.IsNullOrEmpty(returnData) && returnData.StartsWith("game:"))
                {
                    Log.Info("Received trigger from game server");
                    SendDiscord(returnData[5..], true);
                }
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
            BeginListen();
        }

        public void Initialise()
        {
            new Thread(new ThreadStart(BeginListen)).Start();
        }

        public async void SendDiscord(string message, bool login = false)
        {
            try
            {
                if (login)
                {
                    await coreDiscord.SendLoginLog(message);
                }
                else
                {
                    await coreDiscord.SendChannel(message);
                }
            }
            catch (Exception e)
            {
                Log.Error("Error handling discord socket:");
                Log.Error(e);
            }
        }
    }
}
