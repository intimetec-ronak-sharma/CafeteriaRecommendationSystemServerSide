using MySql.Data.MySqlClient;
using System;
using System.Text;
using System.IO;

namespace CafeteriaRecommendationSystem.Services
{
    internal class ChefService
    {
        public static string ExecuteChefAction(string action, string parameters)
        {
            switch (action.ToLower())
            {
                case "recommenditem":
                    return RecommendItem(parameters);
                case "viewfeedback":
                    return ViewFeedback();
                case "viewemployeevote":
                    return ViewEmployeeVotes();
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
                case "discardmenuitems":
                    return DiscardMenuItemList();
                case "removefooditem":
                    return RemoveFoodItem(parameters);
                case "insertfeedbacknotification":
                    return InsertFeedbackNotification(parameters);
                default:
                    return "Invalid action specified.";
            }
        }

        public static string RecommendItem(string parameters)
        {
            string[] paramParts = parameters.Split(';');
            if (paramParts.Length == 2 && int.TryParse(paramParts[1], out int size))
            {
                return GetRecommendedItemsForNextDay(paramParts[0], size);
            }
            return "Invalid parameters for recommenditem.";
        }

        public static string DiscardMenuItemList()
        {
            try
            {
                using (var connection = DatabaseUtility.GetConnection())
                {
                    connection.Open();
                    var negativeWords = File.ReadAllLines(@"C:\Users\ronak.sharma\source\repos\CafeteriaRecommendationSystem\CafeteriaRecommendationSystem\Data\negative_words.txt");
                    var likeClauses = new StringBuilder();
                    foreach (var word in negativeWords)
                    {
                        if (likeClauses.Length > 0)
                        {
                            likeClauses.Append(" OR ");
                        }
                        likeClauses.Append($"s.CommentSentiments LIKE '%{word}%'");
                    }

                    string query = "SELECT i.ItemId, i.Name, s.OverallRating, s.CommentSentiments FROM Item i INNER JOIN Sentiment s ON i.ItemId = s.ItemId WHERE s.OverallRating < 2 AND (" + likeClauses.ToString() + ")";

                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                return "No items to discard.";
                            }

                            var result = new StringBuilder();
                            result.AppendLine("\nDiscard Menu Item List");
                            result.AppendLine("------------------------------------------------------------------------------");
                            result.AppendLine($"{"ItemId",-10} {"Name",-25} {"OverallRating",-15} {"CommentSentiments"}");
                            result.AppendLine("------------------------------------------------------------------------------");

                            while (reader.Read())
                            {
                                result.AppendLine(
                                $"{reader.GetInt32("ItemId"),-10} " +
                                $"{reader.GetString("Name"),-25} " +
                                $"{reader.GetFloat("OverallRating"),-15} " +
                                $"{reader.GetString("CommentSentiments")}");
                            }

                            result.AppendLine();
                            return result.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return "Error retrieving discard menu items: " + ex.Message;
            }
        }

