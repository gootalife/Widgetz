using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Widgetz.Interface;

namespace Widgetz {
    /// <summary>
    /// SettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingWindow : Window {
        private IEnumerable<CommonSetting> commonSettings;
        private readonly IEnumerable<IWidget> widgets;
        private RoutedEventHandler routedEventHandler;
        public SettingWindow(IEnumerable<IWidget> widgets, IEnumerable<CommonSetting> settings) {
            InitializeComponent();
            this.widgets = widgets;
            commonSettings = settings;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            WidgetListBox.ItemsSource = commonSettings;
            WidgetListBox.DisplayMemberPath = "WidgetName";
            WidgetListBox.SelectedIndex = 0;
            SetSettingInformation();
        }

        private void SetSettingInformation() {
            var item = (CommonSetting)WidgetListBox.SelectedItem;
            var widget = widgets.OrEmptyIfNull().First(x => x.WidgetName == item.WidgetName);
            WidgetName.Text = item.WidgetName;
            WidgetWidth.Text = Math.Round(widget.WidgetControl.ActualWidth).ToString(CultureInfo.InvariantCulture);
            WidgetHeight.Text = Math.Round(widget.WidgetControl.ActualHeight).ToString(CultureInfo.InvariantCulture);
            PosX.Text = item.PosX.ToString(CultureInfo.InvariantCulture);
            PosY.Text = item.PosY.ToString(CultureInfo.InvariantCulture);
            AutoBoot.IsChecked = item.AutoBoot;
            if(routedEventHandler != null) {
                IndividualSettingButton.Click -= routedEventHandler;
            }
            routedEventHandler = (s, e) => {
                _ = widget.SettingWindow.ShowDialog();
            };
            IndividualSettingButton.Click += routedEventHandler;
        }

        private void WidgetListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            SetSettingInformation();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) {
            Close();
        }

        private async void ApplyButton_Click(object sender, RoutedEventArgs e) {
            var item = (CommonSetting)WidgetListBox.SelectedItem;
            try {
                item.PosX = int.Parse(PosX.Text, CultureInfo.InvariantCulture);
                item.PosY = int.Parse(PosY.Text, CultureInfo.InvariantCulture);
                item.AutoBoot = (bool)AutoBoot.IsChecked;
                WidgetListBox.SelectedItem = item;
                commonSettings = WidgetListBox.Items.Cast<CommonSetting>();
                var settingPath = $@"{Directory.GetCurrentDirectory()}\CommonSettings.json";
                var json = JsonConvert.SerializeObject(commonSettings, Formatting.Indented);
                await File.WriteAllTextAsync(settingPath, json);
                _ = MessageBox.Show($"{WidgetName.Text}の設定を保存しました。\n(次回起動から保存)", "設定保存完了");
            } catch {
                _ = MessageBox.Show("入力値が不正です。", "設定保存不可", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
