using MySql.Data.MySqlClient;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Transactions;
using System.Threading.Tasks;

namespace CafeteriaRecommendationSystem
{
    class Server
    {
        public static LoginResult LoginUser(string email)
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
            return new LoginResult { Success = false, UserId = 0, Role = "" };
        }

        public static string ExecuteRoleBasedFunctionality(string role, string action, string parameters)
        {
            switch (role)
            {
                case "Admin":
                    return Services.AdminService.AdminFunctionality(action, parameters);
                case "Chef":
                    return Services.ChefService.ChefFunctionality(action, parameters);
                case "Employee":
                    return Services.EmployeeService.EmployeeFunctionality(action, parameters);
                default:
                    return "Please enter a valid option.";
            }
        }

        public static string AddMenuItem(string parameters)
        {
            string[] paramParts = parameters.Split(';');
            if (paramParts.Length < 4)
            {
                return "Admin: Invalid parameters for adding item";
            }

            string name = paramParts[0];
            decimal price;
            bool availabilityStatus;
            string mealType = paramParts[3];

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
                    int mealTypeId;
                    string getMealTypeIdQuery = "SELECT meal_type_id FROM meal_type WHERE type = @Type";
                    using (MySqlCommand getMealTypeIdCmd = new MySqlCommand(getMealTypeIdQuery, conn))
                    {
                        getMealTypeIdCmd.Parameters.AddWithValue("@Type", mealType);
                        object result = getMealTypeIdCmd.ExecuteScalar();
                        if (result == null)
                        {
                            return "Admin: Invalid meal type";
                        }
                        mealTypeId = Convert.ToInt32(result);
                    }
                    string query = "INSERT INTO Items (Name, Price, AvailabilityStatus, MealTypeId) VALUES (@Name, @Price, @AvailabilityStatus,@MealTypeId)";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@Price", price);
                        cmd.Parameters.AddWithValue("@AvailabilityStatus", availabilityStatus);
                        cmd.Parameters.AddWithValue("@MealTypeId", mealTypeId);
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

        public static string UpdateMenuItem(string parameters)
        {
            string[] paramParts = parameters.Split(';');
            if (paramParts.Length < 5)
            {
                return "Admin: Invalid parameters for updating item";
            }

            int itemId;
            string name = paramParts[1];
            decimal price;
            bool availabilityStatus;
            string mealType = paramParts[4];

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
                    int mealTypeId;
                    string getMealTypeIdQuery = "SELECT meal_type_id FROM meal_type WHERE type = @Type";
                    using (MySqlCommand getMealTypeIdCmd = new MySqlCommand(getMealTypeIdQuery, conn))
                    {
                        getMealTypeIdCmd.Parameters.AddWithValue("@Type", mealType);
                        object result = getMealTypeIdCmd.ExecuteScalar();
                        if (result == null)
                        {
                            return "Admin: Invalid meal type";
                        }
                        mealTypeId = Convert.ToInt32(result);
                    }

                    string query = "UPDATE Items SET Name = @Name, Price = @Price, AvailabilityStatus = @AvailabilityStatus, MealTypeId = @MealTypeId WHERE ItemId = @ItemId";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ItemId", itemId);
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@Price", price);
                        cmd.Parameters.AddWithValue("@AvailabilityStatus", availabilityStatus);
                        cmd.Parameters.AddWithValue("@MealTypeId", mealTypeId);
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

        public static string DeleteMenuItem(string parameters)
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
                    string checkQuery = "SELECT COUNT(*) FROM Items WHERE ItemId = @ItemId";
                    using (MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@ItemId", itemId);
                        long count = (long)checkCmd.ExecuteScalar();

                        if (count == 0)
                        {
                            return "Admin: Item ID not found";
                        }
                    }
                    string deleteQuery = "DELETE FROM Items WHERE ItemId = @ItemId";
                    using (MySqlCommand deleteCmd = new MySqlCommand(deleteQuery, conn))
                    {
                        deleteCmd.Parameters.AddWithValue("@ItemId", itemId);
                        deleteCmd.ExecuteNonQuery();
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

        public static string ViewMenuItems()
        {
            string connectionString = "Server=localhost;Database=cafeteriarecommenderdb;User ID=root;Password=ronak5840R!;Port=3306";
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT i.ItemId, i.Name, i.Price, i.AvailabilityStatus, m.type AS MealType " +
                               "FROM Items i " +
                               "INNER JOIN meal_type m ON i.MealTypeId = m.meal_type_id";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                StringBuilder result = new StringBuilder("\nItems List: \n");
                                while (reader.Read())
                                {
                                    result.AppendLine($"ID: {reader["ItemId"]}, Name: {reader["Name"]}, Price: {reader["Price"]}, Availability: {reader["AvailabilityStatus"]}, Meal Type: {reader["MealType"]}");
                                }
                                return result.ToString();
                            }
                            else
                            {
                                return "Admin: No items found";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Database exception: " + ex.Message);
                return "Admin: Failed to retrieve items";
            }
        }

        public static string ViewFeedback()
        {
            string connectionString = "Server=localhost;Database=cafeteriarecommenderdb;User ID=root;Password=ronak5840R!;Port=3306";

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT f.FeedbackId, f.UserId, f.ItemId, i.Name AS ItemName, f.Comment, f.Rating, f.FeedbackDate " +
                                   "FROM Feedback f " +
                                   "JOIN Users u ON f.UserId = u.UserId " +
                                   "JOIN Items i ON f.ItemId = i.ItemId " +
                                   "WHERE f.FeedbackDate >= DATE_SUB(NOW(), INTERVAL 1 DAY)";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        StringBuilder feedbackList = new StringBuilder();
                        Console.WriteLine("\nLast one Day Feedback is: ");
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                feedbackList.AppendLine($"\nFeedback ID: {reader.GetInt32("FeedbackId")}, Item: {reader.GetString("ItemName")}, Comment: {reader.GetString("Comment")}, Rating: {reader.GetInt32("Rating")}, Date: {reader.GetDateTime("FeedbackDate")}");
                            }
                        }
                        return feedbackList.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Database exception: " + ex.Message);
                return "Failed to retrieve recent feedback.";
            }
        }

        public static string ViewMenu(string mealType)
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
                                menu.Append($"\nItem ID: {reader.GetInt32("ItemId")}, Item: {reader.GetString("Name")}, Price: {reader.GetDecimal("Price")}, Available: {reader.GetBoolean("AvailabilityStatus")}");
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

        public const string RapidAPIKey = "90d85ce8fbmshc1c3263c7beaa4cp17b345jsn7328aca98f46";
        public const string RapidAPIHost = "text-sentiment-analyzer-api1.p.rapidapi.com";
        public static async Task<string> GiveFeedback(string parameters)
        {
            string connectionString = "Server=localhost;Database=cafeteriarecommenderdb;User ID=root;Password=ronak5840R!;Port=3306";

            try
            {
                string[] feedbackParams = parameters.Split(';');
                int userId = int.Parse(feedbackParams[0]);
                int itemId = int.Parse(feedbackParams[1]);
                string comment = feedbackParams[2];
                int rating = int.Parse(feedbackParams[3]);

                string keyword = await ExtractKeywordWithAPI(comment);
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    try
                    {
                        string feedbackQuery = "INSERT INTO Feedback (UserId, ItemId, Comment, Rating, FeedbackDate) VALUES (@UserId, @ItemId, @Comment, @Rating, NOW())";
                        using (MySqlCommand cmd = new MySqlCommand(feedbackQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@UserId", userId);
                            cmd.Parameters.AddWithValue("@ItemId", itemId);
                            cmd.Parameters.AddWithValue("@Comment", comment);
                            cmd.Parameters.AddWithValue("@Rating", rating);
                            cmd.ExecuteNonQuery();
                        }

                        string votedItemsQuery = "INSERT INTO voteditems (UserId, ItemId, VoteDate) VALUES (@UserId, @ItemId, NOW())";
                        using (MySqlCommand cmd = new MySqlCommand(votedItemsQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@UserId", userId);
                            cmd.Parameters.AddWithValue("@ItemId", itemId);
                            cmd.ExecuteNonQuery();
                        }
                        string keywordQuery = "INSERT INTO Sentiments (ItemId, OverallRating, SentimentComment) VALUES (@ItemId, @OverallRating, @SentimentComment)";
                        using (MySqlCommand cmd = new MySqlCommand(keywordQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@ItemId", itemId);
                            cmd.Parameters.AddWithValue("@OverallRating", rating); 
                            cmd.Parameters.AddWithValue("@SentimentComment", keyword); 
                            cmd.ExecuteNonQuery();
                        }
                        return "Feedback submitted successfully.";
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Database exception: " + ex.Message);
                        return "Failed to submit feedback.";
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Database exception: " + ex.Message);
                return "Failed to submit feedback.";
            }
        }

        private static async Task<string> ExtractKeywordWithAPI(string comment)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("x-rapidapi-key", RapidAPIKey);
                    client.DefaultRequestHeaders.Add("x-rapidapi-host", RapidAPIHost);

                    string endpoint = "https://text-sentiment-analyzer-api1.p.rapidapi.com/sentiment";
                    string body = $"{{\"text\":\"{comment}\",\"extractors\":\"entities,topics\"}}";

                    HttpResponseMessage response = await client.PostAsync(endpoint, new StringContent(body, Encoding.UTF8, "application/json"));
                    if (!response.IsSuccessStatusCode)
                    {

                        string errorResponseBody = await response.Content.ReadAsStringAsync();
                        Console.WriteLine("Error response: " + errorResponseBody);
                    }
                    response.EnsureSuccessStatusCode();

                    string responseBody = await response.Content.ReadAsStringAsync();
                    string keyword = ParseKeywordResponse(responseBody);

                    return keyword;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error calling keyword extraction API: " + ex.Message);
                return "";
            }
        }

        private static string ParseKeywordResponse(string responseBody)
        {
            try
            {
                JObject json = JObject.Parse(responseBody);
                string sentiment = json["sentiment"]?.ToString();
                return sentiment ?? "";
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error parsing keyword extraction response: " + ex.Message);
                return "";
            }
        }

        public class LoginResult
        {
            public bool Success { get; set; }
            public int UserId { get; set; }
            public string Role { get; set; }
        }
    }
}
