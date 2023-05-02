
using PdfiumViewer;
using Microsoft.Win32;
using System.Printing;
using PrinterApp.Properties;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Drawing.Printing;
using System;
using Escorp.Printing;
using Ghostscript.NET;
using Ghostscript.NET.Processor;
using System.Collections.Generic;

namespace PrinterApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string OUTPUT_PATH = @"D:\Printing\";
        const string OUTPUT_PATH_PS = @"D:\Printing\postscript\";

        private FileSystemWatcher _pdfWatcher;
        private FileSystemWatcher _psWatcher;

        public MainWindow()
        {
            InitializeComponent();
            lblUserName.Content = "Пользователь: " + Properties.Settings.Default.UserLogin;
            //createVirtualPrinter();
            watchPDF();
            watchPS();
        }

        private void createVirtualPrinter()
        {

            if(!Path.Exists(OUTPUT_PATH))
            {
                Directory.CreateDirectory(OUTPUT_PATH);
            }

            if(!Path.Exists(OUTPUT_PATH_PS))
            {
                Directory.CreateDirectory(OUTPUT_PATH_PS);
            }

            const string MonitorName = "mfilemon";
            const string PortName = "MYPORT:";
            const string DriverName = "MyDriver";
            const string PrinterName = "MyPrinter";

            const string BASE_PATH = "C:\\Users\\valit\\source\\repos\\PrinterApp\\PrinterApp\\NewDrivers\\";

            const string MonitorFile = BASE_PATH + "mfilemon.dll";
            const string DriverFile = BASE_PATH + "pscript5.dll";
            const string DriverDataFile = BASE_PATH + "testprinter.ppd";
            const string DriverConfigFile = BASE_PATH + "ps5ui.dll";
            const string DriverHelpFile = BASE_PATH + "pscript.hlp";

            PrintingApi.TryRestart();

            Monitor monitor = new Monitor(MonitorName,MonitorFile);
            var res = monitor.TryInstall();

            Port port = PrintingApi.OpenPort(PortName,monitor);
            bool portInstalled = port.TryInstall();
            Driver driver = PrintingApi.InstallDriver(DriverName,DriverFile,DriverDataFile,DriverConfigFile,DriverHelpFile,3,Escorp.Printing.Environment.Current,DataType.RAW,null,monitor);
            bool driverInstalled = port.TryInstall();

            Printer printer = new(PrinterName,PortName,DriverName);

            if(!printer.TryInstall(out PrintingException? ex))
                Console.WriteLine(ex?.Message);

            string keyName = $"SYSTEM\\CurrentControlSet\\Control\\Print\\Monitors\\{MonitorName}\\{PortName}";

            Registry.LocalMachine.CreateSubKey(keyName);


            using(RegistryKey regKey = Registry.LocalMachine.OpenSubKey(keyName,true))
            {
                if(regKey == null)
                    return;

                regKey.SetValue("OutputPath",OUTPUT_PATH_PS,RegistryValueKind.String);
                regKey.SetValue("FilePattern","%r_%c_%u_%Y%m%d_%H%n%s_%j.ps",RegistryValueKind.String);
                regKey.SetValue("Overwrite",0,RegistryValueKind.DWord);
                regKey.SetValue("UserCommand",string.Empty,RegistryValueKind.String);
                regKey.SetValue("ExecPath",string.Empty,RegistryValueKind.String);
                regKey.SetValue("WaitTermination",0,RegistryValueKind.DWord);
                regKey.SetValue("PipeData",0,RegistryValueKind.DWord);
            }

            PrintingApi.TryRestart();

        }

        void PrinterHandler(object sender,  FileSystemEventArgs e)
        {
            // Проверяем тип изменения состояния.
            switch(e.ChangeType)
            {
                // Событие создания файла. В этом ветвлении так же можно задать и другие события, при необходимости.
                case WatcherChangeTypes.Created:
                    try
                    {
                        GeneratePdf(e.FullPath,e.Name);

                        // Здесь мы можем обработать полученные данные.

                        File.Delete(e.FullPath);    // По завершению мы можем тут же удалить файл, если он больше не нужен.
                    }
                    catch(Exception ex) {
                        Console.WriteLine(ex.ToString()); 
                    }
                    break;
            }
        }

        
        private void watchPDF()
        {
            if(_pdfWatcher == null)
            {
                _pdfWatcher = new FileSystemWatcher();
                _pdfWatcher.Path = OUTPUT_PATH;
                _pdfWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                                       | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                _pdfWatcher.Filter = "*.*";
                _pdfWatcher.Changed += new FileSystemEventHandler(OnChangedPDF);
                _pdfWatcher.EnableRaisingEvents = true;
            }
        }

        private void watchPS()
        {
            if(_psWatcher == null)
            {
                _psWatcher = new FileSystemWatcher();
                _psWatcher.Path = OUTPUT_PATH_PS;
                _psWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                                       | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                _psWatcher.Filter = "*.*";
                _psWatcher.Changed += new FileSystemEventHandler(OnChangedPS);
                _psWatcher.EnableRaisingEvents = true;
            }
        }

        private void OnChangedPDF(object source,FileSystemEventArgs e)
        {
            var ext = Path.GetExtension(e.FullPath).ToLower();
            //Copies file to another directory.
            if(ext == ".pdf")
            {
                ItemInfo info = new ItemInfo();
                string fileName = e.Name;
                info.Name = fileName;
                info.Position = lvFiles.Items.Count + 1;
                info.Path = e.FullPath;
                Application.Current.Dispatcher.InvokeAsync(new Action(() =>
                {
                    lvFiles.Items.Add(info);
                }));
                
            }
        }

        private void OnChangedPS(object source,FileSystemEventArgs e)
        {
            GeneratePdf(e.FullPath, "Test"+DateTime.UtcNow.ToString("yyyy-MM-dd-mm-hh-ss")+ ".pdf");
        }

        private string GetFileNameFromPrintJob(PrintSystemJobInfo job)
        {
            // Read the job data stream
            byte[] buffer = new byte[1024];
            MemoryStream stream = new MemoryStream();
            Stream jobStream = job.JobStream;
            int bytesRead = 0;

            while((bytesRead = jobStream.Read(buffer,0,buffer.Length)) > 0)
            {
                stream.Write(buffer,0,bytesRead);
            }

            jobStream.Close();
            stream.Seek(0,SeekOrigin.Begin);

            // Parse the stream data to get the file name
            // In this example, we assume the file name was included in the job data as a text string
            // You may need to adjust this code to fit the format of your job data
            string data = new StreamReader(stream).ReadToEnd();

            int index = data.IndexOf("FILENAME=");
            if(index != -1)
            {
                int endIndex = data.IndexOf("\r\n",index);

                if(endIndex != -1)
                {
                    return data.Substring(index + "FILENAME=".Length,endIndex - (index + "FILENAME=".Length));
                }
            }

            return null;
        }

        public void btnAddFile_Click(object sender,RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "PDF files (*.pdf)|*.pdf";

            if(openFileDialog.ShowDialog() == true)
            {
                for(int i = 0; i < openFileDialog.SafeFileNames.Length; i++)
                {
                    if(Path.GetExtension(openFileDialog.FileNames[i]).ToLower() == ".pdf")
                    {
                        ItemInfo info = new ItemInfo();
                        string fileName = openFileDialog.SafeFileNames[i];
                        info.Name = fileName;
                        info.Position = lvFiles.Items.Count + 1;
                        info.Path = openFileDialog.FileNames[i];
                        lvFiles.Items.Add(info);
                    }
                }
            }
        }

        public async void btnUploadFile_Click(object sender,RoutedEventArgs e)
        {
            ItemInfo itemInfo = FromSender(sender);
            if(itemInfo == null)
            {
                return;
            }

            // Сохранение информации о том, какой путь к какой кнопке принадлежит
            string filePath = itemInfo.Path;

            // Создание объекта MultipartFormDataContent для отправки файлов
            var content = new MultipartFormDataContent();

            // Создание экземпляра HttpClient
            HttpClient client = new HttpClient();

            // Чтение содержимого файла в байтовый массив
            byte[] fileContent = System.IO.File.ReadAllBytes(filePath);

            client.DefaultRequestHeaders.Add("Authorization","Token " + Settings.Default.AuthToken);

            // Добавление содержимого файла в контент
            var fileContentByteArray = new ByteArrayContent(fileContent);
            content.Add(fileContentByteArray,"document",$"{itemInfo.Name}");

            // Отправка запроса на сервер
            var response = await client.PostAsync("https://sign-o.ru/api/v1/file/",content);

            // Обработка ответа сервера
            if(response.IsSuccessStatusCode)
            {
                MessageBox.Show("Файл успешно выгружен на сервер.");
            }
            else
            {
                MessageBox.Show($"Ошибка выгрузки файла: {response.ReasonPhrase}");
            }
        }

        private void btnDeleteFile_Click(object sender,RoutedEventArgs e)
        {
            ItemInfo itemInfo = FromSender(sender);
            if(itemInfo != null)
            {
                // Удаляем элемент из списка
                lvFiles.Items.Remove(itemInfo);
            }
        }

        private void btnToPrint_Click(object sender,RoutedEventArgs e)
        {
            ItemInfo itemInfo = FromSender(sender);
            if(itemInfo != null)
            {
                string filePath = itemInfo.Path;
                if(File.Exists(filePath))
                {
                    PdfDocument document = PdfDocument.Load(filePath);
                    PrintDocument printDocument = new PrintDocument();
                    int pageNumber = 0;
                    printDocument.PrintPage += (sender,e) =>
                    {
                        // Draw the PDF page on the printer graphics object
                        using(var image = document.Render(pageNumber++,e.PageBounds.Width,e.PageBounds.Height,PdfRenderFlags.CorrectFromDpi))
                        {
                            // Draw the image on the printer graphics object
                            e.Graphics.DrawImage(image,e.PageBounds);
                        }
                        bool hasMore = e.PageSettings.PrinterSettings.PrintRange == PrintRange.AllPages && pageNumber < document.PageCount;
                        Console.WriteLine($"Print page {pageNumber} with hasMore = {hasMore}");
                        // Set the HasMorePages property to indicate whether there are more pages to print
                        e.HasMorePages = hasMore;
                    };

                    PrintDialog printDialog = new PrintDialog();
                    if(printDialog.ShowDialog() == true)
                    {
                        // Get the printer settings from the selected printer
                        PrintQueue printQueue = new PrintQueue(new PrintServer(),printDialog.PrintQueue.Name);
                        printDocument.PrinterSettings.PrinterName = printQueue.FullName;
                        printDocument.PrinterSettings.PrintRange = PrintRange.AllPages;
                        // Print the document
                        printDocument.Print();
                    }

                    // Dispose of the PdfDocument object
                    document.Dispose();
                }
            }
        }

        private ItemInfo FromSender(object sender)
        {
            Button button = sender as Button;
            if(button != null)
            {
                // Получаем элемент данных из контекста кнопки
                ItemInfo itemInfo = button.DataContext as ItemInfo;
                return itemInfo;
            }

            return null;
        }

        private void MainGridPanel_Drop(object sender,DragEventArgs e)
        {
            if(e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                for(int i = 0; i < files.Length; i++)
                {
                    if(Path.GetExtension(files[i]).ToLower() == ".pdf")
                    {
                        ItemInfo info = new ItemInfo();
                        string fileName = files[i];
                        info.Name = fileName;
                        info.Position = lvFiles.Items.Count + 1;
                        info.Path = files[i];
                        lvFiles.Items.Add(info);
                    }
                }
            }
        }

        public void btnLogout_Click(object sender,RoutedEventArgs e)
        {
            if(Settings.Default.AuthToken != null)
            {
                MessageBoxResult result = MessageBox.Show("Вы действительно хотите выйти?","Выход",MessageBoxButton.YesNo,MessageBoxImage.Question);
                if(result == MessageBoxResult.Yes)
                {
                    Settings.Default.AuthToken = null;
                    Settings.Default.Save();
                    new Auth().Show();
                    this.Close();
                }
            }
        }

        public void GeneratePdf(string inputPath, string pdfFileName)
        {

            GhostscriptPipedOutput gsPipedOutput = new GhostscriptPipedOutput();

            // pipe handle format: %handle%hexvalue
            string outputPipeHandle = "%handle%" + int.Parse(gsPipedOutput.ClientHandle).ToString("X2");

            using(GhostscriptProcessor processor = new GhostscriptProcessor())
            {
                List<string> switches = new List<string>();
                switches.Add("-empty");
                switches.Add("-dQUIET");
                switches.Add("-dSAFER");
                switches.Add("-dBATCH");
                switches.Add("-dNOPAUSE");
                switches.Add("-dNOPROMPT");
                switches.Add("-sDEVICE=pdfwrite");
                switches.Add("-o" + outputPipeHandle);
                switches.Add("-q");
                switches.Add("-f");
                switches.Add(inputPath);

                try
                {
                    processor.StartProcessing(switches.ToArray(),null);

                    byte[] rawDocumentData = gsPipedOutput.Data;
                    File.WriteAllBytes(OUTPUT_PATH + pdfFileName, rawDocumentData);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    gsPipedOutput.Dispose();
                    gsPipedOutput = null;
                }
            }
        }

    }
}
