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

                _ = ListenLoop();
                _ = PingLoop();
                _ = ScreenshotLoop();
            };
        }

        
        void InitTray()
        {
            trayIcon = new NotifyIcon();
            trayIcon.Icon = new Icon("eye.ico");
            trayIcon.Visible = true;
            trayIcon.Text = "Big Brother is watching";

            var menu = new ContextMenuStrip();

            menu.Items.Add("Status", null, (s, e) =>
            {
                System.Windows.MessageBox.Show("Aplikacja działa w tle");
            });

            menu.Items.Add("Pokaż okno", null, (s, e) =>
            {
                this.Show();
                this.WindowState = WindowState.Normal;
            });

            menu.Items.Add("Ukryj", null, (s, e) =>
            {
                this.Hide();
            });

            menu.Items.Add("Wyjdź", null, (s, e) =>
            {
                trayIcon.Visible = false;
                System.Windows.Application.Current.Shutdown();
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
                client = new TcpClient();
                client.NoDelay = true;

                await client.ConnectAsync("192.168.18.102", 6767);
                stream = client.GetStream();

                stream.ReadTimeout = 15000;
                stream.WriteTimeout = 15000;

                Debug.WriteLine("Connected to server");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Connect error: " + ex);
            }
        }

        async Task ListenLoop()
        {
            try
            {
                while (true)
                {
                    byte[] lengthBytes = new byte[4];
                    int read = await ReadExact(lengthBytes, 4);
                    if (read == 0) return;

                    int length = BitConverter.ToInt32(lengthBytes, 0);

                    if (length <= 0 || length > 10_000_000)
                        return;

                    byte[] buffer = new byte[length];
                    await ReadExact(buffer, length);

                    using var ms = new MemoryStream(buffer);
                    using var br = new BinaryReader(ms, Encoding.UTF8);

                    string type = br.ReadString();

                    switch (type)
                    {
                        case "CMD":
                            HandleCommand(br.ReadString());
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Listen error: " + ex);
            }
        }

        void HandleCommand(string cmd)
        {
            Debug.WriteLine("CMD: " + cmd);

            if (cmd == "LOCK")
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (!IsLockOpen())
                        new LockWindow().Show();
                });
            }

            if (cmd == "UNLOCK")
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (Window w in System.Windows.Application.Current.Windows)
                        if (w is LockWindow)
                            w.Close();
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
                    await SendPacket(bw =>
                    {
                        bw.Write("PING");
                    });
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
                    byte[] img = CaptureScreenshot();

                    await SendPacket(bw =>
                    {
                        bw.Write("SCREENSHOT");
                        bw.Write(img.Length);
                        bw.Write(img);
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }

                await Task.Delay(5000);
            }
        }

        async Task SendPacket(Action<BinaryWriter> writeAction)
        {
            if (stream == null) return;

            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms, Encoding.UTF8);

            writeAction(bw);

            byte[] packet = ms.ToArray();
            byte[] length = BitConverter.GetBytes(packet.Length);

            await stream.WriteAsync(length);
            await stream.WriteAsync(packet);
        }

        async Task<int> ReadExact(byte[] buffer, int size)
        {
            int offset = 0;

            while (offset < size)
            {
                int read = await stream.ReadAsync(buffer, offset, size - offset);
                if (read == 0) return 0;

                offset += read;
            }

            return offset;
        }

        byte[] CaptureScreenshot()
        {
            var bounds = Screen.PrimaryScreen.Bounds;

            using Bitmap bmp = new Bitmap(bounds.Width, bounds.Height);
            using Graphics g = Graphics.FromImage(bmp);

            g.CopyFromScreen(0, 0, 0, 0, bounds.Size);

            using MemoryStream ms = new MemoryStream();

            // 🔥 JPEG zamiast PNG (dużo mniejsze)
            var encoder = GetJpegEncoder();
            var quality = new EncoderParameters(1);
            quality.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 50L);

            bmp.Save(ms, encoder, quality);

            return ms.ToArray();
        }

        ImageCodecInfo GetJpegEncoder()
        {
            foreach (var codec in ImageCodecInfo.GetImageEncoders())
                if (codec.FormatID == ImageFormat.Jpeg.Guid)
                    return codec;

            return null;
        }

        private void Lock_Click(object sender, RoutedEventArgs e)
        {
            LockWindow lockWindow = new LockWindow();
            lockWindow.ShowDialog();
        }
    }
}