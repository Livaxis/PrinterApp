using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using PrinterApp.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


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
            string psFolder = psFolderTB.Text;
            Settings.Default.PsFolder = psFolder;
            string pdfFolder = pdfFolderTB.Text;
            Settings.Default.PdfFolder = pdfFolder;

            if(!tryCreateDirectory(psFolder)) { return; }

            if(!tryCreateDirectory(pdfFolder)) { return; }

            Settings.Default.PrinterName = printerNameTB.Text;
            Settings.Default.PortName = portNameTB.Text;

            if(PrinterUtils.isGhostScriptNotInstalled())
            {
                showDownloadChostscriptDialog();
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
            if(PrinterUtils.isGhostScriptNotInstalled())
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

    }
}
