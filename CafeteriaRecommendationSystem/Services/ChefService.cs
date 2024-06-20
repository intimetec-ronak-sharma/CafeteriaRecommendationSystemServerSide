using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
                    string[] paramParts = parameters.Split(';');
                    if (paramParts.Length == 2)
                    {
                        string menuType = paramParts[0];
                        if (int.TryParse(paramParts[1], out int size))
                        {
                            return recEngineGetFoodItemForNextDay(menuType, size);
                        }
                        else
                        {
                            return "Invalid size parameter.";
                        }
                    }
                    else
                    {
                        return "Invalid parameters for recommenditem.";
                    }
                case "viewfeedback":
                    return ViewFeedback();
                case "viewmenuitem":
                    return ViewMenuItems();
                case "rolloutmenu":
                    if (int.TryParse(parameters, out int itemId))
                    {
                         return InsertChefRecommendation(itemId);
                    }
                    else
                    {
                        return "Invalid Item ID.";
                    }
                default:
                    return "Please enter a valid option.";
            }
        }

        public static string InsertChefRecommendation(int itemId)
        {
            try
            {
                int sentimentId = GetSentimentId(itemId);

                if (sentimentId == -1)
                {
                    return "Failed to retrieve sentimentId for itemId.";
                }

                using (MySqlConnection connection = DatabaseUtility.GetConnection())
                {
                    connection.Open();
                    string insertQuery = @"INSERT INTO Recommendation (ItemId, SentimentId)
                                   VALUES (@ItemId, @SentimentId)";
                    MySqlCommand command = new MySqlCommand(insertQuery, connection);
                    command.Parameters.AddWithValue("@ItemId", itemId);
                    command.Parameters.AddWithValue("@SentimentId", sentimentId);

                    int rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        return "Recommendation inserted successfully.";
                    }
                    else
                    {
                        return "Failed to insert recommendation.";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occurred: " + ex.Message);
                return "An error occurred while inserting recommendation.";
            }
        }

        public static int GetSentimentId(int itemId)
        {
            try
            {
                using (MySqlConnection connection = DatabaseUtility.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT SentimentId FROM Sentiment WHERE ItemId = @ItemId";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@ItemId", itemId);

                    object result = command.ExecuteScalar();
                    if (result != null)
                    {
                        return Convert.ToInt32(result);
                    }
                    else
                    {
                        return -1; 
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occurred: " + ex.Message);
                return -1; 
            }
        }

        public static string recEngineGetFoodItemForNextDay(string menuType,int returnItemListSize)
        {
            List<string> recommendedItems = new List<string>();

            string query = $@"
            SELECT s.ItemId, s.OverallRating, s.SentimentScore, s.VoteCount
            FROM Sentiment s
            JOIN Item i ON s.ItemId = i.ItemId
            JOIN MealType m ON i.MealTypeId = m.meal_type_id
            WHERE m.Type = @MenuType
            ORDER BY s.OverallRating DESC, s.SentimentScore DESC, s.VoteCount DESC
            LIMIT {returnItemListSize}";


            using (MySqlConnection conn = DatabaseUtility.GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@MenuType", menuType);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int itemId = reader.GetInt32("ItemId");
                        float overallRating = reader.GetFloat("OverallRating");
                        float sentimentalScore = reader.GetFloat("SentimentScore");
                        int noOfVotes = reader.GetInt32("VoteCount");
                        recommendedItems.Add($"ItemId: {itemId}, Rating: {overallRating}, Sentiment Score: {sentimentalScore}, Votes: {noOfVotes}");
                    }
                }

            }
            return string.Join(Environment.NewLine, recommendedItems);
        }
        public static string ViewMenuItems()
        {
            try
            {
                using (MySqlConnection connection = DatabaseUtility.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT i.ItemId, i.Name, i.Price, i.AvailabilityStatus, m.type AS MealType " +
                               "FROM Item i " +
                               "INNER JOIN MealType m ON i.MealTypeId = m.meal_type_id " +
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
                                   "JOIN User u ON f.UserId = u.UserId " +
                                   "JOIN Item i ON f.ItemId = i.ItemId " +
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

