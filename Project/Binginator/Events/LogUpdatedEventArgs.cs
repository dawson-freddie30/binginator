using System;
using System.Windows.Media;

namespace Binginator.Events {
    public class LogUpdatedEventArgs : EventArgs {
        public string Data { get; private set; }
        public Color Color { get; private set; }
        public bool Inline { get; private set; }

        public LogUpdatedEventArgs(string data, Color color, bool inline) {
            Data = data;
            Color = color;
            Inline = inline;
        }
    }
}
