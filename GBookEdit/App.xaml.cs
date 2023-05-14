using GBookEdit.WPF;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shell;

namespace GBookEdit
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static JumpList AppJumpList { get; } = new() { ShowRecentCategory = true, ShowFrequentCategory = false };

        public App()
        {
            JumpList.SetJumpList(Application.Current, AppJumpList);
        }

        public static void AddRecent(string path)
        {
            JumpList.AddToRecentCategory(path);
        }

        public static bool IsRunningAsAdministrator()
        {
            var windowsIdentity = WindowsIdentity.GetCurrent();
            var windowsPrincipal = new WindowsPrincipal(windowsIdentity);
            return windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static void RegisterAssociation()
        {
            var processFileName = Process.GetCurrentProcess().MainModule.FileName;

            if (!IsRunningAsAdministrator())
            {
                if (MessageBox.Show("This action requires administrator privileges.\nContinuing will attempt to run the task with elevated privileges.", "Guidebook",
                    MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
                {
                    var processStartInfo = new ProcessStartInfo(processFileName)
                    {
                        UseShellExecute = true,
                        Verb = "runas",
                        Arguments = "/register"
                    };
                    Process.Start(processStartInfo);
                }
                return;
            }

            var hkcr = Registry.ClassesRoot;
            using var appKey = hkcr.CreateSubKey("Guidebook.Xml");
            appKey.SetValue("", "Guidebook XML File");
            appKey.SetValue("FriendlyTypeName", "Guidebook XML File");
            appKey.SetValue("ContentType", "text/xml");
            appKey.SetValue("PerceivedType", "Document");
            using var defaultIcon = appKey.CreateSubKey("DefaultIcon");
            defaultIcon.SetValue("", processFileName + ",0");
            using RegistryKey shellKey = appKey.CreateSubKey("shell"),
                    openKey = shellKey.CreateSubKey("open"),
                    commandKey = openKey.CreateSubKey("command");
            commandKey.SetValue("", $"\"{processFileName}\" \"%1\"");
            using RegistryKey xmlKey = hkcr.CreateSubKey(".xml"),
                    openWithProgIds = xmlKey.CreateSubKey("OpenWithProgIDs");
            openWithProgIds.SetValue("Guidebook.Xml", "");
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            string openPath = "";

            if (e.Args.Length > 0)
            {
                if (e.Args[0] == "/register")
                {
                    RegisterAssociation();
                    Application.Current.Shutdown();
                    return;
                }
                else if (e.Args[0].StartsWith("/"))
                {
                    // Not implemented, ignore
                }
                else // open given path
                {
                    openPath = e.Args[0];
                }
            }

            var mainWindow = new MainWindow();
            mainWindow.Show();
            if (openPath != "")
            {
                mainWindow.OpenDocument(openPath);
            }
        }
    }
}
