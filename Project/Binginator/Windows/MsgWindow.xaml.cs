using System.Windows;

namespace Binginator.Windows {
    /// <summary>
    /// Interaction logic for MsgWindow.xaml
    /// </summary>
    public partial class MsgWindow : Window {
        public MsgWindow(string message) {
            InitializeComponent();

            TextMessage.Text = message;
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            Close();
        }
    }
}
