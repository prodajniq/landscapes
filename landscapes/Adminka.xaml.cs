using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Xml;

namespace landscape
{
    public partial class Adminka : Window
    {
        private string connectionString = "Host=localhost;Port=5432;Username=postgres;Password=12345678;Database=landscape";

        public Adminka()
        {
            InitializeComponent();
            LoadData();
        }

        public class User
        {
            public string Login { get; set; }
            public string Phone { get; set; }
            public DateTime? LastLogin { get; set; }
            public int FailedAttempts { get; set; }
            public DateTime? BlockedUntil { get; set; }
        }

        public class Order
        {
            public int OrderId { get; set; }
            public string PhoneNumber { get; set; }
            public decimal TotalAmount { get; set; }
            public byte[] HouseImage { get; set; }
            public byte[] LandImage { get; set; }
            public DateTime CreatedDate { get; set; }
            public BitmapImage HousePreview => ConvertToImage(HouseImage);
            public BitmapImage LandPreview => ConvertToImage(LandImage);

            private BitmapImage ConvertToImage(byte[] data)
            {
                if (data == null) return null;
                var image = new BitmapImage();
                using (var stream = new System.IO.MemoryStream(data))
                {
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.EndInit();
                }
                return image;
            }
        }

        private void LoadData()
        {
            try
            {
                LoadUsers();
                LoadOrders();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void LoadUsers()
        {
            var users = new List<User>();

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string query = @"SELECT login, phone, last_login, 
                                failed_attempts, blocked_until FROM users";

                using (var cmd = new NpgsqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new User
                        {
                            Login = reader.GetString(0),
                            Phone = reader.GetString(1),
                            LastLogin = reader.IsDBNull(2) ? null : (DateTime?)reader.GetDateTime(2),
                            FailedAttempts = reader.GetInt32(3),
                            BlockedUntil = reader.IsDBNull(4) ? null : (DateTime?)reader.GetDateTime(4)
                        });
                    }
                }
            }

            UsersDataGrid.ItemsSource = users;
        }

