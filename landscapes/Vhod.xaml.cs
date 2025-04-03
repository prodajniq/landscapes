// Vhod.xaml.cs
using System;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using landscapes;
using Npgsql;

namespace landscape
{
    public partial class Vhod : Window
    {
        private bool isPasswordVisible = false;
        private string connectionString = "Host=localhost;Port=5432;Username=postgres;Password=12345678;Database=landscape";

        public Vhod()
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
                EyeIcon.Source = new BitmapImage(new Uri("icons/eye_open.png", UriKind.Relative));
            }
            else
            {
                PasswordBox.Password = PasswordTextBox.Text;
                PasswordTextBox.Visibility = Visibility.Collapsed;
                PasswordBox.Visibility = Visibility.Visible;
                EyeIcon.Source = new BitmapImage(new Uri("icons/eye_closed.png", UriKind.Relative));
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginBox.Text.Trim();
            string password = isPasswordVisible ? PasswordTextBox.Text : PasswordBox.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Заполните все поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var conn = new NpgsqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    using (var cmd = new NpgsqlCommand(
                        "SELECT password, failed_attempts, last_login, blocked_until FROM users WHERE login = @login", conn))
                    {
                        cmd.Parameters.AddWithValue("@login", login);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                MessageBox.Show("Неверные данные", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            string storedPassword = reader.GetString(0);
                            int failedAttempts = reader.GetInt32(1);
                            DateTime? blockedUntil = reader.IsDBNull(3) ? null : reader.GetDateTime(3);

                            reader.Close();

                            if (blockedUntil.HasValue && blockedUntil > DateTime.UtcNow)
                            {
                                MessageBox.Show($"Аккаунт заблокирован до {blockedUntil.Value.ToLocalTime():HH:mm}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            if (ComputeSha256Hash(password) != storedPassword)
                            {
                                failedAttempts++;

                                if (failedAttempts >= 3)
                                {
                                    using (var blockCmd = new NpgsqlCommand(
                                        "UPDATE users SET blocked_until = @blockTime WHERE login = @login", conn))
                                    {
                                        blockCmd.Parameters.AddWithValue("@blockTime", DateTime.UtcNow.AddMinutes(1));
                                        blockCmd.Parameters.AddWithValue("@login", login);
                                        blockCmd.ExecuteNonQuery();
                                    }
                                    MessageBox.Show("Аккаунт заблокирован на 1 минуту", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }

                                using (var updateCmd = new NpgsqlCommand(
                                    "UPDATE users SET failed_attempts = @attempts WHERE login = @login", conn))
                                {
                                    updateCmd.Parameters.AddWithValue("@attempts", failedAttempts);
                                    updateCmd.Parameters.AddWithValue("@login", login);
                                    updateCmd.ExecuteNonQuery();
                                }

                                MessageBox.Show($"Осталось попыток: {3 - failedAttempts}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }

                            // Успешный вход
                            using (var successCmd = new NpgsqlCommand(
                                "UPDATE users SET failed_attempts = 0, last_login = NOW() WHERE login = @login", conn))
                            {
                                successCmd.Parameters.AddWithValue("@login", login);
                                successCmd.ExecuteNonQuery();
                            }

                            Zakaz zakazWindow = new Zakaz(); 
                            zakazWindow.Show();
                            this.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка подключения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }

        private void ForgotPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            new ResetPasswordWindow().ShowDialog();
        }

        private void MainMenuButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
    }
}