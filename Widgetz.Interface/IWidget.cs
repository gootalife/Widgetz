using System.Windows;
using System.Windows.Controls;

namespace Widgetz.Interface {
    public interface IWidget {
        public string WidgetName { get; }
        public UserControl WidgetControl { get; }
        public Window SettingWindow { get; }

    }
}
