using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;

namespace BigBrotherClientWPF
{
    class UdpClientDiscovery
    {
        public static void Discover()
        {
            try
            {
                using UdpClient udp = new UdpClient();

                udp.EnableBroadcast = true;
                udp.Client.ReceiveTimeout = 3000;
                udp.Client.Bind(new IPEndPoint(IPAddress.Any, 0));

                IPEndPoint broadcast = new IPEndPoint(
                    IPAddress.Parse("255.255.255.255"), 6768);

                byte[] msg = Encoding.UTF8.GetBytes("DISCOVER_SERVER");

                udp.Send(msg, msg.Length, broadcast);

                Debug.WriteLine("Sent DISCOVER_SERVER");

                IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);

                byte[] response = udp.Receive(ref remote);

                string text = Encoding.UTF8.GetString(response);

                Debug.WriteLine($"Response: {text} from {remote.Address}");

                if (text.StartsWith("SERVER:"))
                {
                    var parts = text.Replace("SERVER:", "").Split(':');

                    string ip = parts[0];
                    int port = int.Parse(parts[1]);

                    Debug.WriteLine($"FOUND SERVER: {ip}:{port}");
                }
            }
            catch (SocketException)
            {
                Debug.WriteLine("No response (timeout or blocked UDP)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}