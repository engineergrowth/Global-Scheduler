using System;
using System.Data;
using System.Linq;  
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Scheduler
{
    public partial class ReportsForm : Form
    {
        private string connectionString = "server=localhost;user=sqlUser;database=client_schedule;port=3306;password=Passw0rd!";

        public ReportsForm()
        {
            InitializeComponent();
        }

        // Method to load the appointment types and counts using lambda expressions
        private void LoadAppointmentTypeReport()
        {
            string query = @"
        SELECT type 
        FROM appointment";

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    var command = new MySqlCommand(query, connection);
                    var adapter = new MySqlDataAdapter(command);
                    var dataTable = new DataTable();

                    connection.Open();
                    adapter.Fill(dataTable);

                    // Convert DataTable to a list of appointment types
                    var appointmentTypes = dataTable.AsEnumerable()
                        .Select(row => row.Field<string>("type"))
                        .ToList();

                    // Use lambda expression to group by type and count appointments
                    var appointmentCounts = appointmentTypes
                        .GroupBy(type => type)
                        .Select(g => new
                        {
                            Type = g.Key,
                            Count = g.Count()
                        }).ToList();

                    // Display result in DataGridView
                    dgvReports.DataSource = appointmentCounts;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading appointment types: " + ex.Message);
            }
        }


        // Method to load the schedule for a specific user using lambda expressions
        private void LoadUserScheduleReport(int userId)
        {
            string query = @"
                SELECT appointmentId, title, description, location, start, end
                FROM appointment
                WHERE userId = @userId
                ORDER BY start";

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@userId", userId);
                    var adapter = new MySqlDataAdapter(command);
                    var dataTable = new DataTable();

                    connection.Open();
                    adapter.Fill(dataTable);

                    // Convert DataTable to a list of appointments and apply lambda expression for data processing
                    var appointments = dataTable.AsEnumerable()
                        .Select(row => new
                        {
                            AppointmentId = row.Field<int>("appointmentId"),
                            Title = row.Field<string>("title"),
                            Description = row.Field<string>("description"),
                            Location = row.Field<string>("location"),
                            Start = row.Field<DateTime>("start"),
                            End = row.Field<DateTime>("end")
                        }).ToList();

                    // Display the filtered and grouped data in the DataGridView
                    dgvReports.DataSource = appointments;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading user schedule: " + ex.Message);
            }
        }

        // Method to load custom report (appointments within a date range)
        private void LoadCustomReport(DateTime startDate, DateTime endDate)
        {
            string query = @"
                SELECT appointmentId, title, description, location, start, end
                FROM appointment
                WHERE start BETWEEN @startDate AND @endDate
                ORDER BY start";

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@startDate", startDate);
                    command.Parameters.AddWithValue("@endDate", endDate);
                    var adapter = new MySqlDataAdapter(command);
                    var dataTable = new DataTable();

                    connection.Open();
                    adapter.Fill(dataTable);

                    // Convert the results to a list and apply a lambda expression for filtering or other operations
                    var appointments = dataTable.AsEnumerable()
                        .Select(row => new
                        {
                            AppointmentId = row.Field<int>("appointmentId"),
                            Title = row.Field<string>("title"),
                            Description = row.Field<string>("description"),
                            Location = row.Field<string>("location"),
                            Start = row.Field<DateTime>("start"),
                            End = row.Field<DateTime>("end")
                        }).ToList();

                    // Display the filtered data in DataGridView
                    dgvReports.DataSource = appointments;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading custom report: " + ex.Message);
            }
        }

        // Button click for generating the Appointment Type Report
        private void btnAppointmentTypeReport_Click(object sender, EventArgs e)
        {
            LoadAppointmentTypeReport();
        }

        // Button click for generating the User Schedule Report
        private void btnUserScheduleReport_Click(object sender, EventArgs e)
        {
            int userId;
            if (int.TryParse(txtUserId.Text, out userId))
            {
                LoadUserScheduleReport(userId);
            }
            else
            {
                MessageBox.Show("Please enter a valid UserId.");
            }
        }

        // Button click for generating the Custom Report (appointments in a date range)
        private void btnCustomReport_Click(object sender, EventArgs e)
        {
            DateTime startDate = dtpStartDate.Value;
            DateTime endDate = dtpEndDate.Value;

            // Ensure the start date is before the end date
            if (startDate <= endDate)
            {
                LoadCustomReport(startDate, endDate);
            }
            else
            {
                MessageBox.Show("Start date must be before the end date.");
            }
        }
    }
}
