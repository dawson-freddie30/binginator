using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Binginator.Events;
using Binginator.Models;
using Binginator.Windows.ViewModels;

namespace Binginator.Windows {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private MainViewModel _viewModel;

        public MainWindow() {
            _viewModel = new MainViewModel(new MainModel());

            InitializeComponent();

            DataContext = _viewModel;

            RichTextBoxLog.Document.Blocks.Clear();
            _viewModel.LogUpdated += DataContext_LogUpdated;
        }

        private void DataContext_LogUpdated(object sender, LogUpdatedEventArgs e) {
            BlockCollection blocks = RichTextBoxLog.Document.Blocks;

            if (e.Inline) {
                var range = new TextRange(RichTextBoxLog.Document.ContentEnd, RichTextBoxLog.Document.ContentEnd);
                range.Text = e.Data;
                range.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(e.Color));
            }
            else {
                Paragraph paragraph = new Paragraph(new Run(e.Data) { Foreground = new SolidColorBrush(e.Color) });
                blocks.Add(paragraph);
            }

            if (blocks.Count > 2000)
                blocks.Remove(blocks.FirstBlock);

            RichTextBoxLog.ScrollToEnd();
        }

        private void Window_Closed(object sender, System.EventArgs e) {
            _viewModel.Quit();
        }
    }
}