        private void LoadOrders()
        {
            var orders = new List<Order>();

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string query = @"SELECT orderid, phonenumber, totalamount, 
                                houseimage, landimage, createddate FROM orders";

                using (var cmd = new NpgsqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        orders.Add(new Order
                        {
                            OrderId = reader.GetInt32(0),
                            PhoneNumber = reader.GetString(1),
                            TotalAmount = reader.GetDecimal(2),
                            HouseImage = reader.IsDBNull(3) ? null : (byte[])reader[3],
                            LandImage = reader.IsDBNull(4) ? null : (byte[])reader[4],
                            CreatedDate = reader.GetDateTime(5)
                        });
                    }
                }
            }

            OrdersDataGrid.ItemsSource = orders;
        }

        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            User selectedUser = UsersDataGrid.SelectedItem as User;
            if (selectedUser == null)
            {
                MessageBox.Show("Выберите пользователя для удаления");
                return;
            }

            MessageBoxResult result = MessageBox.Show($"Вы действительно хотите удалить пользователя {selectedUser.Login}?",
                "Подтверждение удаления", MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var conn = new NpgsqlConnection(connectionString))
                    {
                        conn.Open();
                        string query = "DELETE FROM users WHERE login = @login";

                        using (var cmd = new NpgsqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@login", selectedUser.Login);
                            int affected = cmd.ExecuteNonQuery();

                            if (affected > 0)
                            {
                                MessageBox.Show("Пользователь успешно удален");
                                LoadUsers(); // Перезагрузить список пользователей
                            }
                            else
                            {
                                MessageBox.Show("Ошибка при удалении пользователя");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}");
                }
            }
        }

        private void UnblockUser_Click(object sender, RoutedEventArgs e)
        {
            User selectedUser = UsersDataGrid.SelectedItem as User;
            if (selectedUser == null)
            {
                MessageBox.Show("Выберите пользователя для разблокировки");
                return;
            }

            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "UPDATE users SET failed_attempts = 0, blocked_until = NULL WHERE login = @login";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@login", selectedUser.Login);
                        int affected = cmd.ExecuteNonQuery();

                        if (affected > 0)
                        {
                            MessageBox.Show("Пользователь успешно разблокирован");
                            LoadUsers(); // Перезагрузить список пользователей
                        }
                        else
                        {
                            MessageBox.Show("Ошибка при разблокировке пользователя");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при разблокировке: {ex.Message}");
            }
        }

        private void DeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            Order selectedOrder = OrdersDataGrid.SelectedItem as Order;
            if (selectedOrder == null)
            {
                MessageBox.Show("Выберите заказ для удаления");
                return;
            }

            MessageBoxResult result = MessageBox.Show($"Вы действительно хотите удалить заказ с ID {selectedOrder.OrderId}?",
                "Подтверждение удаления", MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var conn = new NpgsqlConnection(connectionString))
                    {
                        conn.Open();
                        string query = "DELETE FROM orders WHERE orderid = @orderid";

                        using (var cmd = new NpgsqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@orderid", selectedOrder.OrderId);
                            int affected = cmd.ExecuteNonQuery();

                            if (affected > 0)
                            {
                                MessageBox.Show("Заказ успешно удален");
                                LoadOrders(); // Перезагрузить список заказов
                            }
                            else
                            {
                                MessageBox.Show("Ошибка при удалении заказа");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}");
                }
            }
        }

        private void ExportUserToDesktop_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            User user = (button.DataContext as User);

            if (user != null)
            {
                try
                {
                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string filePath = Path.Combine(desktopPath, $"User_{user.Login}_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.html");

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("<!DOCTYPE html>");
                    sb.AppendLine("<html>");
                    sb.AppendLine("<head>");
                    sb.AppendLine("<meta charset=\"UTF-8\">");
                    sb.AppendLine("<title>Данные пользователя</title>");
                    sb.AppendLine("<style>");
                    sb.AppendLine("body { font-family: Arial, sans-serif; margin: 40px; }");
                    sb.AppendLine("h1 { color: #4E7D47; }");
                    sb.AppendLine("table { border-collapse: collapse; width: 100%; }");
                    sb.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
                    sb.AppendLine("th { background-color: #A1C181; color: white; }");
                    sb.AppendLine("</style>");
                    sb.AppendLine("</head>");
                    sb.AppendLine("<body>");
                    sb.AppendLine("<h1>Данные пользователя Tranquil Landscapes</h1>");
                    sb.AppendLine("<table>");
                    sb.AppendLine("<tr><th>Параметр</th><th>Значение</th></tr>");
                    sb.AppendLine($"<tr><td>Логин</td><td>{user.Login}</td></tr>");
                    sb.AppendLine($"<tr><td>Телефон</td><td>{user.Phone}</td></tr>");
                    sb.AppendLine($"<tr><td>Последний вход</td><td>{(user.LastLogin.HasValue ? user.LastLogin.Value.ToString() : "Не входил")}</td></tr>");
                    sb.AppendLine($"<tr><td>Неудачные попытки</td><td>{user.FailedAttempts}</td></tr>");
                    sb.AppendLine($"<tr><td>Блокировка до</td><td>{(user.BlockedUntil.HasValue ? user.BlockedUntil.Value.ToString() : "Не заблокирован")}</td></tr>");
                    sb.AppendLine("</table>");
                    sb.AppendLine("</body>");
                    sb.AppendLine("</html>");

                    File.WriteAllText(filePath, sb.ToString());
                    MessageBox.Show($"Данные пользователя экспортированы в файл: {filePath}");

                    // Открываем файл в браузере по умолчанию
                    System.Diagnostics.Process.Start(filePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка экспорта: {ex.Message}");
                }
            }
        }

        private void ExportOrderToDesktop_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Order order = (button.DataContext as Order);

            if (order != null)
            {
                try
                {
                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string filePath = Path.Combine(desktopPath, $"Order_{order.OrderId}_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.html");

                    // Конвертируем изображения в Base64 для встраивания в HTML
                    string houseImageBase64 = order.HouseImage != null
                        ? Convert.ToBase64String(order.HouseImage) : "";

                    string landImageBase64 = order.LandImage != null
                        ? Convert.ToBase64String(order.LandImage) : "";

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("<!DOCTYPE html>");
                    sb.AppendLine("<html>");
                    sb.AppendLine("<head>");
                    sb.AppendLine("<meta charset=\"UTF-8\">");
                    sb.AppendLine("<title>Данные заказа</title>");
                    sb.AppendLine("<style>");
                    sb.AppendLine("body { font-family: Arial, sans-serif; margin: 40px; }");
                    sb.AppendLine("h1, h2 { color: #4E7D47; }");
                    sb.AppendLine("table { border-collapse: collapse; width: 100%; margin-bottom: 30px; }");
                    sb.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
                    sb.AppendLine("th { background-color: #A1C181; color: white; }");
                    sb.AppendLine(".images { display: flex; gap: 20px; margin-top: 20px; }");
                    sb.AppendLine(".image-container { width: 45%; }");
                    sb.AppendLine(".image-container img { max-width: 100%; border: 1px solid #ddd; }");
                    sb.AppendLine("</style>");
                    sb.AppendLine("</head>");
                    sb.AppendLine("<body>");
                    sb.AppendLine("<h1>Данные заказа Tranquil Landscapes</h1>");
                    sb.AppendLine("<table>");
                    sb.AppendLine("<tr><th>Параметр</th><th>Значение</th></tr>");
                    sb.AppendLine($"<tr><td>ID заказа</td><td>{order.OrderId}</td></tr>");
                    sb.AppendLine($"<tr><td>Телефон</td><td>{order.PhoneNumber}</td></tr>");
                    sb.AppendLine($"<tr><td>Сумма</td><td>{order.TotalAmount.ToString("0.00")} миллионов.</td></tr>");
                    sb.AppendLine($"<tr><td>Дата создания</td><td>{order.CreatedDate}</td></tr>");
                    sb.AppendLine("</table>");

                    sb.AppendLine("<h2>Изображения заказа</h2>");
                    sb.AppendLine("<div class=\"images\">");

                    if (!string.IsNullOrEmpty(houseImageBase64))
                    {
                        sb.AppendLine("<div class=\"image-container\">");
                        sb.AppendLine("<h3>Дом</h3>");
                        sb.AppendLine($"<img src=\"data:image/jpeg;base64,{houseImageBase64}\" alt=\"Изображение дома\">");
                        sb.AppendLine("</div>");
                    }

                    if (!string.IsNullOrEmpty(landImageBase64))
                    {
                        sb.AppendLine("<div class=\"image-container\">");
                        sb.AppendLine("<h3>Участок</h3>");
                        sb.AppendLine($"<img src=\"data:image/jpeg;base64,{landImageBase64}\" alt=\"Изображение участка\">");
                        sb.AppendLine("</div>");
                    }

                    sb.AppendLine("</div>");
                    sb.AppendLine("</body>");
                    sb.AppendLine("</html>");

                    File.WriteAllText(filePath, sb.ToString());
                    MessageBox.Show($"Данные заказа экспортированы в файл: {filePath}");

                    // Открываем файл в браузере по умолчанию
                    System.Diagnostics.Process.Start(filePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка экспорта: {ex.Message}");
                }
            }
        }

        private void Botton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
    }
}