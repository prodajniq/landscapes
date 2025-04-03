using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Npgsql; // Библиотека для работы с PostgreSQL

namespace landscape
{
    public partial class vhodadminka : Window
    {
        private bool isPasswordVisible = false;
        private string connectionString = "Host=localhost;Port=5432;Username=postgres;Password=12345678;Database=landscape";

        public vhodadminka()
        {
            InitializeComponent();
            PasswordTextBox.Visibility = Visibility.Collapsed;
        }

        private void TogglePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            isPasswordVisible = !isPasswordVisible;

            if (isPasswordVisible)
            {
                PasswordTextBox.Text = PasswordBox.Password;
                PasswordBox.Visibility = Visibility.Collapsed;
                PasswordTextBox.Visibility = Visibility.Visible;
                EyeIcon.Source = new BitmapImage(new Uri("image/1.png", UriKind.Relative));
            }
            else
            {
                PasswordBox.Password = PasswordTextBox.Text;
                PasswordTextBox.Visibility = Visibility.Collapsed;
                PasswordBox.Visibility = Visibility.Visible;
                EyeIcon.Source = new BitmapImage(new Uri("image/213094-200.png", UriKind.Relative));
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginBox.Text;
            string password = PasswordBox.Password;

            if (AuthenticateUser(login, password))
            {
                MessageBox.Show("Авторизация успешна!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                Adminka adminka = new Adminka(); // Открываем новую страницу
                adminka.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Неверный логин или пароль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool AuthenticateUser(string login, string password)
        {
            bool isAuthenticated = false;
            try
            {
                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT COUNT(*) FROM adminka WHERE login = @login AND password = @password";

                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@login", login);
                        cmd.Parameters.AddWithValue("@password", password);

                        int userCount = Convert.ToInt32(cmd.ExecuteScalar());
                        isAuthenticated = userCount > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка подключения к базе данных: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return isAuthenticated;
        }

        private void MainMenuButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
    }
}
