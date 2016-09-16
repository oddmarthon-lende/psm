using PSMonitor;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Hardcodet.Wpf.TaskbarNotification;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace PSMControl
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : PSMonitor.Theme.Window
    {

        private TaskbarIcon _trayicon = new TaskbarIcon();

        private static PSMonitor.Powershell.Powershell _ps = new PSMonitor.Powershell.Powershell();

        private PSMonitor.Log LogToStore = (message, type) =>
        {

            if (type != EventLogEntryType.Error && type != EventLogEntryType.FailureAudit)
                return;

            PSM.Store().Write(new Envelope()
            {

                Path = "@errors",
                Entries = new Entry[1] {

                    new Entry {

                        Key = Environment.MachineName,
                        Type = typeof(string),
                        Value = message,
                        Timestamp = DateTime.Now

                    }
                }
            });

        };

        private FileSystemWatcher _watcher;
        
        public MainWindow()
        {

            App.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            InitializeComponent();
            
            Logger.OnLog += LogToStore;
            Logger.OnLog += (message, type) =>
            {

                if (!IsVisible)
                    return;

                TextBox box;

                try
                {

                    if (IsVisible)
                        Dispatcher.InvokeAsync(delegate {

                            switch (type)
                            {
                                case EventLogEntryType.Error:
                                case EventLogEntryType.Warning:

                                    box = errorOutputBox;
                                    statusText.Text = type + " : " + message;
                                
                                    break;

                                default:

                                

                                    box = informationOutputBox;

                                    break;

                            }

                            box.AppendText(DateTime.Now.ToString("o") + " : " + message + "\n");

                            if (Mouse.LeftButton != MouseButtonState.Pressed && box.LineCount > 0)
                                box.ScrollToLine(box.LineCount - 1);

                        });
                    }
                    catch(System.Threading.ThreadInterruptedException) { }
                

            };

            Closing += (sender, e) =>
            {
                e.Cancel = true;
                Hide();

                informationOutputBox.Clear();
                errorOutputBox.Clear();

            };

            _watcher = new FileSystemWatcher(Setup.Powershell.Directory);

            _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime;
            _watcher.Filter = "*.ps1";

            _watcher.Changed += new FileSystemEventHandler(OnChanged);
            _watcher.Created += new FileSystemEventHandler(OnChanged);
            _watcher.Deleted += new FileSystemEventHandler(OnChanged);
            _watcher.Renamed += new RenamedEventHandler(OnRenamed);

            if (Directory.Exists(Setup.Powershell.Directory))
            {
                _ps.Reload();
                _watcher.EnableRaisingEvents = true;
            }
            else
            {
                throw new DirectoryNotFoundException("Cannot find the directory: " + Setup.Powershell.Directory);
            }

            //Set the tray icon
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PSMControl.Icons.tray.ico"))
            {
                _trayicon.Icon = new System.Drawing.Icon(stream);
            }

            _trayicon.ToolTipText = "PSM";
            _trayicon.TrayMouseDoubleClick += (sender, args) =>
            {
                Show();
                Focus();
                
            };

            Hide();
            
        }
        
        private void OnChanged(object src, FileSystemEventArgs e)
        {
            _ps.Reload(e.FullPath);
        }

        private void OnRenamed(object src, FileSystemEventArgs e)
        {
            _ps.Reload(e.FullPath);
        }
    }
}
