using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;
using System.IO; 

namespace Scheduler
{
    public partial class LoginForm : Form
    {
        private string userLanguage;

        public LoginForm()
        {
            InitializeComponent(); 
            DetectLanguage();
            FetchLocation();
        }

        // Method to detect the user's language
        private void DetectLanguage()
        {
            // Get the current culture's two-letter ISO language code
            userLanguage = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            // Log detected language (for debugging purposes)
            Console.WriteLine($"Detected language: {userLanguage}");
        }

        // Event handler for the Login button click
        private async void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();  // Get the username
            string password = txtPassword.Text.Trim();  // Get the password

            // Validate the login using the database
            bool isValidUser = await ValidateLogin(username, password);

            if (isValidUser)
            {
                MessageBox.Show("Login successful!");
                lblError.Visible = false;
                this.Hide();

                // Log the login attempt
                LogLoginHistory(username);

                // Get the userId after login for further use
                int userId = await GetUserIdByUsername(username);

                // Store the userId in the UserSession class
                UserSession.UserId = userId;

                // Check if the user has any upcoming appointments within the next 15 minutes
                await CheckForUpcomingAppointments(userId);

                // Pass the connection string to the CustomerForm
                string connectionString = "server=localhost;user=sqlUser;database=client_schedule;port=3306;password=Passw0rd!";
                CustomerForm customerForm = new CustomerForm(connectionString);
                customerForm.Show();
            }
            else
            {
                lblError.Visible = true;
                UpdateErrorMessage();
            }
        }

        // Method to log the login history to a file on the desktop
        private void LogLoginHistory(string username)
        {
            // Get the desktop path for the current user
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = Path.Combine(desktopPath, "Login_History.txt");
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            try
            {
                // Append the username and timestamp to the Login_History.txt file
                using (StreamWriter writer = new StreamWriter(filePath, true))
                {
                    writer.WriteLine($"{timestamp} - {username} logged in.");
                }

              
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error logging login history: {ex.Message}");

  
            }
        }


        // Method to validate login credentials using the database
        private async Task<bool> ValidateLogin(string username, string password)
        {
            string connectionString = "server=localhost;user=sqlUser;database=client_schedule;port=3306;password=Passw0rd!";

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string query = "SELECT COUNT(*) FROM user WHERE userName = @username AND password = @password AND active = 1";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@password", password);

                        int userCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                        return userCount > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database error: {ex.Message}");
                return false;
            }
        }

        // Method to update the error message based on the detected language
        private void UpdateErrorMessage()
        {
            if (userLanguage == "es")
            {
                lblError.Text = "El nombre de usuario y la contraseña no coinciden.";
            }
            else
            {
                lblError.Text = "The username and password do not match.";
            }
        }

        // Method to fetch the user's location using IP Geolocation API and update lblLocation
        private async Task FetchLocation()
        {
            try
            {
                string location = await GetLocationAsync();

                if (lblLocation.InvokeRequired)
                {
                    lblLocation.Invoke(new Action(() =>
                    {
                        lblLocation.Text = $"Location: {location}";
                    }));
                }
                else
                {
                    lblLocation.Text = $"Location: {location}";
                }
            }
            catch (Exception ex)
            {
                lblLocation.Text = "Failed to retrieve location.";
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        // Method to fetch location data from an API
        private async Task<string> GetLocationAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                string url = "http://ip-api.com/json";
                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    dynamic locationData = JsonConvert.DeserializeObject(json);

                    string country = locationData.country;
                    string region = locationData.regionName;

                    return $"{region}, {country}";
                }
                else
                {
                    return "Unable to retrieve location";
                }
            }
        }

        // Method to retrieve userId by username
        private async Task<int> GetUserIdByUsername(string username)
        {
            string connectionString = "server=localhost;user=sqlUser;database=client_schedule;port=3306;password=Passw0rd!";
            string query = "SELECT userId FROM user WHERE userName = @username";

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        var result = await cmd.ExecuteScalarAsync();
                        return result != null ? Convert.ToInt32(result) : 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error retrieving user ID: {ex.Message}");
                return 0;
            }
        }

        // Method to check for upcoming appointments within the next 15 minutes
        private async Task CheckForUpcomingAppointments(int userId)
        {
            DateTime currentTime = DateTime.Now;
            DateTime thresholdTime = currentTime.AddMinutes(15);

            string connectionString = "server=localhost;user=sqlUser;database=client_schedule;port=3306;password=Passw0rd!";
            string query = @"
                SELECT appointmentId, title, start
                FROM appointment 
                WHERE userId = @userId 
                AND start BETWEEN @currentTime AND @thresholdTime";

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        cmd.Parameters.AddWithValue("@currentTime", currentTime);
                        cmd.Parameters.AddWithValue("@thresholdTime", thresholdTime);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (reader.HasRows)
                            {
                                string appointments = "You have appointments coming up in the next 15 minutes:\n";
                                while (await reader.ReadAsync())
                                {
                                    string title = reader["title"].ToString();
                                    DateTime start = Convert.ToDateTime(reader["start"]);
                                    appointments += $"{title} - {start:MM/dd/yyyy hh:mm tt}\n";
                                }
                                MessageBox.Show(appointments, "Upcoming Appointments", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                MessageBox.Show("You have no appointments within the next 15 minutes.", "No Upcoming Appointments", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error checking appointments: {ex.Message}");
            }
        }
    }
}
