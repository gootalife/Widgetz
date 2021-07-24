using Newtonsoft.Json;
using PInvoke;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            MoveZToBottom();
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
                        if(!commonSettings.OrEmptyIfNull().Any(config => config.WidgetName == widget.WidgetName)) {
                            newConfigs.Add(new CommonSetting { WidgetName = widget.WidgetName, PosX = 0, PosY = 0, AutoBoot = false });
                        }
                    }
                    var configsList = commonSettings.OrEmptyIfNull().ToList();
                    configsList.AddRange(newConfigs);
                    commonSettings = configsList;
                } else {
                    // 共通設定が無いなら各ウィジェット毎に新規作成・保存
                    var newConfigs = new List<CommonSetting>();
                    foreach(var widget in widgets) {
                        newConfigs.Add(new CommonSetting { WidgetName = widget.WidgetName, PosX = 0, PosY = 0, AutoBoot = false });
                    }
                    var configsList = commonSettings.OrEmptyIfNull().ToList();
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
                            Name = widget.WidgetName,
                            IsChecked = isRunning[widget.WidgetName]
                        };
                        var setting = commonSettings.OrEmptyIfNull().First(config => config.WidgetName == widget.WidgetName);
                        // クリックすると起動
                        item.Click += async (s, e) => {
                            if(!isRunning[widget.WidgetName]) {
                                await BootWidget(widget, setting);
                            } else {
                                RemoveWidget(widget);
                            }
                            item.IsChecked = isRunning[widget.WidgetName];
                        };
                        _ = WidgetMenu.Items.Add(item);
                    }
                }), null);
            });
        }

        private async Task AutoBoot() {
            await Task.Run(async () => {
                foreach(var widget in widgets) {
                    var config = commonSettings.OrEmptyIfNull().First(config => config.WidgetName == widget.WidgetName);
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

        private void SettingMenu_ClickAsync(object sender, RoutedEventArgs e) {
            var settingWindow = new SettingWindow(widgets, commonSettings);
            _ = settingWindow.ShowDialog();
        }

        private void CloseMenu_Click(object sender, RoutedEventArgs e) {
            Close();
        }

        private void RemoveWidget(IWidget widget) {
            Canvas.Children.Remove(widget.WidgetControl);
            isRunning[widget.WidgetName] = false;
        }

        private static void MoveZToBottom() {
            var window = Process.GetCurrentProcess().MainWindowHandle;
            _ = User32.SetWindowPos(window, new IntPtr(1), 0, 0, 0, 0, User32.SetWindowPosFlags.SWP_NOSIZE | User32.SetWindowPosFlags.SWP_NOMOVE);
        }

        private void Window_Activated(object sender, EventArgs e) {
            MoveZToBottom();
        }
    }
}
