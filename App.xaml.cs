using Ghostscript.NET.Processor;
using PrinterApp.Properties;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace PrinterApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender,StartupEventArgs e)
        {
            if(Application.Current != null)
            {
                if(String.IsNullOrEmpty(Settings.Default.AuthToken))
                {
                    Application.Current.MainWindow = new Auth();
                }
                else if(isSettingsNotFilled() || PrinterUtils.isGhostScriptNotInstalled())
                {
                    Application.Current.MainWindow = new SettingsWindow();
                }
                else
                {
                    Application.Current.MainWindow = new MainWindow();
                }
                Application.Current.MainWindow.Show();
            }
        }

        private bool isSettingsNotFilled()
        {
            return String.IsNullOrEmpty(Settings.Default.PsFolder) ||
                String.IsNullOrEmpty(Settings.Default.PdfFolder) ||
                String.IsNullOrEmpty(Settings.Default.PrinterName) ||
                String.IsNullOrEmpty(Settings.Default.PortName);
        }

        
    }
}
