using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Windows;
using PrinterApp.Properties;
using System.Net;
using Newtonsoft.Json;

namespace PrinterApp
{
    /// <summary>
    /// Логика взаимодействия для Auth.xaml
    /// </summary>
    public partial class Auth : Window
    {
        public Auth()
        {
            InitializeComponent();
        }
        private async void AuthButton_Click(object sender, RoutedEventArgs e)
        {
            // create HTTP client
            HttpClient client = new HttpClient();

            //ro2@clinicdelta.ru
            //Mm654321
            
            // create request content with login and password
            string login = LoginTextBox.Text;
            string password = PasswordTextBox.Password;

            Properties.Settings.Default.UserLogin = LoginTextBox.Text;
            Properties.Settings.Default.Save();

            // validate login and password
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Пожалуйста, введите логин и пароль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            AuthButton.IsEnabled = false;

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("email", login),
                new KeyValuePair<string, string>("password", password)
            });

            try
            {
                var response = await client.PostAsync("https://sign-o.ru/api/v1/token/login", content);
                response.EnsureSuccessStatusCode();
                string tokenJson = await response.Content.ReadAsStringAsync();
                var tokenData = JsonConvert.DeserializeObject<TokenData>(tokenJson);
                Settings.Default.AuthToken = tokenData?.Token;
                Settings.Default.Save();

                // обработка полученного токена
                new MainWindow().Show();
                this.Close();
            }
            catch (HttpRequestException ex)
            {
                // обработка ошибки запроса
                if (ex.StatusCode == HttpStatusCode.BadRequest)
                {
                    MessageBoxResult result = MessageBox.Show("Неверный логин или пароль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    if (result == MessageBoxResult.OK)
                    {
                        AuthButton.IsEnabled = true;
                    }
                }
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    MessageBoxResult result = MessageBox.Show("Сервер не может найти запрошенный ресурс.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    if (result == MessageBoxResult.OK)
                    {
                        AuthButton.IsEnabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                // обработка других ошибок
                MessageBox.Show($"Произошла ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [JsonObject]
        public class TokenData
        {
            [JsonProperty("auth_token")]
            public string Token { get; set; }
        }
    }
}
