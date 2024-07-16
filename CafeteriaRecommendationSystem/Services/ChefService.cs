using MySql.Data.MySqlClient;
using System;
using System.Text;

namespace CafeteriaRecommendationSystem.Services
{
    internal class ChefService
    {
        public static string ChefFunctionality(string action, string parameters)
        {
            switch (action.ToLower())
            {
                case "recommenditem":
                    return "Chef: Recommend item functionality executed";
                case "viewfeedback":
                    return ViewFeedback();
                case "viewmenuitem":
                    return ViewMenuItems();
                default:
                    return "Please enter a valid option.";
            }
        }
    public static string ViewMenuItems()
    {
        try
        {
            using (MySqlConnection connection = DatabaseUtility.GetConnection())
            {
                connection.Open();
                string query = "SELECT i.ItemId, i.Name, i.Price, i.AvailabilityStatus, m.type AS MealType " +
                           "FROM Items i " +
                           "INNER JOIN meal_type m ON i.MealTypeId = m.meal_type_id " +
                           "ORDER BY i.ItemId";

                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
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

                    using (MySqlCommand command = new MySqlCommand(query, conn))
                    {
                        StringBuilder feedbackList = new StringBuilder();
                        Console.WriteLine("\nLast one Day Feedback is: ");
                        using (MySqlDataReader reader = command.ExecuteReader())
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

    }

}

