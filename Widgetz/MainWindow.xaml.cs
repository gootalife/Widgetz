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
        }

        private async void Window_LoadedAsync(object sender, RoutedEventArgs e) {
            try {
                // ウィジェットのロード
                var widgetsPath = $@"{Directory.GetCurrentDirectory()}\Widgets";
                if(Directory.Exists(widgetsPath)) {
                    widgets = WidgetLorder.GetWidgets(widgetsPath);
                }
                // 設定のロード
                var configPath = $@"{Directory.GetCurrentDirectory()}\CommonSettings.json";
                if(File.Exists(configPath)) {
                    var json = File.ReadAllText(configPath);
                    commonSettings = JsonConvert.DeserializeObject<IEnumerable<CommonSetting>>(json);
                    var newConfigs = new List<CommonSetting>();
                    foreach(var widget in widgets) {
                        // 共通設定が存在しないなら新規作成
                        if(!commonSettings.Any(config => config.Name == widget.WidgetName)) {
                            newConfigs.Add(new CommonSetting { Name = widget.WidgetName, PosX = 0, PosY = 0, AutoBoot = false });
                        }
                    }
                    var configsList = commonSettings.ToList();
                    configsList.AddRange(newConfigs);
                    commonSettings = configsList;
                } else {
                    // 共通設定が無いなら各ウィジェット毎に新規作成・保存
                    var newConfigs = new List<CommonSetting>();
                    foreach(var widget in widgets) {
                        newConfigs.Add(new CommonSetting { Name = widget.WidgetName, PosX = 0, PosY = 0, AutoBoot = false });
                    }
                    var configsList = commonSettings.ToList();
                    configsList.AddRange(newConfigs);
                    commonSettings = configsList;
                    var json = JsonConvert.SerializeObject(commonSettings, Formatting.Indented);
                    File.WriteAllText(configPath, json);
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
                        var config = commonSettings.First(config => config.Name == widget.WidgetName);
                        var bootMenu = new MenuItem {
                            Header = isRunning[widget.WidgetName] ? "終了" : "起動"
                        };
                        // クリックすると起動
                        bootMenu.Click += async (s, e) => {
                            await BootWidget(widget, config);
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
                    var config = commonSettings.First(config => config.Name == widget.WidgetName);
                    if(config.AutoBoot) {
                        await BootWidget(widget, config);
                    }
                }
            });
        }

        private async Task BootWidget(IWidget widget, CommonSetting config) {
            if(!isRunning[widget.WidgetName]) {
                await Task.Run(async () => {
                    await Dispatcher.BeginInvoke(new Action(() => {
                        var control = widget.WidgetControl;
                        _ = Canvas.Children.Add(control);
                        Canvas.SetLeft(control, config.PosX);
                        Canvas.SetTop(control, config.PosY);
                    }), null);
                });
                isRunning[widget.WidgetName] = true;
            } else {
                _ = MessageBox.Show("既に起動しています。");
            }
        }

        private void SettingMenu_Click(object sender, RoutedEventArgs e) {
            var window = new SettingWindow(commonSettings);
            _ = window.ShowDialog();
        }

        private void CloseMenu_Click(object sender, RoutedEventArgs e) {
            Close();
        }
    }
}
