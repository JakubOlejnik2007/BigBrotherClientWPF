using System.Windows;
using System.Windows.Input;

namespace BigBrotherClientWPF
{
    public partial class LockWindow : Window
    {
        public LockWindow()
        {
            InitializeComponent();
        }

        private void Unlock_Click(object sender, RoutedEventArgs e)
        {
            if (PinBox.Password == "1234")
            {
                this.Close(); // odblokowanie
            }
            else
            {
                System.Windows.MessageBox.Show("Zły PIN!");
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            //e.Handled = true; // blokuje klawiaturę
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = false; // pozwalamy zamknąć tylko po PIN
        }
    }
}