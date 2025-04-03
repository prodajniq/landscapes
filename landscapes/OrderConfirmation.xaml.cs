using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using landscape;
using Npgsql;

namespace landscapes
{
    public partial class OrderConfirmation : Window
    {
        private readonly string phoneNumber;
        private readonly string totalAmount;
        private readonly byte[] houseImageData;
        private readonly byte[] landImageData;

        public OrderConfirmation(string phone, string total, byte[] houseImage = null, byte[] landImage = null)
        {
            InitializeComponent();

            phoneNumber = phone;
            totalAmount = total;
            houseImageData = houseImage;
            landImageData = landImage;

            // Заполняем данные в форме
            PhoneTextBox.Text = phoneNumber;
            TotalTextBox.Text = totalAmount + " миллионов";

            // Загружаем изображения, если они предоставлены
            if (houseImageData != null)
            {
                HouseImage.Source = LoadImageFromBytes(houseImageData);
            }

            if (landImageData != null)
            {
                LandImage.Source = LoadImageFromBytes(landImageData);
            }
        }

        private BitmapImage LoadImageFromBytes(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return null;

            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Сохраняем заказ в базу данных если он еще не был сохранен
                const string connectionString = "Host=localhost;Port=5432;Username=postgres;Password=12345678;Database=landscape";

                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    // Проверяем, существует ли заказ с таким номером телефона
                    var checkCmd = new NpgsqlCommand(
                        "SELECT COUNT(*) FROM Orders WHERE PhoneNumber = @phone",
                        connection);
                    checkCmd.Parameters.AddWithValue("@phone", phoneNumber);

                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                    // Если заказа нет, добавляем его
                    if (count == 0 && houseImageData != null && landImageData != null)
                    {
                        var cmd = new NpgsqlCommand(
                            "INSERT INTO Orders (PhoneNumber, TotalAmount, HouseImage, LandImage) " +
                            "VALUES (@phone, @total, @houseImg, @landImg)",
                            connection);

                        // Преобразуем сумму правильно
                        var totalValue = decimal.Parse(totalAmount);

                        cmd.Parameters.AddWithValue("@phone", phoneNumber);
                        cmd.Parameters.AddWithValue("@total", totalValue);
                        cmd.Parameters.AddWithValue("@houseImg", houseImageData);
                        cmd.Parameters.AddWithValue("@landImg", landImageData);

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Заказ успешно оформлен! Мы свяжемся с вами в ближайшее время.",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                MainWindow mail = new MainWindow();
                mail.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при оформлении заказа: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}