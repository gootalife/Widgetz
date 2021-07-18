using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Widgetz.Interface;

namespace Widgetz {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private IEnumerable<IWidget> widgets = new List<IWidget>();
        private readonly Dictionary<string, bool> isRunning = new();
        private IEnumerable<CommonSetting> commonSettings = new List<CommonSetting>();

        public MainWindow() {
            InitializeComponent();
            // Semaphoreクラスのインスタンスを生成し、アプリケーション終了まで保持する
            const string SemaphoreName = "Widgetz";
            using var semaphore = new System.Threading.Semaphore(1, 1, SemaphoreName, out var createdNew);
            if(!createdNew) {
                _ = MessageBox.Show("すでに起動しています", "多重起動", MessageBoxButton.OK, MessageBoxImage.Hand);
                Close();
            }
        }

        private async void Window_LoadedAsync(object sender, RoutedEventArgs e) {
            try {
                TaskTrayIcon.Icon = Properties.Resources.Icon32;
                // ウィジェットのロード
                var widgetsPath = $@"{Directory.GetCurrentDirectory()}\Widgets";
                if(Directory.Exists(widgetsPath)) {
                    widgets = WidgetLorder.GetWidgets(widgetsPath);
                }
                // 設定のロード
                var settingPath = $@"{Directory.GetCurrentDirectory()}\CommonSettings.json";
                if(File.Exists(settingPath)) {
                    var json = File.ReadAllText(settingPath);
                    commonSettings = JsonConvert.DeserializeObject<IEnumerable<CommonSetting>>(json);
                    var newConfigs = new List<CommonSetting>();
                    foreach(var widget in widgets) {
                        // 共通設定が存在しないなら新規作成
                        if(!commonSettings.Any(config => config.WidgetName == widget.WidgetName)) {
                            newConfigs.Add(new CommonSetting { WidgetName = widget.WidgetName, PosX = 0, PosY = 0, AutoBoot = false });
                        }
                    }
                    var configsList = commonSettings.ToList();
                    configsList.AddRange(newConfigs);
                    commonSettings = configsList;
                } else {
                    // 共通設定が無いなら各ウィジェット毎に新規作成・保存
                    var newConfigs = new List<CommonSetting>();
                    foreach(var widget in widgets) {
                        newConfigs.Add(new CommonSetting { WidgetName = widget.WidgetName, PosX = 0, PosY = 0, AutoBoot = false });
                    }
                    var configsList = commonSettings.ToList();
                    configsList.AddRange(newConfigs);
                    commonSettings = configsList;
                    var json = JsonConvert.SerializeObject(commonSettings, Formatting.Indented);
                    File.WriteAllText(settingPath, json);
                }
                foreach(var widget in widgets) {
                    if(!isRunning.ContainsKey(widget.WidgetName)) {
                        isRunning.Add(widget.WidgetName, false);
                    } else {
                        _ = MessageBox.Show(@$"重複するウィジェット名({widget.WidgetName})があります");
                    }
                }
                await AutoBoot();
                await GenerateWidgetMenu();
            } catch {
                _ = MessageBox.Show("Initialize failed.");
                Close();
            }
        }
        private async Task GenerateWidgetMenu() {
            await Task.Run(async () => {
                await Dispatcher.BeginInvoke(new Action(() => {
                    foreach(var widget in widgets) {
                        var item = new MenuItem {
                            Header = widget.WidgetName,
                            Name = widget.WidgetName
                        };
                        var setting = commonSettings.First(config => config.WidgetName == widget.WidgetName);
                        var bootMenu = new MenuItem {
                            Header = isRunning[widget.WidgetName] ? "終了" : "起動"
                        };
                        // クリックすると起動
                        bootMenu.Click += async (s, e) => {
                            if(!isRunning[widget.WidgetName]) {
                                await BootWidget(widget, setting);
                            } else {
                                RemoveWidget(widget);
                            }
                            bootMenu.Header = isRunning[widget.WidgetName] ? "終了" : "起動";
                        };
                        _ = item.Items.Add(bootMenu);
                        _ = WidgetMenu.Items.Add(item);
                    }
                }), null);
            });
        }

        private async Task AutoBoot() {
            await Task.Run(async () => {
                foreach(var widget in widgets) {
                    var config = commonSettings.First(config => config.WidgetName == widget.WidgetName);
                    if(config.AutoBoot) {
                        await BootWidget(widget, config);
                    }
                }
            });
        }

        private async Task BootWidget(IWidget widget, CommonSetting config) {
            await Task.Run(async () => {
                await Dispatcher.BeginInvoke(new Action(() => {
                    var control = widget.WidgetControl;
                    _ = Canvas.Children.Add(control);
                    Canvas.SetLeft(control, config.PosX);
                    Canvas.SetTop(control, config.PosY);
                }), null);
            });
            isRunning[widget.WidgetName] = true;
        }

        private async void SettingMenu_ClickAsync(object sender, RoutedEventArgs e) {
            var settingWindow = new SettingWindow(widgets, commonSettings);
            _ = settingWindow.ShowDialog();
            // 変更後の設定を取得
            commonSettings = settingWindow.CommonSettings;
            var settingPath = $@"{Directory.GetCurrentDirectory()}\CommonSettings.json";
            var json = JsonConvert.SerializeObject(commonSettings, Formatting.Indented);
            await File.WriteAllTextAsync(settingPath, json);
        }

        private void CloseMenu_Click(object sender, RoutedEventArgs e) {
            Close();
        }

        private void RemoveWidget(IWidget widget) {
            Canvas.Children.Remove(widget.WidgetControl);
            isRunning[widget.WidgetName] = false;
        }
    }
}
