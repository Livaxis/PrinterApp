using Escorp.Printing;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Shell;
using Org.BouncyCastle.Crypto.Tls;
using PrinterApp.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using Environment = System.Environment;

namespace PrinterApp
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {

        public SettingsWindow()
        {
            InitializeComponent();
        }

        public void psFolderPickButtonClick(object sender, RoutedEventArgs e)
        {
            psFolderTB.Text = choiceFolder();
        }

        public void pdfFolderPickButtonClick(object sender,RoutedEventArgs e)
        {
            pdfFolderTB.Text = choiceFolder();
        }

        public void downloadGhostscriptClick(object sender, RoutedEventArgs e)
        {
            openDownloadChostscriptPage();
        }

        public void saveSettingsClick(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            button.IsEnabled = false;
            string psFolder = psFolderTB.Text;
            Settings.Default.PsFolder = psFolder;
            string pdfFolder = pdfFolderTB.Text;
            Settings.Default.PdfFolder = pdfFolder;

            if(!tryCreateDirectory(psFolder)) {
                button.IsEnabled = true;
                return; 
            }

            if(!tryCreateDirectory(pdfFolder)) {
                button.IsEnabled = true;
                return; 
            }

            Settings.Default.PrinterName = printerNameTB.Text;
            Settings.Default.PortName = portNameTB.Text;

            if(PrinterUtils.IsGhostScriptNotInstalled())
            {
                showDownloadChostscriptDialog();
                button.IsEnabled = true;
                return;
            }

            if(!createVirtualPrinter())
            {
                button.IsEnabled = true;
                return;
            }

            Settings.Default.Save();
            openMainWindow();
        }

        private bool tryCreateDirectory(string path)
        {
            DirectoryInfo info = new DirectoryInfo(path);
            if(info.IsReadable() && info.IsWriteable())
            {
                if(!info.Exists)
                {
                    try
                    {
                        Directory.CreateDirectory(path);
                        return true;
                    }
                    catch(UnauthorizedAccessException)
                    {
                        MessageBox.Show($"Нет прав на создание папки по указанному пути\n {path}");
                        return false;
                    }
                    catch(Exception)
                    {
                        MessageBox.Show($"Невозможно создать папку по указанному пути\n {path}");
                        return false;
                    }
                }
                return true;
            } 
            else
            {
                MessageBox.Show($"Невозможно создать папку по указанному пути\n {path}");
                return false;
            }
            
        }

        private void openMainWindow()
        {
            new MainWindow().Show();
            this.Close();
        }

        private void showDownloadChostscriptDialog()
        {
            MessageBoxResult result = MessageBox.Show("Необходимо скачать и установить Chostscript для продолжения работы.", "Скачать",MessageBoxButton.YesNoCancel);
            if(result == MessageBoxResult.Yes)
            {
                openDownloadChostscriptPage();
            }
        }

        private void openDownloadChostscriptPage()
        {
            if(PrinterUtils.IsGhostScriptNotInstalled())
            {
                const string x32System = "https://github.com/ArtifexSoftware/ghostpdl-downloads/releases/download/gs10011/gs10011w32.exe";
                const string x64System = "https://github.com/ArtifexSoftware/ghostpdl-downloads/releases/download/gs10011/gs10011w64.exe";
                System.Diagnostics.Process.Start(new ProcessStartInfo
                {
                    FileName = Environment.Is64BitOperatingSystem ? x64System : x32System,
                    UseShellExecute = true
                });
                return;
            }
            MessageBox.Show("Ghostscript уже установлен. Повторная установка не требуется.");
        }

        public void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            if(Settings.Default.AuthToken != null)
            {
                MessageBoxResult result = MessageBox.Show("Вы действительно хотите выйти?","Выход",MessageBoxButton.YesNo,MessageBoxImage.Question);
                if(result == MessageBoxResult.Yes)
                {
                    SettingsUtils.Logout();
                    new Auth().Show();
                    this.Close();
                }
            }
        }

        private string choiceFolder()       
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            dialog.IsFolderPicker = true;
            if(dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                return dialog.FileName;
            }

            return string.Empty;
        }

        private bool createVirtualPrinter()
        {
            const string monitorName = "mfilemon";
            //const string driverName = "SignoDriver";
            const string driverName = "Microsoft MS-XPS Class Driver 2";

            string portName = portNameTB.Text;
            string printerName = printerNameTB.Text;

            string BASE_PATH = Directory.GetCurrentDirectory() + "\\NewDrivers\\";

            string monitorFile = BASE_PATH + "mfilemon.dll";
            string driverFile = BASE_PATH + "pscript5.dll";
            string DriverDataFile = BASE_PATH + "testprinter.ppd";
            string DriverConfigFile = BASE_PATH + "ps5ui.dll";
            string DriverHelpFile = BASE_PATH + "pscript.hlp";

            PrintingApi.TryRestart();

            Monitor monitor = new Monitor(monitorName,monitorFile);
            if(!PrinterUtils.IsMonitorInstalled(monitorName))
            {
                bool installed = monitor.TryInstall();
                if(!installed)
                {
                    MessageBox.Show("Безуспешная установка монитора. Попробуйте еще раз.");
                    return false;
                }
            }

            
            if(!PrinterUtils.IsPortInstalled(portName))
            {
                PrintingApi.OpenPort(portName,monitor);
                if(!PrinterUtils.IsPortInstalled(portName))
                {
                    MessageBox.Show($"Безуспешная установка порта {portName}. Попробуйте еще раз.");
                    return false;
                }

                if(!isRegistryConfigured(monitorName,portName))
                {
                    if(!configureMonitorRegistry(monitorName,portName))
                    {
                        deleteRegistryConfig(monitorName,portName);
                        return false;
                    }
                }
            }

            
            if(!PrinterUtils.IsDriverInstalled(driverName))
            {
                PrintingApi.InstallDriver(
                    driverName,
                    driverFile,
                    DriverDataFile,
                    DriverConfigFile,
                    DriverHelpFile,
                    3,
                    Escorp.Printing.Environment.Current,
                    DataType.RAW,
                    null,
                    monitor);

                if(!PrinterUtils.IsDriverInstalled(driverName))
                {
                    MessageBox.Show($"Безуспешная установка драйвера {driverName}. Попробуйте еще раз.");
                    return false;
                }
            }

            if(!PrinterUtils.IsPrinterInstalled(printerName))
            {
                Printer printer = new(printerName,portName,driverName);
                bool installed = printer.TryInstall(out PrintingException? ex);
                if(!installed)
                {
                    MessageBox.Show($"Безуспешная установка принтера {printerName} с указанной ошибкой {ex.Message}. Попробуйте еще раз.");
                    return false;
                }
            }

            
            MessageBox.Show($"Виртуальный принтер {printerName} установлен.");
            return true;
        }

        private bool configureMonitorRegistry(string monitorName, string portName)
        {
            string keyName = $"SYSTEM\\CurrentControlSet\\Control\\Print\\Monitors\\{monitorName}\\{portName}";
            try
            {
                Registry.LocalMachine.CreateSubKey(keyName);
                using(RegistryKey regKey = Registry.LocalMachine.OpenSubKey(keyName,true))
                {
                    if(regKey == null)
                        return false;

                    regKey.SetValue("OutputPath",       Settings.Default.PsFolder,      RegistryValueKind.String);
                    regKey.SetValue("FilePattern",      "file%i.ps",                    RegistryValueKind.String);
                    regKey.SetValue("Overwrite",        0,                              RegistryValueKind.DWord);
                    regKey.SetValue("UserCommand",      string.Empty,                   RegistryValueKind.String);
                    regKey.SetValue("ExecPath",         string.Empty,                   RegistryValueKind.String);
                    regKey.SetValue("WaitTermination",  0,                              RegistryValueKind.DWord);
                    regKey.SetValue("PipeData",         0,                              RegistryValueKind.DWord);
                }

                return true;
            }
            catch(UnauthorizedAccessException e)
            {
                Console.WriteLine(e?.Message);
                MessageBox.Show("Необходимо запустить от имени администратора.");
                return false;
            }
        }

        private bool isRegistryConfigured(string monitorName,string portName)
        {
            string keyName = $"SYSTEM\\CurrentControlSet\\Control\\Print\\Monitors\\{monitorName}\\{portName}";
            try
            {
                using(RegistryKey regKey = Registry.LocalMachine.OpenSubKey(keyName,true))
                {
                    if(regKey == null)
                        return false;
                    return regKey.GetValue("OutputPath").Equals(Settings.Default.PsFolder);
                }
            }
            catch(UnauthorizedAccessException e)
            {
                Console.WriteLine(e?.Message);
                return false;
            }
        }

        private bool deleteRegistryConfig(string monitorName,string portName)
        {
            string keyName = $"SYSTEM\\CurrentControlSet\\Control\\Print\\Monitors\\{monitorName}\\{portName}";
            try
            {
                Registry.LocalMachine.DeleteSubKey(keyName,true);
                return true;
            }
            catch(UnauthorizedAccessException e)
            {
                Console.WriteLine(e?.Message);
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e?.Message);
                return false;
            }
        }

    }
}
