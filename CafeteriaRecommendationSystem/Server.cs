using MySql.Data.MySqlClient;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Server
{
    private static TcpListener listener;

    public static void Main()
    {
        listener = new TcpListener(IPAddress.Any, 5001);
        listener.Start();
        Console.WriteLine("Server started on port 5001");

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            Console.WriteLine("Client connected");
            Thread clientThread = new Thread(HandleClient);
            clientThread.Start(client);
        }
    }

    private static void HandleClient(object obj)
    {
        TcpClient client = (TcpClient)obj;
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        int byteCount;

        try
        {
            while ((byteCount = stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                string request = Encoding.ASCII.GetString(buffer, 0, byteCount);
                Console.WriteLine("Received: " + request);
                string[] requestParts = request.Trim().Split(':');
                string email = requestParts[0];
                string action = requestParts.Length > 1 ? requestParts[1] : "";
                string parameters = requestParts.Length > 2 ? requestParts[2] : "";

                LoginResult result = LoginUser(email);
                string response;

                if (result.Success)
                {
                    response = $"Login successful as {result.Role} with UserId {result.UserId}. ";
                    response += ExecuteRoleBasedFunctionality(result.Role, action, parameters);
                }
                else
                {
                    response = "Login failed";
                }

                byte[] responseData = Encoding.ASCII.GetBytes(response);
                stream.Write(responseData, 0, responseData.Length);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception: " + e.Message);
        }
        finally
        {
            stream.Close();
            client.Close();
        }
    }

    private static LoginResult LoginUser(string email)
    {
        string connectionString = "Server=localhost;Database=cafeteriarecommenderdb;User ID=root;Password=ronak5840R!;Port=3306";

        try
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT u.UserId, RoleName FROM Users u JOIN Roles r ON u.RoleId = r.RoleId WHERE Email = @Email";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int userId = reader.GetInt32(0);
                            string role = reader.GetString(1);
                            return new LoginResult { Success = true, UserId = userId, Role = role };
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Database exception: " + ex.Message);
        }
        return new LoginResult { Success = false,UserId = 0, Role = "" };
    }

    private static string ExecuteRoleBasedFunctionality(string role, string action, string parameters)
    {
        switch (role)
        {
            case "Admin":
                return AdminFunctionality(action, parameters);
            case "Chef":
                return ChefFunctionality(action, parameters);
            case "Employee":
                return EmployeeFunctionality(action, parameters);
            default:
                return "No specific functionality";
        }
    }

    private static string AdminFunctionality(string action, string parameters)
    {
        switch (action.ToLower())
        {
            case "additem":
                return AddMenuItem(parameters);
            case "updateitem":
                return UpdateMenuItem(parameters);
            case "deleteitem":
                return DeleteMenuItem(parameters);
            default:
                return "Admin: Unknown action";
        }
    }


    private static string AddMenuItem(string parameters)
    {
        string[] paramParts = parameters.Split(';');
        if (paramParts.Length < 3)
        {
            return "Admin: Invalid parameters for adding item";
        }

        string name = paramParts[0];
        decimal price;
        bool availabilityStatus;

        if (!decimal.TryParse(paramParts[1], out price) || !bool.TryParse(paramParts[2], out availabilityStatus))
        {
            return "Admin: Invalid parameters for adding item";
        }

        string connectionString = "Server=localhost;Database=cafeteriarecommenderdb;User ID=root;Password=ronak5840R!;Port=3306";
        try
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT INTO Items (Name, Price, AvailabilityStatus) VALUES (@Name, @Price, @AvailabilityStatus)";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", name);
                    cmd.Parameters.AddWithValue("@Price", price);
                    cmd.Parameters.AddWithValue("@AvailabilityStatus", availabilityStatus);
                    cmd.ExecuteNonQuery();
                    return "Admin: Item added successfully";
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Database exception: " + ex.Message);
            return "Admin: Failed to add item";
        }
    }

    private static string UpdateMenuItem(string parameters)
    {
        string[] paramParts = parameters.Split(';');
        if (paramParts.Length < 4)
        {
            return "Admin: Invalid parameters for updating item";
        }

        int itemId;
        string name = paramParts[1];
        decimal price;
        bool availabilityStatus;

        if (!int.TryParse(paramParts[0], out itemId) || !decimal.TryParse(paramParts[2], out price) || !bool.TryParse(paramParts[3], out availabilityStatus))
        {
            return "Admin: Invalid parameters for updating item";
        }

        string connectionString = "Server=localhost;Database=cafeteriarecommenderdb;User ID=root;Password=ronak5840R!;Port=3306";
        try
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "UPDATE Items SET Name = @Name, Price = @Price, AvailabilityStatus = @AvailabilityStatus WHERE ItemId = @ItemId";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ItemId", itemId);
                    cmd.Parameters.AddWithValue("@Name", name);
                    cmd.Parameters.AddWithValue("@Price", price);
                    cmd.Parameters.AddWithValue("@AvailabilityStatus", availabilityStatus);
                    cmd.ExecuteNonQuery();
                    return "Admin: Item updated successfully";
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Database exception: " + ex.Message);
            return "Admin: Failed to update item";
        }
    }

    private static string DeleteMenuItem(string parameters)
    {
        int itemId;

        if (!int.TryParse(parameters, out itemId))
        {
            return "Admin: Invalid item ID for deletion";
        }

        string connectionString = "Server=localhost;Database=cafeteriarecommenderdb;User ID=root;Password=ronak5840R!;Port=3306";
        try
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM Items WHERE ItemId = @ItemId";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ItemId", itemId);
                    cmd.ExecuteNonQuery();
                    return "Admin: Item deleted successfully";
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Database exception: " + ex.Message);
            return "Admin: Failed to delete item";
        }
    }

    private static string ChefFunctionality(string action, string parameters)
    {
        switch (action.ToLower())
        {
            case "recommenditem":
                return "Chef: Recommend item functionality executed";
            case "viewfeedback":
                return "Chef: View feedback functionality executed";
            default:
                return "Chef: Unknown action";
        }
    }

    private static string EmployeeFunctionality(string action, string parameters)
    {
        switch (action.ToLower())
        {
            case "viewmenu":
                return ViewMenu(parameters);
            case "givefeedback":
                string[] feedbackParams = parameters.Split(';');
                return GiveFeedback(parameters);
            default:
                return "Employee: Unknown action";
        }
    }

    private static string ViewMenu(string mealType)
    {
        string connectionString = "Server=localhost;Database=cafeteriarecommenderdb;User ID=root;Password=ronak5840R!;Port=3306";

        try
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT ItemId, Name, Price, AvailabilityStatus FROM Items i JOIN Meal_Type mt ON i.MealTypeId = mt.meal_type_id WHERE mt.type = @MealType";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@MealType", mealType);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        StringBuilder menu = new StringBuilder();
                        while (reader.Read())
                        {
                            menu.Append($"Item ID: {reader.GetInt32("ItemId")}, Item: {reader.GetString("Name")}, Price: {reader.GetDecimal("Price")}, Available: {reader.GetBoolean("AvailabilityStatus")}\n");
                        }
                        return menu.ToString();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Database exception: " + ex.Message);
            return "Failed to retrieve menu.";
        }
    }

    private static string GiveFeedback(string parameters)
    {
        string connectionString = "Server=localhost;Database=cafeteriarecommenderdb;User ID=root;Password=ronak5840R!;Port=3306";
        try
        {
            string[] feedbackParams = parameters.Split(';');
            int userId = int.Parse(feedbackParams[0]);
            int itemId = int.Parse(feedbackParams[1]);
            string comment = feedbackParams[2];
            int rating = int.Parse(feedbackParams[3]);

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT INTO Feedback (UserId, ItemId, Comment, Rating, FeedbackDate) VALUES (@UserId, @ItemId, @Comment, @Rating, NOW())";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@ItemId", itemId);
                    cmd.Parameters.AddWithValue("@Comment", comment);
                    cmd.Parameters.AddWithValue("@Rating", rating);
                    cmd.ExecuteNonQuery();
                    return "Feedback submitted successfully.";
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Database exception: " + ex.Message);
            return "Failed to submit feedback.";
        }
    }

    private class LoginResult
    {
        public bool Success { get; set; }
        public int UserId { get; set; }
        public string Role { get; set; }
    }
}
