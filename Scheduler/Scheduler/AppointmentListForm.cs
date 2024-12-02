using System;
using System.CodeDom;
using System.Data;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Scheduler
{
    public partial class AppointmentListForm : Form
    {

        public int CustomerId { get; set; }

        // Connection string for the MySQL database
        private string connectionString = "server=localhost;user=sqlUser;database=client_schedule;port=3306;password=Passw0rd!";

        public AppointmentListForm(int customerId)
        {
            InitializeComponent();
            this.Load += AppointmentListForm_Load;
            CustomerId = customerId; 
            dtpStart.Format = DateTimePickerFormat.Custom;
            dtpStart.CustomFormat = "MM/dd/yyyy hh:mm tt";  

            dtpEnd.Format = DateTimePickerFormat.Custom;
            dtpEnd.CustomFormat = "MM/dd/yyyy hh:mm tt";  
        }

        // Method to load appointments for the passed customerId
        private void LoadAppointments()
        {
            string query = "SELECT appointmentId, title, description, location, contact, type, url, start, end FROM appointment WHERE customerId = @customerId";


            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@customerId", CustomerId);

                    var adapter = new MySqlDataAdapter(command);
                    var dataTable = new DataTable();

                    connection.Open();
                    adapter.Fill(dataTable);

                    // Check if data is loaded into the DataTable
                    if (dataTable.Rows.Count > 0)
                    {
                        dgvAppointments.DataSource = dataTable;
                    }
                    else
                    {
                        MessageBox.Show("No appointments found for this customer.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading appointments: " + ex.Message);
            }
        }


        // Load appointments when the form loads
        private void AppointmentListForm_Load(object sender, EventArgs e)
        {
            
            dgvAppointments.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            LoadAppointments();
            dgvAppointments.CellClick += DgvAppointments_CellClick;

        }

        private void DgvAppointments_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow selectedRow = dgvAppointments.Rows[e.RowIndex];

                // Populate the text boxes with data from the selected row
                txtTitle.Text = selectedRow.Cells["title"].Value?.ToString() ?? "";
                txtDescription.Text = selectedRow.Cells["description"].Value?.ToString() ?? "";
                txtLocation.Text = selectedRow.Cells["location"].Value?.ToString() ?? "";
                txtContact.Text = selectedRow.Cells["contact"].Value?.ToString() ?? "";
                txtType.Text = selectedRow.Cells["type"].Value?.ToString() ?? "";
                txtUrl.Text = selectedRow.Cells["url"].Value?.ToString() ?? "";

                // Convert start and end times from UTC to local time
                if (DateTime.TryParse(selectedRow.Cells["start"].Value?.ToString(), out DateTime startUtc))
                {
                    dtpStart.Value = ConvertToLocalTime(startUtc);  // Convert UTC to local time
                }
                if (DateTime.TryParse(selectedRow.Cells["end"].Value?.ToString(), out DateTime endUtc))
                {
                    dtpEnd.Value = ConvertToLocalTime(endUtc);  // Convert UTC to local time
                }
            }
        }


        // Method to add a new appointment to the database
        private void AddAppointment()
        {
            int userId = UserSession.UserId;

            // Collect data from the form
            string title = txtTitle.Text;
            string description = txtDescription.Text;
            string location = txtLocation.Text;
            string contact = txtContact.Text;
            string type = txtType.Text;
            DateTime start = ConvertToUtc(dtpStart.Value);  
            DateTime end = ConvertToUtc(dtpEnd.Value);      
            string url = txtUrl.Text;

            // Validate required fields
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("Title is required.");
                return;
            }
            if (string.IsNullOrWhiteSpace(description))
            {
                MessageBox.Show("Description is required.");
                return;
            }
            if (string.IsNullOrWhiteSpace(location))
            {
                MessageBox.Show("Location is required.");
                return;
            }
            if (string.IsNullOrWhiteSpace(contact))
            {
                MessageBox.Show("Contact is required.");
                return;
            }
            if (string.IsNullOrWhiteSpace(type))
            {
                MessageBox.Show("Type is required.");
                return;
            }
            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show("URL is required.");
                return;
            }

            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show("URL is required.");
                return;
            }

            if (start >= end)
            {
                MessageBox.Show("Start time must be before end time.");
                return;
            }

            // Validate that the appointment does not overlap with existing appointments for the same customer
            if (HasOverlappingAppointment(start, end))
            {
                MessageBox.Show("The appointment time overlaps with an existing appointment. Please choose a different time.");
                return;
            }

            // Check if appointment is within business hours 
            if (!IsWithinBusinessHours(start, end))
            {
                MessageBox.Show("Appointments must be scheduled between 9:00 AM and 5:00 PM, Monday to Friday (Eastern Standard Time).");
                return;
            }

            // Insert the appointment into the database
            string query = @"
    INSERT INTO appointment (customerId, title, description, location, contact, type, url, start, end, createDate, createdBy, lastUpdate, lastUpdateBy, userId)
    VALUES (@customerId, @title, @description, @location, @contact, @type, @url, @start, @end, NOW(), @createdBy, NOW(), @lastUpdateBy, @userId)";

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@customerId", CustomerId);
                    command.Parameters.AddWithValue("@title", title);
                    command.Parameters.AddWithValue("@description", description);
                    command.Parameters.AddWithValue("@location", location);
                    command.Parameters.AddWithValue("@contact", contact);
                    command.Parameters.AddWithValue("@type", type);
                    command.Parameters.AddWithValue("@url", url);
                    command.Parameters.AddWithValue("@start", start);
                    command.Parameters.AddWithValue("@end", end);
                    command.Parameters.AddWithValue("@createdBy", userId);
                    command.Parameters.AddWithValue("@lastUpdateBy", userId);
                    command.Parameters.AddWithValue("@userId", userId);

                    connection.Open();
                    command.ExecuteNonQuery();

                    MessageBox.Show("Appointment added successfully.");

                 
                    LoadAppointments();
                    ClearFormFields();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error adding appointment: " + ex.Message);
            }
        }

        private bool HasOverlappingAppointment(DateTime start, DateTime end)
        {
            string query = @"
              SELECT COUNT(*) FROM appointment
              WHERE customerId = @customerId
              AND ((@start BETWEEN start AND end) OR (@end BETWEEN start AND end) OR (start BETWEEN @start AND @end))
               ";

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@customerId", CustomerId);
                    command.Parameters.AddWithValue("@start", start);
                    command.Parameters.AddWithValue("@end", end);

                    connection.Open();
                    int count = Convert.ToInt32(command.ExecuteScalar());

                    // If count is greater than 0, there is an overlapping appointment
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error checking for overlapping appointments: " + ex.Message);
                return true; // Return true to prevent adding the appointment if there is an error
            }
        }

        private void btnAddAppointment_Click(object sender, EventArgs e)
        {
     
            AddAppointment();
            ClearFormFields();
        }

        private void ClearFormFields()
        {
            txtTitle.Clear();
            txtDescription.Clear();
            txtLocation.Clear();
            txtContact.Clear();
            txtType.Clear();
            txtUrl.Clear();
            dtpStart.Value = DateTime.Now;
            dtpEnd.Value = DateTime.Now.AddHours(1);
        }

        private void btnUpdateAppointment_Click(object sender, EventArgs e)
        {
        
            if (dgvAppointments.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select an appointment to update.");
                return;
            }

          
            if (string.IsNullOrWhiteSpace(txtTitle.Text) ||
                string.IsNullOrWhiteSpace(txtDescription.Text) ||
                string.IsNullOrWhiteSpace(txtLocation.Text) ||
                string.IsNullOrWhiteSpace(txtContact.Text) ||
                string.IsNullOrWhiteSpace(txtType.Text) ||
                string.IsNullOrWhiteSpace(txtUrl.Text))
            {
                MessageBox.Show("All fields must be filled out.");
                return;
            }

            DateTime start = ConvertToUtc(dtpStart.Value); 
            DateTime end = ConvertToUtc(dtpEnd.Value);    

            // Validate that start time is before end time
            if (start >= end)
            {
                MessageBox.Show("Start time must be before end time.");
                return;
            }

            // Check if the updated time is within business hours
            if (!IsWithinBusinessHours(start, end))
            {
                MessageBox.Show("Appointments must be scheduled between 9:00 a.m. and 5:00 p.m., Monday to Friday (Eastern Standard Time).");
                return;
            }

            // Ensure the updated appointment does not overlap with any existing appointment
            if (HasOverlappingAppointment(start, end))
            {
                MessageBox.Show("The appointment time overlaps with an existing appointment. Please choose a different time.");
                return;
            }

            try
            {
                // Get the appointmentId from the selected row
                int appointmentId = Convert.ToInt32(dgvAppointments.SelectedRows[0].Cells["appointmentId"].Value);

                string title = txtTitle.Text;
                string description = txtDescription.Text;
                string location = txtLocation.Text;
                string contact = txtContact.Text;
                string type = txtType.Text;
                string url = txtUrl.Text;

                // SQL Update query
                string query = @"
                UPDATE appointment
                SET 
                    title = @title,
                    description = @description,
                    location = @location,
                    contact = @contact,
                    type = @type,
                    url = @url,
                    start = @start,
                    end = @end,
                    lastUpdate = NOW(),
                    lastUpdateBy = @lastUpdateBy,
                    userId = @userId  -- Include the userId in the update
                    WHERE appointmentId = @appointmentId";

                using (var connection = new MySqlConnection(connectionString))
                {
                    var command = new MySqlCommand(query, connection);

                    // Add parameters
                    command.Parameters.AddWithValue("@title", title);
                    command.Parameters.AddWithValue("@description", description);
                    command.Parameters.AddWithValue("@location", location);
                    command.Parameters.AddWithValue("@contact", contact);
                    command.Parameters.AddWithValue("@type", type);
                    command.Parameters.AddWithValue("@url", url);
                    command.Parameters.AddWithValue("@start", start);
                    command.Parameters.AddWithValue("@end", end);
                    command.Parameters.AddWithValue("@lastUpdateBy", UserSession.UserId);
                    command.Parameters.AddWithValue("@userId", UserSession.UserId);
                    command.Parameters.AddWithValue("@appointmentId", appointmentId);

                    // Execute the update
                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();

                    // Provide feedback
                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Appointment updated successfully.");
                        LoadAppointments(); 
                        ClearFormFields(); 
                    }
                    else
                    {
                        MessageBox.Show("No changes were made.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating appointment: " + ex.Message);
            }
        }




        private void btnDeleteAppointment_Click(object sender, EventArgs e)
        {
            // Ensure a row is selected in the DataGridView
            if (dgvAppointments.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select an appointment to delete.");
                return;
            }

            // Confirm deletion with the user
            var confirmation = MessageBox.Show("Are you sure you want to delete this appointment?", "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirmation != DialogResult.Yes)
            {
                return;
            }

            try
            {
                // Get the appointmentId from the selected row
                int appointmentId = Convert.ToInt32(dgvAppointments.SelectedRows[0].Cells["appointmentId"].Value);

                // SQL Delete query
                string query = "DELETE FROM appointment WHERE appointmentId = @appointmentId";

                using (var connection = new MySqlConnection(connectionString))
                {
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@appointmentId", appointmentId);

                    // Execute the delete operation
                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();

                    // Provide feedback to the user
                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Appointment deleted successfully.");
                        LoadAppointments(); // Refresh the DataGridView
                        ClearFormFields();  // Clear the form fields
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete the appointment. It may not exist.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting appointment: " + ex.Message);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            ClearFormFields();
        }

        private bool IsWithinBusinessHours(DateTime start, DateTime end)
        {
            // Define business hours: 9 AM to 5 PM (Eastern Time)
            TimeSpan businessStart = new TimeSpan(9, 0, 0); // 9:00 AM
            TimeSpan businessEnd = new TimeSpan(17, 0, 0);  // 5:00 PM

            try
            {
                // Get the Eastern Time zone, accounting for Daylight Saving Time
                TimeZoneInfo easternTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

                // Convert the start and end times to Eastern Time
                DateTime startEastern = TimeZoneInfo.ConvertTimeFromUtc(start, easternTimeZone);
                DateTime endEastern = TimeZoneInfo.ConvertTimeFromUtc(end, easternTimeZone);

              
                // Check if the start and end times fall within business hours
                bool isWithinHours = startEastern.TimeOfDay >= businessStart && endEastern.TimeOfDay <= businessEnd;
                bool isWeekday = startEastern.DayOfWeek >= DayOfWeek.Monday && startEastern.DayOfWeek <= DayOfWeek.Friday;

                return isWithinHours && isWeekday;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in business hours check: " + ex.Message);
                return false;
            }
        }



        private void monthCalendarAppointments_DateChanged(object sender, DateRangeEventArgs e)
        {
            DateTime selectedDate = e.Start; // The selected date from the calendar
             // Load the appointments for the selected date
            LoadAppointmentsForSelectedDate(selectedDate);
        }


        private void LoadAppointmentsForSelectedDate(DateTime selectedDate)
        {
            // Construct the SQL query to fetch appointments for the selected date
            string query = @"
        SELECT appointmentId, title, description, location, contact, type, url, start, end 
        FROM appointment 
        WHERE customerId = @customerId 
        AND DATE(start) = @selectedDate";  // Filter appointments for the selected day

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@customerId", CustomerId);
                    command.Parameters.AddWithValue("@selectedDate", selectedDate.Date);  

                    var adapter = new MySqlDataAdapter(command);
                    var dataTable = new DataTable();

                    connection.Open();
                    adapter.Fill(dataTable);

                    // Check if data is loaded into the DataTable
                    if (dataTable.Rows.Count > 0)
                    {
                        dgvAppointments.DataSource = dataTable;  // Display appointments in DataGridView
                    }
                    else
                    {
                        dgvAppointments.DataSource = null;  // Clear previous results if no appointments
                        MessageBox.Show("No appointments found for the selected date.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading appointments: " + ex.Message);
            }
        }

        private void btnShowAllAppointments_Click(object sender, EventArgs e)
        {
            LoadAppointments();
        }

        private DateTime ConvertToUtc(DateTime localTime)
        {
            // Get the user's local time zone
            TimeZoneInfo localTimeZone = TimeZoneInfo.Local;

            // Convert local time to UTC considering DST
            return TimeZoneInfo.ConvertTimeToUtc(localTime, localTimeZone);
        }


        private DateTime ConvertToLocalTime(DateTime utcTime)
        {
            // Get the user's local time zone
            TimeZoneInfo localTimeZone = TimeZoneInfo.Local;

            // Convert UTC time to local time considering DST
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, localTimeZone);
        }

        private void btnReports_Click(object sender, EventArgs e)
        {
       
            ReportsForm reportsForm = new ReportsForm();

 
            reportsForm.ShowDialog();
        }

    }
}
