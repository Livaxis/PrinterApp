
using PdfiumViewer;
using Microsoft.Win32;
using PrinterApp.Properties;
using System.IO;
using System.Net.Http;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Drawing.Printing;
using System;

namespace PrinterApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            lblUserName.Content = "Пользователь: " + Properties.Settings.Default.UserLogin;
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
    }
}
