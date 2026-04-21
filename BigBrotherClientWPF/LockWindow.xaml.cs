using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace BigBrotherClientWPF
{
    public partial class LockWindow : Window
    {
        public LockWindow()
        {
            InitializeComponent();
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c taskkill /f /im explorer.exe",
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }

        //private void Unlock_Click(object sender, RoutedEventArgs e)
        //{
        //    if (PinBox.Password == "1234")
        //    {
        //        this.Close();
        //    }
        //    else
        //    {
        //        System.Windows.MessageBox.Show("Zły PIN!");
        //    }
        //}

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.System && e.SystemKey == Key.F4)
            {
                e.Handled = true;
                return;
            }
            base.OnKeyDown(e);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            Process.Start("explorer.exe");
            e.Cancel = false; // pozwalamy zamknąć tylko po PIN
        }
    }
}