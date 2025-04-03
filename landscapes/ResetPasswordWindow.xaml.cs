using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Text;
using System.Text.RegularExpressions;
using Npgsql;
using System.Windows.Input;
using System.Security.Cryptography;

namespace landscape
{
    public partial class ResetPasswordWindow : Window
    {
        private string connectionString = "Host=localhost;Port=5432;Username=postgres;Password=12345678;Database=landscape";
        private bool isFormatting = false; // Флаг для предотвращения рекурсии

        public ResetPasswordWindow()
        {
            InitializeComponent();
            PhoneTextBox.Text = "+7 ";
            PhoneTextBox.CaretIndex = 3; // Установка курсора после префикса
        }

        // Блокировка удаления префикса "+7 "
        private void PhoneTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            // Блокируем Backspace и Delete в префиксе
            if (e.Key == Key.Back || e.Key == Key.Delete)
            {
                int caretIndex = textBox.CaretIndex;
                // Запрещаем удаление, если курсор в зоне префикса
                if (caretIndex <= 3)
                {
                    e.Handled = true;
                }
            }
        }

        private void PhoneTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            bool isDigit = e.Text.All(char.IsDigit);
            e.Handled = !isDigit; // Разрешаем только цифры
        }

        private void PhoneTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isFormatting) return;
            isFormatting = true;

            var textBox = sender as TextBox;
            string currentText = textBox.Text;

            // Восстановление префикса, если он удален
            if (!currentText.StartsWith("+7 ") || currentText.Length < 3)
            {
                textBox.Text = "+7 ";
                textBox.CaretIndex = 3;
                isFormatting = false;
                return;
            }

            // Форматирование номера
            string cleanNumber = new string(currentText.Where(char.IsDigit).ToArray()).Substring(1); // Убираем первую 7
            if (cleanNumber.Length > 10)
                cleanNumber = cleanNumber.Substring(0, 10);

            StringBuilder formattedNumber = new StringBuilder("+7 ");
            for (int i = 0; i < cleanNumber.Length; i++)
            {
                if (i == 3 || i == 6)
                    formattedNumber.Append(" ");
                formattedNumber.Append(cleanNumber[i]);
            }

            string newText = formattedNumber.ToString().Trim();
            if (textBox.Text != newText)
            {
                textBox.Text = newText;
                textBox.CaretIndex = newText.Length;
            }

            isFormatting = false;
        }

        private void ResetPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            string phone = PhoneTextBox.Text.Trim();
            string newPassword = NewPasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;

            // Проверка, введён ли номер телефона полностью
            string cleanNumber = new string(phone.Where(char.IsDigit).ToArray());

            if (cleanNumber.Length != 11)
            {
                MessageBox.Show("Введите полный номер телефона (11 цифр).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Проверка формата номера
            if (!Regex.IsMatch(phone, @"^\+7 \d{3} \d{3} \d{4}$"))
            {
                MessageBox.Show("Неверный формат телефона.\nПример: +7 912 345 6789", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Проверка совпадения паролей
            if (newPassword != confirmPassword)
            {
                MessageBox.Show("Пароли не совпадают.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Остальная логика сброса пароля
            using (var conn = new NpgsqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    // Проверка существования пользователя
                    using (var cmd = new NpgsqlCommand("SELECT 1 FROM users WHERE phone = @phone", conn))
                    {
                        cmd.Parameters.AddWithValue("@phone", phone);
                        if (cmd.ExecuteScalar() == null)
                        {
                            MessageBox.Show("Телефон не найден в системе.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    // Обновление пароля
                    using (var cmd = new NpgsqlCommand("UPDATE users SET password = @password WHERE phone = @phone", conn))
                    {
                        cmd.Parameters.AddWithValue("@password", ComputeSha256Hash(newPassword));
                        cmd.Parameters.AddWithValue("@phone", phone);
                        cmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Пароль успешно обновлён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка базы данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Метод для хеширования пароля с использованием SHA256
        private string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                return string.Concat(bytes.Select(b => b.ToString("x2")));
            }
        }
    }
}
