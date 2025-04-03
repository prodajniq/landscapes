using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Npgsql;
using landscapes;

namespace landscape
{
    public partial class Registor : Window
    {
        private bool isPasswordVisible = false;
        private string connectionString = "Host=localhost;Port=5432;Username=postgres;Password=12345678;Database=landscape";

        public Registor()
        {
            InitializeComponent();
            PasswordTextBox.Visibility = Visibility.Collapsed;
            PhoneBox.Text = "+7 "; // Начальный текст
            PhoneBox.CaretIndex = PhoneBox.Text.Length; // Устанавливаем каретку в конец
        }

        private void PhoneBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox == null) return;

            string newText = textBox.Text + e.Text;
            if (!Regex.IsMatch(e.Text, @"\d") || newText.Length > 16)
            {
                e.Handled = true; // Запрещаем ввод, если это не цифра или превышен лимит
            }
        }

        private void PhoneBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox == null) return;

            string text = textBox.Text.Replace(" ", "").Replace("+7", ""); // Убираем пробелы и оставляем цифры
            text = "+7 " + FormatPhoneNumber(text);

            textBox.Text = text;
            textBox.CaretIndex = textBox.Text.Length; // Перемещаем курсор в конец
        }

        private string FormatPhoneNumber(string digits)
        {
            if (digits.Length > 10) digits = digits.Substring(0, 10); // Обрезаем до 10 цифр
            string formatted = "";

            if (digits.Length > 0) formatted += digits.Substring(0, Math.Min(3, digits.Length)) + " ";
            if (digits.Length > 3) formatted += digits.Substring(3, Math.Min(3, digits.Length - 3)) + " ";
            if (digits.Length > 6) formatted += digits.Substring(6, Math.Min(4, digits.Length - 6));

            return formatted.Trim();
        }

        private void TogglePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            isPasswordVisible = !isPasswordVisible;

            if (isPasswordVisible)
            {
                PasswordTextBox.Text = PasswordBox.Password;
                PasswordBox.Visibility = Visibility.Collapsed;
                PasswordTextBox.Visibility = Visibility.Visible;
                EyeIcon.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("image/1.png", UriKind.Relative));
            }
            else
            {
                PasswordBox.Password = PasswordTextBox.Text;
                PasswordTextBox.Visibility = Visibility.Collapsed;
                PasswordBox.Visibility = Visibility.Visible;
                EyeIcon.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("image/213094-200.png", UriKind.Relative));
            }
        }

        // Функция хеширования пароля (SHA-256)
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        // Регистрация пользователя
        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginBox.Text.Trim();
            string phone = PhoneBox.Text.Trim();
            string password = PasswordBox.Password.Trim();

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Все поля должны быть заполнены!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string hashedPassword = HashPassword(password);

            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    // Проверяем, существует ли уже такой логин или телефон
                    string checkQuery = "SELECT COUNT(*) FROM users WHERE login = @login OR phone = @phone";
                    using (NpgsqlCommand checkCmd = new NpgsqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("login", login);
                        checkCmd.Parameters.AddWithValue("phone", phone);
                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                        if (count > 0)
                        {
                            MessageBox.Show("Этот логин или номер телефона уже зарегистрирован!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

                    // Добавляем нового пользователя
                    string insertQuery = "INSERT INTO users (login, phone, password) VALUES (@login, @phone, @password)";
                    using (NpgsqlCommand cmd = new NpgsqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("login", login);
                        cmd.Parameters.AddWithValue("phone", phone);
                        cmd.Parameters.AddWithValue("password", hashedPassword);

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Регистрация успешна!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            Zakaz zakazWindow = new Zakaz();
                            zakazWindow.Show();
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("Ошибка регистрации.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка подключения к базе: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void MainMenuButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
    }
}
