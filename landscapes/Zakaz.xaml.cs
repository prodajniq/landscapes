using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Npgsql;

namespace landscapes
{
    public partial class Zakaz : Window
    {
        private double housePrice;
        private double landPrice;
        private readonly Regex phoneNumberRegex = new Regex("[^0-9]");

        public Zakaz()
        {
            InitializeComponent();
            TotalTextBox.Text = "0.0 миллионов";
            PhoneTextBox.Text = "+7 ";
        }

        private void LoadImage(Image imageControl, string path)
        {
            try
            {
                var uri = new Uri(path, UriKind.Relative);
                var bitmap = new BitmapImage(uri);
                imageControl.Source = bitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}",
                              "Ошибка",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        private void UpdateTotal()
        {
            double total = housePrice + landPrice;
            TotalTextBox.Text = $"{total:0.0} миллионов";
        }

        private void HouseButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            switch (button.Content.ToString())
            {
                case "40 м2(1 этаж)":
                    housePrice = 4.5;
                    LoadImage(HouseImage, "image/Д1.jpg");
                    break;
                case "60 м2(1 этаж)":
                    housePrice = 5.8;
                    LoadImage(HouseImage, "image/Д2.jpg");
                    break;
                case "100 м2(2 этажа)":
                    housePrice = 7.4;
                    LoadImage(HouseImage, "image/Д3.jpg");
                    break;
                case "120 м2(2 этажа)":
                    housePrice = 10.0;
                    LoadImage(HouseImage, "image/Д4.jpg");
                    break;
            }
            UpdateTotal();
        }

        private void LandButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            switch (button.Content.ToString())
            {
                case "5 соток":
                    landPrice = 1.0;
                    LoadImage(LandImage, "image/Зем1.jpg");
                    break;
                case "9 соток":
                    landPrice = 2.0;
                    LoadImage(LandImage, "image/Зем2.jpg");
                    break;
                case "15 соток":
                    landPrice = 3.0;
                    LoadImage(LandImage, "image/Зем3.jpg");
                    break;
                case "20 соток":
                    landPrice = 4.0;
                    LoadImage(LandImage, "image/Зем4.jpg");
                    break;
            }
            UpdateTotal();
        }

        private void PhoneTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только цифры
            e.Handled = phoneNumberRegex.IsMatch(e.Text);
        }

        private void PhoneTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = (TextBox)sender;
            var text = textBox.Text;
            var caretIndex = textBox.CaretIndex;

            // Удаляем все нецифры, кроме "+"
            var digitsOnly = Regex.Replace(text, @"[^\d+]", "");

            // Если пользователь случайно удалил "+7 ", добавим его снова
            if (!digitsOnly.StartsWith("+7"))
            {
                if (digitsOnly.StartsWith("+"))
                    digitsOnly = "+7" + digitsOnly.Substring(1);
                else
                    digitsOnly = "+7" + digitsOnly;
            }

            // Форматируем номер телефона
            string formattedNumber = FormatPhoneNumber(digitsOnly);

            // Если номер изменился, обновляем текст
            if (formattedNumber != text)
            {
                textBox.Text = formattedNumber;
                // Устанавливаем курсор в нужную позицию
                textBox.CaretIndex = Math.Min(formattedNumber.Length, caretIndex + (formattedNumber.Length - text.Length));
            }
        }

        private string FormatPhoneNumber(string phoneNumber)
        {
            // Оставляем "+7 " и форматируем остальные цифры
            string result = "+7 ";

            // Удаляем "+7" из начала, если есть
            if (phoneNumber.StartsWith("+7"))
                phoneNumber = phoneNumber.Substring(2);

            // Форматируем номер телефона: XXX XXX XXXX
            int length = phoneNumber.Length;

            if (length > 0)
            {
                result += phoneNumber.Substring(0, Math.Min(3, length));

                if (length > 3)
                {
                    result += " " + phoneNumber.Substring(3, Math.Min(3, length - 3));

                    if (length > 6)
                    {
                        result += " " + phoneNumber.Substring(6, Math.Min(4, length - 6));
                    }
                }
            }

            return result;
        }

        private bool IsPhoneNumberValid()
        {
            // Проверяем что номер телефона введен полностью
            // Формат: +7 XXX XXX XXXX (всего 15 символов с учетом пробелов)
            if (string.IsNullOrWhiteSpace(PhoneTextBox.Text) || PhoneTextBox.Text.Length < 15)
            {
                MessageBox.Show("Пожалуйста, введите полный номер телефона в формате +7 XXX XXX XXXX",
                              "Ошибка ввода",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
                PhoneTextBox.Focus();
                return false;
            }
            return true;
        }

        private bool AreSelectionsValid()
        {
            if (HouseImage.Source == null || LandImage.Source == null)
            {
                MessageBox.Show("Пожалуйста, выберите дом и участок!",
                              "Ошибка ввода",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем валидность телефона и выбора опций
            if (!IsPhoneNumberValid() || !AreSelectionsValid())
            {
                return;
            }

            // Открываем форму подтверждения заказа
            var totalValue = TotalTextBox.Text.Split(' ')[0];
            var confirmation = new OrderConfirmation(PhoneTextBox.Text, totalValue,
                                                   GetImageBytes(HouseImage), GetImageBytes(LandImage));

            // Отображаем окно подтверждения заказа на переднем плане
            confirmation.Topmost = true;
            confirmation.Show();
        }

        private byte[] GetImageBytes(Image image)
        {
            if (image.Source is BitmapImage bitmap)
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    encoder.Save(stream);
                    return stream.ToArray();
                }
            }
            return null;
        }
    }
}