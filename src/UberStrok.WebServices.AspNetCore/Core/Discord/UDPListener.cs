using log4net;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UberStrok.WebServices.AspNetCore.WebService;

namespace UberStrok.WebServices.AspNetCore.Core.Discord
{
    public class UDPListener
    {
        public static readonly ILog Log = LogManager.GetLogger(typeof(UDPListener));

        public static UdpClient udpClient = new UdpClient(5070);

        public static void BeginListen()
        {
            try
            {
                _ = udpClient.BeginReceive(onReceive, null);
            }
            catch (Exception e)
            {
                AuthenticationWebService.Log.Error(e.ToString());
            }
        }

        private static void onReceive(IAsyncResult res)
        {
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 5070);
            byte[] received = udpClient.EndReceive(res, ref RemoteIpEndPoint);
            try
            {
                string returnData = Encoding.UTF8.GetString(received);
                if (!string.IsNullOrEmpty(returnData) && returnData.StartsWith("comm:"))
                {
                    SendDiscord(returnData[5..]);
                }
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
            BeginListen();
        }

        public static void Initialise()
        {
            new Thread(new ThreadStart(BeginListen)).Start();
        }

        public static void SendDiscord(string message, bool login = false)
        {
            try
            {
                if (login)
                {
                    CoreDiscord.SendLoginLog(message).GetAwaiter().GetResult();
                }
                else
                {
                    CoreDiscord.SendChannel(message).GetAwaiter().GetResult();
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
