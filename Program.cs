using System;
using System.Data.SqlClient;

class Program
{
    static string connectionString = "Server=.;Database=SuperMarketSystem;Trusted_Connection=True;";

    // Store logged-in user's ID globally for session
    static int LoggedInUserId = -1;

    static void Main(string[] args)
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== SuperMarket System ===");
            Console.WriteLine("1. Sign Up");
            Console.WriteLine("2. Login");
            Console.WriteLine("3. Exit");
            Console.Write("Choose an option: ");
            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    SignUp();
                    break;
                case "2":
                    Login();
                    break;
                case "3":
                    return;
                default:
                    Console.WriteLine("Invalid choice.");
                    break;
            }

            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
        }
    }

    static void SignUp()
    {
        Console.Clear();
        Console.WriteLine("=== Sign Up ===");

        Console.Write("Full Name: ");
        string fullName = Console.ReadLine();

        Console.Write("Email: ");
        string email = Console.ReadLine();

        Console.Write("Password: ");
        string password = Console.ReadLine();

        Console.Write("User Type (admin/user): ");
        string userType = Console.ReadLine().ToLower();

        if (userType != "admin" && userType != "user")
        {
            Console.WriteLine("Invalid user type.");
            return;
        }

        using (SqlConnection con = new SqlConnection(connectionString))
        {
            con.Open();

            SqlTransaction transaction = con.BeginTransaction();

            try
            {
                // Insert into UserAccount
                string query = "INSERT INTO UserAccount (FullName, Email, Password, User_Type) " +
                               "OUTPUT INSERTED.User_ID VALUES (@FullName, @Email, @Password, @UserType)";
                SqlCommand cmd = new SqlCommand(query, con, transaction);
                cmd.Parameters.AddWithValue("@FullName", fullName);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Password", password); // Replace with hashed password if needed
                cmd.Parameters.AddWithValue("@UserType", userType);

                int userId = (int)cmd.ExecuteScalar();

                // Insert into respective table
                if (userType == "admin")
                {
                    string insertAdmin = "INSERT INTO Admin (User_ID, Name, Phone) VALUES (@UserID, @Name, NULL)";
                    SqlCommand adminCmd = new SqlCommand(insertAdmin, con, transaction);
                    adminCmd.Parameters.AddWithValue("@UserID", userId);
                    adminCmd.Parameters.AddWithValue("@Name", fullName);
                    adminCmd.ExecuteNonQuery();
                }
                else
                {
                    string insertCustomer = "INSERT INTO Customer (User_ID, Name, Address, Phone, No_Visits, Last_Visited) " +
                                            "VALUES (@UserID, @Name, NULL, NULL, 0, NULL)";
                    SqlCommand customerCmd = new SqlCommand(insertCustomer, con, transaction);
                    customerCmd.Parameters.AddWithValue("@UserID", userId);
                    customerCmd.Parameters.AddWithValue("@Name", fullName);
                    customerCmd.ExecuteNonQuery();
                }

                transaction.Commit();
                Console.WriteLine("Account created successfully!");
            }
            catch (SqlException ex)
            {
                transaction.Rollback();

                if (ex.Number == 2627) // Unique constraint error
                    Console.WriteLine("An account with this email already exists.");
                else
                    Console.WriteLine("Error: " + ex.Message);
            }
        }
    }

    
    static void Login()
    {
        Console.Clear();
        Console.WriteLine("=== Login ===");

        Console.Write("Email: ");
        string email = Console.ReadLine();

        Console.Write("Password: ");
        string password = Console.ReadLine();

        using (SqlConnection con = new SqlConnection(connectionString))
        {
            string query = "SELECT User_ID, FullName, User_Type FROM UserAccount WHERE Email = @Email AND Password = @Password";
            SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@Password", password);

            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                LoggedInUserId = (int)reader["User_ID"];
                string fullName = reader["FullName"].ToString();
                string userType = reader["User_Type"].ToString();

                // Update visit count and last visit datetime for Customers only
                if (userType == "user")
                {
                    reader.Close(); // Close reader before running another command on same connection

                    string updateVisitQuery = @"
                        UPDATE Customer 
                        SET No_Visits = ISNULL(No_Visits, 0) + 1, Last_Visited = GETDATE()
                        WHERE User_ID = @UserID";

                    SqlCommand updateVisitCmd = new SqlCommand(updateVisitQuery, con);
                    updateVisitCmd.Parameters.AddWithValue("@UserID", LoggedInUserId);
                    updateVisitCmd.ExecuteNonQuery();
                }
                else
                {
                    reader.Close();
                }

                Console.WriteLine($"\nWelcome, {fullName}!");

                if (userType == "admin")
                    ShowAdminDashboard();
                else
                    

                ShowUserDashboard(LoggedInUserId);

                LoggedInUserId = -1; // reset on logout
            }
            else
            {
                Console.WriteLine("Invalid email or password.");
            }

            con.Close();
        }
    }

    static void ShowAdminDashboard()
    {
        while (true)
        {
            Console.WriteLine("\n=== Admin Dashboard ===");
            Console.WriteLine("1. Sign Up New Customer");
            Console.WriteLine("2. Update Customer Data");
            Console.WriteLine("3. Remove Customer");
            Console.WriteLine("4. Remove Product");
            Console.WriteLine("5. Update Product Details");
            Console.WriteLine("6. Browse All Products");
            Console.WriteLine("7. Show Available Products ");
            Console.WriteLine("8. Show Low-Stock Products ");
            Console.WriteLine("9. Show Frequent Customers ");
            Console.WriteLine("10. Add Product");
            Console.WriteLine("11. Show All Users");
            Console.WriteLine("12. Show All Orders with Details");
            Console.WriteLine("13. Show All Invoices");
            Console.WriteLine("14. Logout");
            Console.Write("Choose an option: ");
            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1": AdminSignUpCustomer(); break;
                case "2": AdminUpdateCustomer(); break;
                case "3": AdminRemoveCustomer(); break;
                case "4": RemoveProduct(); break;
                case "5": UpdateProduct(); break;
                case "6": ViewProducts(); break;
                case "7": ShowAvailableProducts(); break;
                case "8": ShowLowStockProducts(); break;
                case "9": ShowFrequentCustomers(); break;
                case "10": AddProduct(); break;
                case "11": ShowAllUsers(); break;
                case "12": ShowAllOrdersWithDetails(); break;
                case "13": ShowAllInvoices(); break;

                case "14": return;
                default: Console.WriteLine("Invalid choice."); break;
            }
        }
    }





    static void ShowUserDashboard( int LoggedInUserId)
    {
        while (true)
        {
            Console.WriteLine("\n=== User Dashboard ===");
            Console.WriteLine("1. View All Products");
            Console.WriteLine("2. Update My Data");
            Console.WriteLine("3. Remove My Account"); 
            Console.WriteLine("4. make order");
            Console.WriteLine("4. show my orders ");
            Console.WriteLine("5. Logout");
            Console.Write("Choose an option: ");
            string choice = Console.ReadLine();

            if (choice == "1")
                ShowAvailableProducts();
            else if (choice == "2")
                UpdateUserData();
            else if (choice == "3")
            {
                if (RemoveAccount())
                    break;
            }
            else if (choice == "4")
                MakeOrder();
            else if (choice == "5")
                ShowMyOrders(); 
            
            else if (choice == "6")
                break;
            else
                Console.WriteLine("Invalid choice.");
        }
    }




    static void AddProduct()
    {
        Console.WriteLine("\n=== Add New Product ===");
        Console.Write("Product Name: ");
        string name = Console.ReadLine();

        Console.Write("Category (Type): ");
        string category = Console.ReadLine();

        Console.Write("Price: ");
        if (!decimal.TryParse(Console.ReadLine(), out decimal price))
        {
            Console.WriteLine("Invalid price.");
            return;
        }

        Console.Write("Initial Quantity: ");
        if (!int.TryParse(Console.ReadLine(), out int quantity))
        {
            Console.WriteLine("Invalid quantity.");
            return;
        }

        using (SqlConnection con = new SqlConnection(connectionString))
        {
            string query = @"INSERT INTO Product (Name, Type, Price, Quantity)
                         VALUES (@Name, @Type, @Price, @Quantity)";
            SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@Name", name);
            cmd.Parameters.AddWithValue("@Type", category);
            cmd.Parameters.AddWithValue("@Price", price);
            cmd.Parameters.AddWithValue("@Quantity", quantity);

            try
            {
                con.Open();
                cmd.ExecuteNonQuery();
                Console.WriteLine("Product added successfully!");
            }
            catch (SqlException ex)
            {
                Console.WriteLine("Error adding product: " + ex.Message);
            }
        }
    }


    static void ViewProducts()
    {
        using (SqlConnection con = new SqlConnection(connectionString))
        {
            string query = @"
            SELECT Product_ID, Name, Type, Price, Quantity 
            FROM Product 
            ";

            try
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(query, con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    Console.WriteLine("\n--- Available Products ---");
                    Console.WriteLine("ID\tName\t\tType\t\tPrice\tQuantity");
                    Console.WriteLine("------------------------------------------------------");

                    while (reader.Read())
                    {
                        Console.WriteLine($"{reader["Product_ID"]}\t{reader["Name"],-15}\t{reader["Type"],-12}\t{reader["Price"],-8:C}\t{reader["Quantity"]}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while fetching available products: " + ex.Message);
            }
        }
    }


    static void UpdateUserData()
    {
        Console.WriteLine("\n--- Update Your Data ---");

        // First, let's fetch current UserAccount data
        string currentFullName = null;
        string currentEmail = null;

        using (SqlConnection con = new SqlConnection(connectionString))
        {
            con.Open();

            string selectUserQuery = "SELECT FullName, Email FROM UserAccount WHERE User_ID = @UserID";
            SqlCommand userCmd = new SqlCommand(selectUserQuery, con);
            userCmd.Parameters.AddWithValue("@UserID", LoggedInUserId);

            using (SqlDataReader reader = userCmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    currentFullName = reader["FullName"] as string;
                    currentEmail = reader["Email"] as string;
                }
                else
                {
                    Console.WriteLine("No user account data found.");
                    return;
                }
            }
        }

        Console.Write($"Enter new full name (leave blank to keep '{currentFullName}'): ");
        string newName = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(newName)) newName = currentFullName;

        Console.Write($"Enter new email (leave blank to keep '{currentEmail}'): ");
        string newEmail = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(newEmail)) newEmail = currentEmail;

        Console.Write("Enter new address (leave blank to keep current): ");
        string newAddress = Console.ReadLine();

        Console.Write("Enter new phone (leave blank to keep current): ");
        string newPhone = Console.ReadLine();

        using (SqlConnection con = new SqlConnection(connectionString))
        {
            con.Open();

            // Get current Customer data first
            string selectCustomerQuery = "SELECT Name, Address, Phone FROM Customer WHERE User_ID = @UserID";
            SqlCommand selectCustCmd = new SqlCommand(selectCustomerQuery, con);
            selectCustCmd.Parameters.AddWithValue("@UserID", LoggedInUserId);

            string currentAddress = null, currentPhone = null;

            using (SqlDataReader reader = selectCustCmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    // Name comes from UserAccount, so no need here
                    currentAddress = reader["Address"] as string;
                    currentPhone = reader["Phone"] as string;
                }
                else
                {
                    Console.WriteLine("No customer data found.");
                    return;
                }
            }

            if (string.IsNullOrEmpty(newAddress)) newAddress = currentAddress;
            if (string.IsNullOrEmpty(newPhone)) newPhone = currentPhone;

            // Begin transaction to update both tables atomically
            SqlTransaction transaction = con.BeginTransaction();

            try
            {
                // Update UserAccount
                string updateUserQuery = "UPDATE UserAccount SET FullName = @FullName, Email = @Email WHERE User_ID = @UserID";
                SqlCommand updateUserCmd = new SqlCommand(updateUserQuery, con, transaction);
                updateUserCmd.Parameters.AddWithValue("@FullName", newName);
                updateUserCmd.Parameters.AddWithValue("@Email", newEmail);
                updateUserCmd.Parameters.AddWithValue("@UserID", LoggedInUserId);
                updateUserCmd.ExecuteNonQuery();

                // Update Customer
                string updateCustomerQuery = "UPDATE Customer SET Name = @Name, Address = @Address, Phone = @Phone WHERE User_ID = @UserID";
                SqlCommand updateCustomerCmd = new SqlCommand(updateCustomerQuery, con, transaction);
                updateCustomerCmd.Parameters.AddWithValue("@Name", newName);
                updateCustomerCmd.Parameters.AddWithValue("@Address", (object)newAddress ?? DBNull.Value);
                updateCustomerCmd.Parameters.AddWithValue("@Phone", (object)newPhone ?? DBNull.Value);
                updateCustomerCmd.Parameters.AddWithValue("@UserID", LoggedInUserId);
                updateCustomerCmd.ExecuteNonQuery();

                transaction.Commit();
                Console.WriteLine("Your data was updated successfully.");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine("Failed to update data: " + ex.Message);
            }
        }
    }

    static bool RemoveAccount()
    {
        Console.WriteLine("\nAre you sure you want to delete your account? This action cannot be undone. (yes/no)");
        string confirmation = Console.ReadLine().ToLower();

        if (confirmation != "yes")
        {
            Console.WriteLine("Account deletion cancelled.");
            return false;
        }

        using (SqlConnection con = new SqlConnection(connectionString))
        {
            con.Open();

            SqlTransaction transaction = con.BeginTransaction();

            try
            {
                // Delete from Customer
                string deleteCustomer = "DELETE FROM Customer WHERE User_ID = @UserID";
                SqlCommand delCustCmd = new SqlCommand(deleteCustomer, con, transaction);
                delCustCmd.Parameters.AddWithValue("@UserID", LoggedInUserId);
                delCustCmd.ExecuteNonQuery();

                // Delete from Admin (if exists)
                string deleteAdmin = "DELETE FROM Admin WHERE User_ID = @UserID";
                SqlCommand delAdminCmd = new SqlCommand(deleteAdmin, con, transaction);
                delAdminCmd.Parameters.AddWithValue("@UserID", LoggedInUserId);
                delAdminCmd.ExecuteNonQuery();

                // Finally delete UserAccount
                string deleteUser = "DELETE FROM UserAccount WHERE User_ID = @UserID";
                SqlCommand delUserCmd = new SqlCommand(deleteUser, con, transaction);
                delUserCmd.Parameters.AddWithValue("@UserID", LoggedInUserId);
                delUserCmd.ExecuteNonQuery();

                transaction.Commit();

                Console.WriteLine("Your account was deleted successfully.");
                return true; // Indicate logout needed
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine("Error deleting account: " + ex.Message);
                return false;
            }
        }
    }


    static void MakeOrder()
    {
        using (SqlConnection con = new SqlConnection(connectionString))
        {
            con.Open();

            
            string getCustomerIdQuery = "SELECT Customer_ID FROM Customer WHERE User_ID = @UserID";
            SqlCommand getCustomerCmd = new SqlCommand(getCustomerIdQuery, con);
            getCustomerCmd.Parameters.AddWithValue("@UserID", LoggedInUserId);

            int customerId = (int?)getCustomerCmd.ExecuteScalar() ?? -1;

            if (customerId == -1)
            {
                Console.WriteLine("Error: Customer record not found.");
                return;
            }

            // Step 2: Ask for product ID and quantity
            Console.Write("Enter Product ID: ");
            int productId = int.Parse(Console.ReadLine());

            Console.Write("Enter Quantity: ");
            int quantityRequested = int.Parse(Console.ReadLine());

            // Step 3: Check product availability
            string checkProductQuery = "SELECT Quantity, Price FROM Product WHERE Product_ID = @ProductID";
            SqlCommand checkProductCmd = new SqlCommand(checkProductQuery, con);
            checkProductCmd.Parameters.AddWithValue("@ProductID", productId);

            SqlDataReader reader = checkProductCmd.ExecuteReader();

            if (!reader.Read())
            {
                Console.WriteLine("Product not found.");
                return;
            }

            int availableQuantity = (int)reader["Quantity"];
            decimal price = (decimal)reader["Price"];
            reader.Close();

            if (availableQuantity < quantityRequested)
            {
                Console.WriteLine("Insufficient quantity in stock.");
                return;
            }

            // Step 4: Insert into Orders
            string insertOrderQuery = "INSERT INTO Orders (Customer_ID, OrderDate) OUTPUT INSERTED.Order_ID VALUES (@CustomerID, GETDATE())";
            SqlCommand insertOrderCmd = new SqlCommand(insertOrderQuery, con);
            insertOrderCmd.Parameters.AddWithValue("@CustomerID", customerId);
            int orderId = (int)insertOrderCmd.ExecuteScalar();

            // Step 5: Insert into OrderDetails
            string insertDetailsQuery = "INSERT INTO OrderDetails (Order_ID, Product_ID, Quantity) VALUES (@OrderID, @ProductID, @Quantity)";
            SqlCommand insertDetailsCmd = new SqlCommand(insertDetailsQuery, con);
            insertDetailsCmd.Parameters.AddWithValue("@OrderID", orderId);
            insertDetailsCmd.Parameters.AddWithValue("@ProductID", productId);
            insertDetailsCmd.Parameters.AddWithValue("@Quantity", quantityRequested);
            insertDetailsCmd.ExecuteNonQuery();

            // Step 6: Update product quantity
            string updateQtyQuery = "UPDATE Product SET Quantity = Quantity - @OrderedQty WHERE Product_ID = @ProductID";
            SqlCommand updateQtyCmd = new SqlCommand(updateQtyQuery, con);
            updateQtyCmd.Parameters.AddWithValue("@OrderedQty", quantityRequested);
            updateQtyCmd.Parameters.AddWithValue("@ProductID", productId);
            updateQtyCmd.ExecuteNonQuery();

            // Step 7: Create invoice
            decimal taxRate = 0.10M;
            decimal subtotal = price * quantityRequested;
            decimal tax = subtotal * taxRate;
            decimal finalAmount = subtotal + tax;

            string insertInvoiceQuery = @"
            INSERT INTO Invoice (Order_ID, InvoiceDate, Tax, FinalAmount)
            VALUES (@OrderID, GETDATE(), @Tax, @FinalAmount)";
            SqlCommand insertInvoiceCmd = new SqlCommand(insertInvoiceQuery, con);
            insertInvoiceCmd.Parameters.AddWithValue("@OrderID", orderId);
            insertInvoiceCmd.Parameters.AddWithValue("@Tax", tax);
            insertInvoiceCmd.Parameters.AddWithValue("@FinalAmount", finalAmount);
            insertInvoiceCmd.ExecuteNonQuery();

            Console.WriteLine("✅ Order placed successfully!");
            Console.WriteLine($"🧾 Final Amount (including tax): {finalAmount:C}");
        }
    }


    static void ShowMyOrders()
    {
        using (SqlConnection con = new SqlConnection(connectionString))
        {
            con.Open();

            // Step 1: Get Customer_ID for the logged-in user
            string getCustomerIdQuery = "SELECT Customer_ID FROM Customer WHERE User_ID = @UserID";
            SqlCommand getCustomerCmd = new SqlCommand(getCustomerIdQuery, con);
            getCustomerCmd.Parameters.AddWithValue("@UserID", LoggedInUserId);

            int customerId = (int?)getCustomerCmd.ExecuteScalar() ?? -1;

            if (customerId == -1)
            {
                Console.WriteLine("❌ Customer record not found.");
                return;
            }

            // Step 2: Get Orders with Product Details
            string ordersQuery = @"
            SELECT o.Order_ID, o.OrderDate, 
                   p.Name AS ProductName, od.Quantity AS ProductQuantity
            FROM Orders o
            INNER JOIN OrderDetails od ON o.Order_ID = od.Order_ID
            INNER JOIN Product p ON od.Product_ID = p.Product_ID
            WHERE o.Customer_ID = @CustomerID
            ORDER BY o.OrderDate DESC";

            SqlCommand ordersCmd = new SqlCommand(ordersQuery, con);
            ordersCmd.Parameters.AddWithValue("@CustomerID", customerId);

            SqlDataReader reader = ordersCmd.ExecuteReader();

            Console.WriteLine("\n📦 Your Orders:");
            Console.WriteLine("Order ID\tDate\t\tProduct\t\tQuantity");

            while (reader.Read())
            {
                Console.WriteLine($"{reader["Order_ID"]}\t\t{Convert.ToDateTime(reader["OrderDate"]).ToShortDateString(),-10}\t{reader["ProductName"],-15}\t{reader["ProductQuantity"]}");
            }

            reader.Close();
        }
    }





    //ADMIN FUNCTIONS



    static void ShowAllUsers()
    {
        using (SqlConnection con = new SqlConnection(connectionString))
        {
            string query = @"
            SELECT u.User_ID, u.FullName, u.Email, u.User_Type
            FROM UserAccount u";

            SqlCommand cmd = new SqlCommand(query, con);

            try
            {
                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    Console.WriteLine("\n--- All Users ---");
                    Console.WriteLine("ID\tName\t\t\tEmail\t\t\t\tType");

                    while (reader.Read())
                    {
                        Console.WriteLine($"{reader["User_ID"]}\t{reader["FullName"],-20}\t{reader["Email"],-30}\t{reader["User_Type"]}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching users: " + ex.Message);
            }
        }
    }




    static void ShowAllOrdersWithDetails()
    {
        using (SqlConnection con = new SqlConnection(connectionString))
        {
            string query = @"
            SELECT o.Order_ID, o.OrderDate, c.Name AS CustomerName,
                   p.Name AS ProductName, od.Quantity, p.Price,
                   (od.Quantity * p.Price) AS TotalItemPrice
            FROM Orders o
            JOIN Customer c ON o.Customer_ID = c.Customer_ID
            JOIN OrderDetails od ON o.Order_ID = od.Order_ID
            JOIN Product p ON od.Product_ID = p.Product_ID
            ORDER BY o.Order_ID";

            SqlCommand cmd = new SqlCommand(query, con);

            try
            {
                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    Console.WriteLine("\n--- All Orders with Details ---");
                    Console.WriteLine("OrderID\tDate\t\tCustomer\tProduct\t\tQty\tUnitPrice\tTotal");

                    while (reader.Read())
                    {
                        Console.WriteLine($"{reader["Order_ID"]}\t{reader["OrderDate"]:yyyy-MM-dd}\t{reader["CustomerName"],-12}" +
                                          $"\t{reader["ProductName"],-12}\t{reader["Quantity"]}\t{reader["Price"]:C}" +
                                          $"\t\t{reader["TotalItemPrice"]:C}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching orders: " + ex.Message);
            }
        }
    }



    static void ShowAllInvoices()
    {
        using (SqlConnection con = new SqlConnection(connectionString))
        {
            string query = @"
            SELECT i.Invoice_ID, i.InvoiceDate, i.Tax, i.FinalAmount,
                   o.Order_ID, c.Name AS CustomerName, o.OrderDate
            FROM Invoice i
            JOIN Orders o ON i.Order_ID = o.Order_ID
            JOIN Customer c ON o.Customer_ID = c.Customer_ID
            ORDER BY i.Invoice_ID";

            SqlCommand cmd = new SqlCommand(query, con);

            try
            {
                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    Console.WriteLine("\n--- All Invoices ---");
                    Console.WriteLine("InvoiceID\tDate\t\tOrderID\tCustomer\tAmount\tTax\tFinal");

                    while (reader.Read())
                    {
                        Console.WriteLine($"{reader["Invoice_ID"]}\t\t{reader["InvoiceDate"]:yyyy-MM-dd}\t{reader["Order_ID"]}" +
                                          $"\t{reader["CustomerName"],-12}\t{reader["FinalAmount"]:C}" +
                                          $"\t{reader["Tax"]:F2}%\t{reader["FinalAmount"]:C}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching invoices: " + ex.Message);
            }
        }
    }


    static void AdminSignUpCustomer()
    {
        Console.Clear();
        Console.WriteLine("=== Admin: Sign Up Customer ===");
        Console.Write("Full Name: ");
        string fullName = Console.ReadLine();
        Console.Write("Email: ");
        string email = Console.ReadLine();
        Console.Write("Password: ");
        string password = Console.ReadLine();

        using (SqlConnection con = new SqlConnection(connectionString))
        {
            con.Open();
            SqlTransaction transaction = con.BeginTransaction();
            try
            {
                string query = "INSERT INTO UserAccount (FullName, Email, Password, User_Type) " +
                               "OUTPUT INSERTED.User_ID VALUES (@FullName, @Email, @Password, 'user')";
                SqlCommand cmd = new SqlCommand(query, con, transaction);
                cmd.Parameters.AddWithValue("@FullName", fullName);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Password", password);

                int userId = (int)cmd.ExecuteScalar();

                string insertCustomer = "INSERT INTO Customer (User_ID, Name, Address, Phone, No_Visits, Last_Visited) " +
                                        "VALUES (@UserID, @Name, NULL, NULL, 0, NULL)";
                SqlCommand customerCmd = new SqlCommand(insertCustomer, con, transaction);
                customerCmd.Parameters.AddWithValue("@UserID", userId);
                customerCmd.Parameters.AddWithValue("@Name", fullName);
                customerCmd.ExecuteNonQuery();

                transaction.Commit();
                Console.WriteLine("Customer account created successfully.");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }



    static void AdminUpdateCustomer()
    {
        Console.Write("Enter customer User_ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out int userId)) return;

        Console.Write("New Name: ");
        string name = Console.ReadLine();
        Console.Write("New Email: ");
        string email = Console.ReadLine();
        Console.Write("New Address: ");
        string address = Console.ReadLine();
        Console.Write("New Phone: ");
        string phone = Console.ReadLine();

        using (SqlConnection con = new SqlConnection(connectionString))
        {
            con.Open();
            SqlTransaction tr = con.BeginTransaction();
            try
            {
                new SqlCommand("UPDATE UserAccount SET FullName = @name, Email = @mail WHERE User_ID = @id", con, tr)
                {
                    Parameters = { new SqlParameter("@name", name), new SqlParameter("@mail", email), new SqlParameter("@id", userId) }
                }.ExecuteNonQuery();

                new SqlCommand("UPDATE Customer SET Name = @name, Address = @address, Phone = @phone WHERE User_ID = @id", con, tr)
                {
                    Parameters = {
                    new SqlParameter("@name", name),
                    new SqlParameter("@address", (object)address ?? DBNull.Value),
                    new SqlParameter("@phone", (object)phone ?? DBNull.Value),
                    new SqlParameter("@id", userId)
                }
                }.ExecuteNonQuery();

                tr.Commit();
                Console.WriteLine("Customer data updated.");
            }
            catch (Exception ex)
            {
                tr.Rollback();
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }


    static void AdminRemoveCustomer()
    {
        Console.Write("Enter customer User_ID to remove: ");
        if (!int.TryParse(Console.ReadLine(), out int userId)) return;

        using (SqlConnection con = new SqlConnection(connectionString))
        {
            con.Open();
            SqlTransaction tr = con.BeginTransaction();
            try
            {
                SqlCommand deleteCustomerCmd = new SqlCommand("DELETE FROM Customer WHERE User_ID = @id", con, tr);
                deleteCustomerCmd.Parameters.AddWithValue("@id", userId);
                deleteCustomerCmd.ExecuteNonQuery();

                SqlCommand deleteUserCmd = new SqlCommand("DELETE FROM UserAccount WHERE User_ID = @id", con, tr);
                deleteUserCmd.Parameters.AddWithValue("@id", userId);
                deleteUserCmd.ExecuteNonQuery();

                tr.Commit();
                Console.WriteLine("Customer removed successfully.");
            }
            catch (Exception ex)
            {
                tr.Rollback();
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }




    static void RemoveProduct()
    {
        Console.Write("Enter Product_ID to remove: ");
        if (!int.TryParse(Console.ReadLine(), out int id)) return;

        using (SqlConnection con = new SqlConnection(connectionString))
        {
            SqlCommand cmd = new SqlCommand("DELETE FROM Product WHERE Product_ID = @id", con);
            cmd.Parameters.AddWithValue("@id", id);
            con.Open();
            int rows = cmd.ExecuteNonQuery();
            Console.WriteLine(rows > 0 ? "Product removed." : "Product not found.");
        }
    }




    static void UpdateProduct()
    {
        Console.Write("Enter Product_ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out int id)) return;

        Console.Write("New Name: ");
        string name = Console.ReadLine();
        Console.Write("New Type: ");
        string type = Console.ReadLine();
        Console.Write("New Price: ");
        if (!decimal.TryParse(Console.ReadLine(), out decimal price)) return;

        using (SqlConnection con = new SqlConnection(connectionString))
        {
            string query = "UPDATE Product SET Name = @name, Type = @type, Price = @price WHERE Product_ID = @id";
            SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@type", type);
            cmd.Parameters.AddWithValue("@price", price);
            cmd.Parameters.AddWithValue("@id", id);
            con.Open();
            cmd.ExecuteNonQuery();
            Console.WriteLine("Product updated.");
        }
    }





    static void ShowAvailableProducts()
    {
        using (SqlConnection con = new SqlConnection(connectionString))
        {
            string query = @"
            SELECT Product_ID, Name, Type, Price, Quantity 
            FROM Product 
            WHERE Quantity > 0";

            try
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(query, con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    Console.WriteLine("\n--- Available Products ---");
                    Console.WriteLine("ID\tName\t\tType\t\tPrice\tQuantity");
                    Console.WriteLine("------------------------------------------------------");

                    while (reader.Read())
                    {
                        Console.WriteLine($"{reader["Product_ID"]}\t{reader["Name"],-15}\t{reader["Type"],-12}\t{reader["Price"],-8:C}\t{reader["Quantity"]}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while fetching available products: " + ex.Message);
            }
        }
    }





    static void ShowLowStockProducts()
    {
        Console.Write("Enter stock threshold (e.g., 5): ");
        if (!int.TryParse(Console.ReadLine(), out int threshold)) return;

        using (SqlConnection con = new SqlConnection(connectionString))
        {
            string query = "SELECT Product_ID, Name, Quantity FROM Product WHERE Quantity < @threshold";
            SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@threshold", threshold);
            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();
            Console.WriteLine("\n--- Low Stock Products ---");
            while (reader.Read())
            {
                Console.WriteLine($"ID: {reader["Product_ID"]} | Name: {reader["Name"]} | Quantity: {reader["Quantity"]}");
            }
        }
    }





    static void ShowFrequentCustomers()
    {
        Console.Write("Enter minimum visit count (e.g., 10): ");
        if (!int.TryParse(Console.ReadLine(), out int minVisits)) return;

        using (SqlConnection con = new SqlConnection(connectionString))
        {
            string query = @"
            SELECT c.Name, u.Email, c.No_Visits
            FROM Customer c
            JOIN UserAccount u ON c.User_ID = u.User_ID
            WHERE c.No_Visits >= @visits";

            try
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@visits", minVisits);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        Console.WriteLine("\n--- Frequent Customers ---");
                        Console.WriteLine("Name\t\t\tEmail\t\t\t\tVisits");
                        Console.WriteLine("--------------------------------------------------------------");

                        while (reader.Read())
                        {
                            Console.WriteLine(
                                $"{reader["Name"],-20}\t" +
                                $"{reader["Email"],-30}\t" +
                                $"{reader["No_Visits"]}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while fetching frequent customers: " + ex.Message);
            }
        }
    }




}

