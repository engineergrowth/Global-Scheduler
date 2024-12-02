using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Scheduler
{
    public partial class CustomerForm : Form
    {
        private string connectionString;
        private int currentCustomerId;

        public CustomerForm(string connString)
        {
            InitializeComponent();
            connectionString = connString;
            this.Shown += CustomerForm_Shown;
        }

        private void CustomerForm_Shown(object sender, EventArgs e)
        {
            LoadCustomers();
        }


        // Insert a new country into the database and return the countryId
        private int InsertCountry(string countryName)
        {
            int countryId = -1;
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "INSERT INTO country (country, createDate, createdBy, lastUpdate, lastUpdateBy) " +
                                   "VALUES (@country, @createDate, @createdBy, @lastUpdate, @lastUpdateBy)";
                    MySqlCommand cmd = new MySqlCommand(query, conn);

                    cmd.Parameters.AddWithValue("@country", countryName);
                    cmd.Parameters.AddWithValue("@createDate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@createdBy", "System");
                    cmd.Parameters.AddWithValue("@lastUpdate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@lastUpdateBy", "System");

                    cmd.ExecuteNonQuery();

                    // Retrieve the generated countryId
                    cmd.CommandText = "SELECT LAST_INSERT_ID()"; // Get the last inserted ID
                    countryId = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error inserting country: {ex.Message}");
            }
            return countryId;
        }

        // Insert a new city into the database and return the cityId
        private int InsertCity(string cityName, int countryId)
        {
            int cityId = -1;
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "INSERT INTO city (city, countryId, createDate, createdBy, lastUpdate, lastUpdateBy) " +
                                   "VALUES (@city, @countryId, @createDate, @createdBy, @lastUpdate, @lastUpdateBy)";
                    MySqlCommand cmd = new MySqlCommand(query, conn);

                    cmd.Parameters.AddWithValue("@city", cityName);
                    cmd.Parameters.AddWithValue("@countryId", countryId);
                    cmd.Parameters.AddWithValue("@createDate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@createdBy", "System");
                    cmd.Parameters.AddWithValue("@lastUpdate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@lastUpdateBy", "System");

                    cmd.ExecuteNonQuery();

                    // Retrieve the generated cityId
                    cmd.CommandText = "SELECT LAST_INSERT_ID()"; 
                    cityId = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error inserting city: {ex.Message}");
            }
            return cityId;
        }

        // Insert a new address and return the addressId
        private int InsertAddress(string address1, string address2, int cityId, string postalCode, string phone)
        {
            int addressId = -1;
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "INSERT INTO address (address, address2, cityId, postalCode, phone, createDate, createdBy, lastUpdate, lastUpdateBy) " +
                                   "VALUES (@address, @address2, @cityId, @postalCode, @phone, @createDate, @createdBy, @lastUpdate, @lastUpdateBy)";
                    MySqlCommand cmd = new MySqlCommand(query, conn);

                    cmd.Parameters.AddWithValue("@address", address1);
                    cmd.Parameters.AddWithValue("@address2", address2);
                    cmd.Parameters.AddWithValue("@cityId", cityId);
                    cmd.Parameters.AddWithValue("@postalCode", postalCode);
                    cmd.Parameters.AddWithValue("@phone", phone);
                    cmd.Parameters.AddWithValue("@createDate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@createdBy", "System");
                    cmd.Parameters.AddWithValue("@lastUpdate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@lastUpdateBy", "System");

                    cmd.ExecuteNonQuery();

                    // Retrieve the generated addressId
                    cmd.CommandText = "SELECT LAST_INSERT_ID()"; // Get the last inserted ID
                    addressId = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error inserting address: {ex.Message}");
            }
            return addressId;
        }

        // Insert a new customer record
        private void InsertCustomer(string customerName, int addressId, bool isActive)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "INSERT INTO customer (customerName, addressId, active, createDate, createdBy, lastUpdate, lastUpdateBy) " +
                                   "VALUES (@customerName, @addressId, @active, @createDate, @createdBy, @lastUpdate, @lastUpdateBy)";
                    MySqlCommand cmd = new MySqlCommand(query, conn);

                    cmd.Parameters.AddWithValue("@customerName", customerName);
                    cmd.Parameters.AddWithValue("@addressId", addressId);
                    cmd.Parameters.AddWithValue("@active", isActive);
                    cmd.Parameters.AddWithValue("@createDate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@createdBy", "System");
                    cmd.Parameters.AddWithValue("@lastUpdate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@lastUpdateBy", "System");

                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Customer added successfully.");
                    ClearFields(); 
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding customer: {ex.Message}");
            }
        }

        // Clear the input fields after adding a customer
        private void ClearFields()
        {
            txtCustomerName.Clear();
            txtAddress1.Clear();
            txtAddress2.Clear();
            txtPostalCode.Clear();
            txtPhone.Clear();
            txtCountry.Clear();
            txtCity.Clear();
            chkActive.Checked = false;
        }

        // Load customers into DataGridView dgvCustomers
        private void LoadCustomers()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT c.customerId, c.customerName, a.address, a.address2, a.postalCode, a.phone, 
                 ci.city, co.country, c.active 
                 FROM customer c 
                 JOIN address a ON c.addressId = a.addressId
                 JOIN city ci ON a.cityId = ci.cityId
                 JOIN country co ON ci.countryId = co.countryId";


                    MySqlDataAdapter da = new MySqlDataAdapter(query, conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    dgvCustomers.DataSource = dt;
                    dgvCustomers.ReadOnly = true;
                    dgvCustomers.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading customer data: {ex.Message}");
            }
        }

        // Handle selection of a customer to edit
        private void dgvCustomers_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < dgvCustomers.Rows.Count - 1) // Ensure the row is not the last blank row
            {
                DataGridViewRow selectedRow = dgvCustomers.Rows[e.RowIndex];

                // Check if values exist for each column and assign to textboxes
                txtCustomerName.Text = selectedRow.Cells["customerName"].Value?.ToString() ?? "";
                txtAddress1.Text = selectedRow.Cells["address"].Value?.ToString() ?? "";
                txtAddress2.Text = selectedRow.Cells["address2"].Value?.ToString() ?? "";
                txtCity.Text = selectedRow.Cells["city"].Value?.ToString() ?? "";
                txtCountry.Text = selectedRow.Cells["country"].Value?.ToString() ?? "";
                txtPostalCode.Text = selectedRow.Cells["postalCode"].Value?.ToString() ?? "";
                txtPhone.Text = selectedRow.Cells["phone"].Value?.ToString() ?? "";
                chkActive.Checked = selectedRow.Cells["active"].Value != DBNull.Value && Convert.ToBoolean(selectedRow.Cells["active"].Value);

                // Store the selected customerId
                currentCustomerId = Convert.ToInt32(selectedRow.Cells["customerId"].Value);
            }
        }

        private bool ColumnExists(string columnName, string tableName)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = $"DESCRIBE {tableName}";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                MySqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (reader["Field"].ToString() == columnName)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        // Method to update an existing customer
        private void btnUpdateCustomer_Click(object sender, EventArgs e)
        {
            string customerName = txtCustomerName.Text.Trim();
            string address1 = txtAddress1.Text.Trim();
            string address2 = txtAddress2.Text.Trim();
            string cityName = txtCity.Text.Trim();
            string countryName = txtCountry.Text.Trim();
            string postalCode = txtPostalCode.Text.Trim();
            string phone = txtPhone.Text.Trim();
            bool isActive = chkActive.Checked;

            // Validate that required fields are filled
            if (!ValidateRequiredFields(customerName, address1, cityName, countryName, postalCode, phone))
            {
                return; 
            }

            // Get the current customer's existing addressId
            int existingAddressId = GetExistingAddressId(currentCustomerId);

            // Check if country exists, if not, insert it
            int countryId = GetCountryIdByName(countryName);
            if (countryId == -1) 
            {
                countryId = InsertCountry(countryName);
                if (countryId == -1)
                {
                    MessageBox.Show("Error inserting country.");
                    return;
                }
            }

            // Check if city exists in the country, if not, insert it
            int cityId = GetCityIdByName(cityName, countryId);
            if (cityId == -1)  
            {
                cityId = InsertCity(cityName, countryId);
                if (cityId == -1)
                {
                    MessageBox.Show("Error inserting city.");
                    return;
                }
            }

            // If the existing addressId is found, update the address
            if (existingAddressId > 0)
            {
                // Update the existing address
                UpdateAddress(existingAddressId, address1, address2, cityId, postalCode, phone);
            }
            else
            {
                // Insert the address if no existing addressId is found and get the new addressId
                existingAddressId = InsertAddress(address1, address2, cityId, postalCode, phone);
                if (existingAddressId == -1)
                {
                    MessageBox.Show("Error inserting address.");
                    return;
                }
            }

            // Update the customer with the new addressId
            UpdateCustomer(currentCustomerId, customerName, existingAddressId, isActive);

            // Reload customers after updating the customer
            LoadCustomers();
        }

        // Get the existing addressId for the current customer
        private int GetExistingAddressId(int customerId)
        {
            int addressId = -1;
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT addressId FROM customer WHERE customerId = @customerId";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@customerId", customerId);

                    object result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        addressId = Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error retrieving addressId for customer: {ex.Message}");
            }
            return addressId;
        }

        // Update the customer with the new addressId and other details
        private void UpdateCustomer(int customerId, string customerName, int addressId, bool isActive)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
               
                    string query = "UPDATE customer SET customerName = @customerName, addressId = @addressId, active = @isActive WHERE customerId = @customerId";
                    MySqlCommand cmd = new MySqlCommand(query, conn);

                    cmd.Parameters.AddWithValue("@customerName", customerName);
                    cmd.Parameters.AddWithValue("@addressId", addressId);
                    cmd.Parameters.AddWithValue("@isActive", isActive ? 1 : 0); 
                    cmd.Parameters.AddWithValue("@customerId", customerId);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected == 0)
                    {
                        MessageBox.Show("Error updating customer.");
                    }
                    else
                    {
                        MessageBox.Show("Customer updated successfully.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating customer: {ex.Message}");
            }
        }


        // Method to update the address
        private void UpdateAddress(int addressId, string address, string address2, int cityId, string postalCode, string phone)
        {
            // Validate that required fields are filled
            if (!ValidateRequiredFields("", address, "", "", postalCode, phone))  // customerName is not required in update, so pass empty string
            {
                return; // Validation failed, stop execution
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "UPDATE address SET address = @address, address2 = @address2, cityId = @cityId, postalCode = @postalCode, phone = @phone WHERE addressId = @addressId";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@address", address);  // mapping to 'address' in the table
                    cmd.Parameters.AddWithValue("@address2", address2);  // mapping to 'address2'
                    cmd.Parameters.AddWithValue("@cityId", cityId);      // mapping to 'cityId'
                    cmd.Parameters.AddWithValue("@postalCode", postalCode);  // mapping to 'postalCode'
                    cmd.Parameters.AddWithValue("@phone", phone);         // mapping to 'phone'
                    cmd.Parameters.AddWithValue("@addressId", addressId);  // mapping to 'addressId'

                    // Execute the query
                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected == 0)
                    {
                        MessageBox.Show("Error updating address.");
                    }
                    else
                    {
                        MessageBox.Show("Address updated successfully.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating address: {ex.Message}");
            }
        }


        // Delete customer from the database
        private void btnDeleteCustomer_Click(object sender, EventArgs e)
        {
            if (dgvCustomers.SelectedRows.Count > 0)
            {
                int customerId = Convert.ToInt32(dgvCustomers.SelectedRows[0].Cells["customerId"].Value);
                DeleteCustomer(customerId);
                LoadCustomers(); // Refresh the customer list
            }
        }

        // Delete customer from database
        private void DeleteCustomer(int customerId)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "DELETE FROM customer WHERE customerId = @customerId";
                    MySqlCommand cmd = new MySqlCommand(query, conn);

                    cmd.Parameters.AddWithValue("@customerId", customerId);
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Customer deleted successfully.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting customer: {ex.Message}");
            }
        }

        private void ConfigureDataGridView()
        {
            dgvCustomers.SelectionMode = DataGridViewSelectionMode.FullRowSelect; // Select entire row
            dgvCustomers.AllowUserToAddRows = false; // Disable adding rows manually
            dgvCustomers.AllowUserToDeleteRows = false; // Disable deleting rows manually
            dgvCustomers.AllowUserToResizeColumns = true; // Allow column resizing

            // Ensure the DataGridView is read-only after setting the DataSource
            dgvCustomers.ReadOnly = true;
        }

        // Form load event to populate customer list
        private void CustomerForm_Load(object sender, EventArgs e)
        {
            LoadCustomers();
            ConfigureDataGridView();
        }


        // Get countryId by country name
        private int GetCountryIdByName(string countryName)
        {
            int countryId = -1;
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT countryId FROM country WHERE country = @country";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@country", countryName);

                    object result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        countryId = Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error retrieving countryId: {ex.Message}");
            }
            return countryId;
        }

        // Get cityId by city name and countryId
        private int GetCityIdByName(string cityName, int countryId)
        {
            int cityId = -1;
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT cityId FROM city WHERE city = @city AND countryId = @countryId";
                    MySqlCommand cmd = new MySqlCommand(query, conn);

                    cmd.Parameters.AddWithValue("@city", cityName);
                    cmd.Parameters.AddWithValue("@countryId", countryId);

                    object result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        cityId = Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error retrieving cityId: {ex.Message}");
            }
            return cityId;
        }


        private void btnClear_Click(object sender, EventArgs e)
        {
            ClearFields();
        }

        private void btnAddCustomer_Click(object sender, EventArgs e)
        {
            string customerName = txtCustomerName.Text.Trim();
            string address1 = txtAddress1.Text.Trim();
            string address2 = txtAddress2.Text.Trim();
            string cityName = txtCity.Text.Trim();
            string countryName = txtCountry.Text.Trim();
            string postalCode = txtPostalCode.Text.Trim();
            string phone = txtPhone.Text.Trim();
            bool isActive = chkActive.Checked;

            // Validate that required fields are filled
            if (!ValidateRequiredFields(customerName, address1, cityName, countryName, postalCode, phone))
            {
                return;
            }

            // Check if country exists, if not, insert it
            int countryId = GetCountryIdByName(countryName);
            if (countryId == -1) 
            {
                countryId = InsertCountry(countryName);
                if (countryId == -1)
                {
                    MessageBox.Show("Error inserting country.");
                    return;
                }
            }

            // Check if city exists in the country, if not, insert it
            int cityId = GetCityIdByName(cityName, countryId);
            if (cityId == -1)  // City doesn't exist, insert it
            {
                cityId = InsertCity(cityName, countryId);
                if (cityId == -1)
                {
                    MessageBox.Show("Error inserting city.");
                    return;
                }
            }

            // Insert the address and get the addressId
            int addressId = InsertAddress(address1, address2, cityId, postalCode, phone);
            if (addressId == -1)
            {
                MessageBox.Show("Error inserting address.");
                return;
            }

            // Insert the customer
            InsertCustomer(customerName, addressId, isActive);
            LoadCustomers();
        }

        private bool ValidateRequiredFields(string customerName, string address, string city, string country, string postalCode, string phone)
        {
            if (string.IsNullOrWhiteSpace(customerName))
            {
                MessageBox.Show("Customer name is required.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(address))
            {
                MessageBox.Show("Address is required.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(city))
            {
                MessageBox.Show("City is required.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(country))
            {
                MessageBox.Show("Country is required.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(postalCode))
            {
                MessageBox.Show("Postal code is required.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(phone))
            {
                MessageBox.Show("Phone is required.");
                return false;
            }

            if (!Regex.IsMatch(phone, @"^[\d\-]+$"))
            {
                MessageBox.Show("Phone number can only contain digits and dashes.");
                return false;
            }

            if (!Regex.IsMatch(postalCode, @"^[A-Za-z0-9\- ]+$"))
            {
                MessageBox.Show("Postal code is not in a valid format.");
                return false;
            }

            return true;
        }


        // Event handler for the "View Appointments" button
        private void btnAppointments_Click(object sender, EventArgs e)
        {
            // Ensure the customer ID is valid and set
            if (currentCustomerId <= 0)
            {
                // No valid customer selected, show an error message
                MessageBox.Show("Please select a customer first.", "Selection Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // If customer ID is valid, create and show the AppointmentListForm, passing the currentCustomerId
            AppointmentListForm appointmentListForm = new AppointmentListForm(currentCustomerId); 
            appointmentListForm.ShowDialog(); // Show the AppointmentListForm
        }

       
    }

}
