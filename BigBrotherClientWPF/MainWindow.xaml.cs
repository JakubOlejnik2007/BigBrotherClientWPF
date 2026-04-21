using System;
using System.Net;
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
        TcpClient client;
        NetworkStream stream;

        NotifyIcon trayIcon;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += async (_, __) =>
            {
                this.Hide();

                InitTray();

                await Connect();

                _ = ListenForCommands();
                _ = PingLoop();
                _ = ScreenshotLoop();
            };
        }

        void InitTray()
        {
            trayIcon = new NotifyIcon();
            trayIcon.Icon = SystemIcons.Application;
            trayIcon.Visible = true;
            trayIcon.Text = "Big Brother is watching";

            var menu = new ContextMenuStrip();

            menu.Items.Add("Status", null, (s, e) =>
            {
                System.Windows.MessageBox.Show("Big Brother is indeed, watching you.");
            });

            trayIcon.ContextMenuStrip = menu;

            trayIcon.DoubleClick += (s, e) =>
            {
                this.Show();
                this.WindowState = WindowState.Normal;
            };
        }

        protected override void OnClosed(EventArgs e)
        {
            trayIcon.Visible = false;
            trayIcon.Dispose();
            base.OnClosed(e);
        }

        async Task Connect()
        {
            try
            {
                var server = DiscoverServer();

                if (server == null)
                {
                    Debug.WriteLine("Nie znaleziono serwera");
                    return;
                }

                client = new TcpClient();
                await client.ConnectAsync(server.Ip, server.Port);
                stream = client.GetStream();

                Debug.WriteLine($"Connected to server {server.Ip}:{server.Port}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Connect error: " + ex);
            }
        }

        DiscoveryResult DiscoverServer()
        {
            using var udp = new UdpClient();
            udp.EnableBroadcast = true;

            var broadcast = new IPEndPoint(IPAddress.Parse("10.10.10.255"), 9999);

            byte[] request = Encoding.UTF8.GetBytes("DISCOVER_SERVER");
            udp.Send(request, request.Length, broadcast);

            udp.Client.ReceiveTimeout = 5000;

            try
            {
                var remote = new IPEndPoint(IPAddress.Any, 0);
                byte[] response = udp.Receive(ref remote);

                string message = Encoding.UTF8.GetString(response);
                var parts = message.Split(':');

                return new DiscoveryResult
                {
                    Ip = parts[0],
                    Port = int.Parse(parts[1])
                };
            }
            catch
            {
                return null;
            }
        }

        async Task ListenForCommands()
        {
            byte[] buffer = new byte[1024];

            while (true)
            {
                try
                {
                    int bytes = await stream.ReadAsync(buffer, 0, buffer.Length);

                    if (bytes == 0)
                        continue;

                    string msg = Encoding.UTF8.GetString(buffer, 0, bytes);

                    HandleCommand(msg);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Listen error: " + ex);
                    break;
                }
            }
        }

        void HandleCommand(string msg)
        {
            Debug.WriteLine("CMD: " + msg);

            if (msg.Contains("CMD|LOCK"))
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (!IsLockOpen())
                    {
                        LockWindow lw = new LockWindow();
                        lw.Show();
                    }
                });
            }

            if (msg.Contains("CMD|UNLOCK"))
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (Window w in System.Windows.Application.Current.Windows)
                    {
                        if (w is LockWindow)
                            w.Close();
                    }
                });
            }
        }

        bool IsLockOpen()
        {
            foreach (Window w in System.Windows.Application.Current.Windows)
                if (w is LockWindow)
                    return true;

            return false;
        }

        async Task PingLoop()
        {
            while (true)
            {
                try
                {
                    await Send("PING");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }

                await Task.Delay(5000);
            }
        }

        async Task ScreenshotLoop()
        {
            while (true)
            {
                try
                {
                    await SendScreenshot();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }

                await Task.Delay(5000);
            }
        }

        async Task Send(string msg)
        {
            if (stream == null) return;

            byte[] data = Encoding.UTF8.GetBytes(msg);
            byte[] length = BitConverter.GetBytes(data.Length);

            await stream.WriteAsync(length, 0, length.Length);
            await stream.WriteAsync(data, 0, data.Length);
        }

        async Task SendScreenshot()
        {
            string base64 = CaptureScreenshotBase64();
            string message = $"SCREENSHOT|{base64}";

            byte[] data = Encoding.UTF8.GetBytes(message);
            byte[] length = BitConverter.GetBytes(data.Length);

            await stream.WriteAsync(length, 0, length.Length);
            await stream.WriteAsync(data, 0, data.Length);
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

        private void Lock_Click(object sender, RoutedEventArgs e)
        {
            LockWindow lockWindow = new LockWindow();
            lockWindow.ShowDialog();
        }
    }

    class DiscoveryResult
    {
        public string Ip { get; set; }
        public int Port { get; set; }
    }
}