        public static string RemoveFoodItem(string itemId)
        {
            if (DateTime.Now.Day != 1)
            {
                return "Food items can only be removed on the first day of the month.";
            }
            try
            {
                string itemName = SqlService.GetItemNameById(itemId);
                if (itemName == null)
                {
                    return $"Item with ID '{itemId}' not found.";
                }
                using (var connection = DatabaseUtility.GetConnection())
                {
                    connection.Open();
                    int rowsAffected = SqlService.DeleteItemByName(connection, itemName);
                    return rowsAffected > 0 ? $"Successfully removed item: {itemName}" : $"Item '{itemName}' not found.";
                }
            }
            catch (Exception ex)
            {
                return "Error removing item: " + ex.Message;
            }
        }
        public static string InsertFeedbackNotification(string itemId)
        {
            try
            {
                using (var connection = DatabaseUtility.GetConnection())
                {
                    connection.Open();
                    string itemName = SqlService.GetItemNameById(itemId);
                    if (itemName == null)
                    {
                        return $"Item with ID '{itemId}' not found.";
                    }
                    string notificationMessage = $"We are trying to improve your experience with {itemName}. Please provide your feedback and help us";
                    SqlService.InsertNotification(connection, notificationMessage);

                    return "Feedback notification inserted successfully.";
                }
            }
            catch (Exception ex)
            {
                return "Error inserting feedback into notification table: " + ex.Message;
            }
        }
        public static string ViewEmployeeVotes()
        {
            try
            {
                using (var connection = DatabaseUtility.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT EmployeeVoteId, ItemId, VoteTime, VoteCount FROM EmployeeVote";

                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                var result = new StringBuilder();
                                result.AppendLine("\nEmployee Votes:");
                                result.AppendLine("-------------------------------------------------------------");
                                result.AppendLine($"{"EmployeeVoteId",-18} {"ItemId",-8} {"VoteTime",-25} {"VoteCount",-10}");
                                result.AppendLine("-------------------------------------------------------------");

                                while (reader.Read())
                                {
                                    result.AppendLine(
                                        $"{reader.GetInt32("EmployeeVoteId"),-18} " +
                                        $"{reader.GetInt32("ItemId"),-8} " +
                                        $"{reader.GetDateTime("VoteTime"),-25} " +
                                        $"{reader.GetInt32("VoteCount"),-10}");
                                }
                                return result.ToString();
                            }
                            else
                            {
                                return "No employee votes found.";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return "Failed to retrieve employee votes.";
            }
        }

        public static string InsertChefRecommendation(int itemId)
        {
            try
            {
                using (var connection = DatabaseUtility.GetConnection())
                {
                    connection.Open();
                    int sentimentId = SqlService.GetSentimentId(itemId);
                    if (sentimentId == -1)
                    {
                        return "No sentiment comments are available for this particular ItemID";
                    }

                    if (SqlService.IsItemRecommended(connection, itemId))
                    {
                        return "Item is already recommended.";
                    }
                    if (SqlService.InsertRecommendation(connection, itemId, sentimentId))
                    {
                        string notificationMessage = $"Chef Roll out today Menu";
                        SqlService.InsertNotification(connection, notificationMessage);
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

        public static string GetRecommendedItemsForNextDay(string menuType, int returnItemListSize)
        {
            var recommendedItems = new StringBuilder();
            string query = SqlService.GetRecommendedItems();

            try
            {
                using (var conn = DatabaseUtility.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@MenuType", menuType);
                        cmd.Parameters.AddWithValue("@ReturnItemListSize", returnItemListSize);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                recommendedItems.AppendLine("\nRecommended Items:");
                                recommendedItems.AppendLine("------------------------------------------------------");
                                recommendedItems.AppendLine($"{"ItemId",-10} {"Rating",-10} {"Sentiment Score",-18} {"Votes",-10}");
                                recommendedItems.AppendLine("------------------------------------------------------");

                                while (reader.Read())
                                {
                                    recommendedItems.AppendLine(
                                    $"{reader.GetInt32("ItemId"),-10} " +
                                    $"{reader.GetFloat("OverallRating"),-10:f2} " +
                                    $"{reader.GetFloat("SentimentScore"),-18:f2} " +
                                    $"{reader.GetInt32("VoteCount"),-10}");
                                }
                            }
                            else
                            {
                                recommendedItems.AppendLine("No recommended items found.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return "Failed to retrieve recommended items.";
            }

            return recommendedItems.ToString();
        }

        public static string ViewMenuItems()
        {
            try
            {
                using (var connection = DatabaseUtility.GetConnection())
                {
                    connection.Open();
                    string query = SqlService.ViewMenuItemsQuery();
                    using (var command = new MySqlCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                StringBuilder result = new StringBuilder();
                                result.AppendLine("\nItems List:");
                                result.AppendLine("------------------------------------------------------------------------------------------------------------------------------");
                                result.AppendLine($"{"ID",-5} {"Name",-20} {"Price",-12} {"Availability",-15} {"Meal Type",-12} {"Diet Preference",-20} {"Spice Level",-12} {"Food Preference",-20} {"Sweet Tooth",-12}");
                                result.AppendLine("------------------------------------------------------------------------------------------------------------------------------");

                                while (reader.Read())
                                {
                                    result.AppendLine(
                                        $"{reader.GetInt32("ItemId"),-5} " +
                                        $"{reader.GetString("Name"),-20} " +
                                        $"Rs. {reader.GetDecimal("Price"),-10:f2} " +
                                        $"{(reader.GetBoolean("AvailabilityStatus") ? "True" : "False"),-15} " +
                                        $"{(reader.IsDBNull(reader.GetOrdinal("MealType")) ? "N/A" : reader.GetString("MealType")),-12}" +
                                        $"{(reader.IsDBNull(reader.GetOrdinal("DietPreference")) ? "N/A" : reader.GetString("DietPreference")),-20} " +
                                        $"{(reader.IsDBNull(reader.GetOrdinal("SpiceLevel")) ? "N/A" : reader.GetString("SpiceLevel")),-12} " +
                                        $"{(reader.IsDBNull(reader.GetOrdinal("FoodPreference")) ? "N/A" : reader.GetString("FoodPreference")),-20} " +
                                        $"{(reader.IsDBNull(reader.GetOrdinal("SweetTooth")) ? "N/A" : reader.GetString("SweetTooth")),-12}");
                            }
                                result.AppendLine("-------------------------------------------------------------------------------------------------------------------------------\n");
                                return result.ToString();
                            }
                            else
                            {
                                return "No items found";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Database exception: " + ex.Message);
                return "Failed to retrieve items";
            }
        }

        public static string ViewFeedback()
        {
            try
            {
                using (var connection = DatabaseUtility.GetConnection())
                {
                    connection.Open();
                    string query = SqlService.ViewFeedback();
                    using (var command = new MySqlCommand(query, connection))
                    {
                        StringBuilder feedbackList = new StringBuilder();
                        feedbackList.AppendLine("\nLast One Day Feedback:");
                        feedbackList.AppendLine("---------------------------------------------------------------------------------------------------------------------------");
                        feedbackList.AppendLine($"{"Feedback ID",-12} {"Item",-15} {"Comment",-50} {"Rating",-10} {"Date",-30}");
                        feedbackList.AppendLine("---------------------------------------------------------------------------------------------------------------------------");

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                feedbackList.AppendLine($"{reader.GetInt32("FeedbackId"),-12} " +
                                                $"{reader.GetString("ItemName"),-15} " +
                                                $"{reader.GetString("Comment"),-50} " +
                                                $"{reader.GetInt32("Rating"),-10} " +
                                                $"{reader.GetDateTime("FeedbackDate"),-30}");
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