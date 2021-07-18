using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Widgetz.Interface;

namespace Widgetz {
    /// <summary>
    /// SettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingWindow : Window {
        public IEnumerable<CommonSetting> CommonSettings { get; private set; }
        private readonly IEnumerable<IWidget> widgets;
        private RoutedEventHandler routedEventHandler;
        public SettingWindow(IEnumerable<IWidget> widgets, IEnumerable<CommonSetting> settings) {
            InitializeComponent();
            this.widgets = widgets;
            CommonSettings = settings;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            WidgetListBox.ItemsSource = CommonSettings;
            WidgetListBox.DisplayMemberPath = "WidgetName";
            WidgetListBox.SelectedIndex = 0;
            SetConfigInformation();
        }

        private void SetConfigInformation() {
            var item = (CommonSetting)WidgetListBox.SelectedItem;
            WidgetName.Text = item.WidgetName;
            PosX.Text = item.PosX.ToString(DateTimeFormatInfo.InvariantInfo);
            PosY.Text = item.PosY.ToString(DateTimeFormatInfo.InvariantInfo);
            AutoBoot.IsChecked = item.AutoBoot;
            var widget = widgets.First(x => x.WidgetName == item.WidgetName);
            if(routedEventHandler != null) {
                IndividualSettingButton.Click -= routedEventHandler;
            }
            routedEventHandler = (s, e) => {
                _ = widget.SettingWindow.ShowDialog();
            };
            IndividualSettingButton.Click += routedEventHandler;
        }

        private void WidgetListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            SetConfigInformation();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) {
            Close();
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e) {
            var item = (CommonSetting)WidgetListBox.SelectedItem;
            try {
                item.PosX = int.Parse(PosX.Text, DateTimeFormatInfo.InvariantInfo);
                item.PosY = int.Parse(PosY.Text, DateTimeFormatInfo.InvariantInfo);
                item.AutoBoot = (bool)AutoBoot.IsChecked;
                WidgetListBox.SelectedItem = item;
                CommonSettings = WidgetListBox.Items.Cast<CommonSetting>();
                _ = MessageBox.Show($@"{WidgetName.Text}の設定を適用しました。\n(次回起動から適用)", "設定適用完了");
            } catch {
                _ = MessageBox.Show("入力値が不正です。", "設定適用不可", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
