using MySql.Data.MySqlClient;
using CafeteriaRecommendationSystem.Models;
using System;
using System.IO;
using System.Text;

namespace CafeteriaRecommendationSystem.Services
{
    internal class SqlService
    {
        public static MySqlConnection GetOpenConnection()
        {
            var connection = DatabaseUtility.GetConnection();
            connection.Open();
            return connection;
        }

        public static MySqlDataReader ExecuteReader(string query, MySqlConnection connection)
        {
            using (MySqlCommand cmd = new MySqlCommand(query, connection))
            {
                return cmd.ExecuteReader();
            }
        }

        public static int InsertMenuItem(MySqlConnection connection, Item item)
        {
            string query = "INSERT INTO Item (Name, Price, AvailabilityStatus, MealTypeId, DietPreference, SpiceLevel, FoodPreference, SweetTooth)" +
                        "VALUES (@Name, @Price, @AvailabilityStatus,@MealTypeId, @DietPreference, @SpiceLevel, @FoodPreference, @SweetTooth)";
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Name", item.Name);
                command.Parameters.AddWithValue("@Price", item.Price);
                command.Parameters.AddWithValue("@AvailabilityStatus", item.AvailabilityStatus);
                command.Parameters.AddWithValue("@MealTypeId", item.MealTypeId);
                command.Parameters.AddWithValue("@DietPreference", item.DietPreference);
                command.Parameters.AddWithValue("@SpiceLevel", item.SpiceLevel);
                command.Parameters.AddWithValue("@FoodPreference", item.FoodPreference);
                command.Parameters.AddWithValue("@SweetTooth", item.SweetTooth);
                command.ExecuteNonQuery();
                return (int)command.LastInsertedId;
            }
        }

        public static void InsertUserPreference(MySqlConnection connection, int userId, Item item)
        {
            string insertQuery = "INSERT INTO UserPreference (UserId, DietPreference, SpiceLevel, FoodPreference, SweetTooth) VALUES (@UserId, @DietPreference, @SpiceLevel, @FoodPreference, @SweetTooth)";
            using (MySqlCommand insertCommand = new MySqlCommand(insertQuery, connection))
            {
                insertCommand.Parameters.AddWithValue("@UserId", userId);
                insertCommand.Parameters.AddWithValue("@DietPreference", item.DietPreference);
                insertCommand.Parameters.AddWithValue("@SpiceLevel", item.SpiceLevel);
                insertCommand.Parameters.AddWithValue("@FoodPreference", item.FoodPreference);
                insertCommand.Parameters.AddWithValue("@SweetTooth", item.SweetTooth);
                insertCommand.ExecuteNonQuery();
            }
        }

        public static void UpdateUserPreference(MySqlConnection connection, int userId, Item item)
        {
            string updateQuery = "UPDATE UserPreference SET DietPreference = @DietPreference, SpiceLevel = @SpiceLevel, FoodPreference = @FoodPreference, SweetTooth = @SweetTooth WHERE UserId = @UserId";
            using (MySqlCommand updateCommand = new MySqlCommand(updateQuery, connection))
            {
                updateCommand.Parameters.AddWithValue("@UserId", userId);
                updateCommand.Parameters.AddWithValue("@DietPreference", item.DietPreference);
                updateCommand.Parameters.AddWithValue("@SpiceLevel", item.SpiceLevel);
                updateCommand.Parameters.AddWithValue("@FoodPreference", item.FoodPreference);
                updateCommand.Parameters.AddWithValue("@SweetTooth", item.SweetTooth);
                updateCommand.ExecuteNonQuery();
            }
        }

        public static void InsertNotification(MySqlConnection connection, string message)
        {
            string notificationQuery = "INSERT INTO Notification (Message, NotificationDate) VALUES (@Message, @NotificationDate)";
            using (MySqlCommand command = new MySqlCommand(notificationQuery, connection))
            {
                command.Parameters.AddWithValue("@Message", message);
                command.Parameters.AddWithValue("@NotificationDate", DateTime.Now);
                command.ExecuteNonQuery();
            }
        }

        public static void UpdateMenuItem(MySqlConnection connection, int itemId, decimal price, bool availabilityStatus)
        {
            string query = "UPDATE Item SET Price = @Price, AvailabilityStatus = @AvailabilityStatus WHERE ItemId = @ItemId";
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@ItemId", itemId);
                command.Parameters.AddWithValue("@Price", price);
                command.Parameters.AddWithValue("@AvailabilityStatus", availabilityStatus);
                command.ExecuteNonQuery();
            }
        }

        public static int GetMealTypeId(MySqlConnection connection, string mealType)
        {
            string query = "SELECT meal_type_id FROM MealType WHERE MealType = @Type";
            using (MySqlCommand cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@Type", mealType);
                object result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : -1;
            }
        }


        public static bool CheckIfItemExists(MySqlConnection connection, int itemId)
        {
            string query = "SELECT COUNT(*) FROM Item WHERE ItemId = @ItemId";
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@ItemId", itemId);
                long count = (long)command.ExecuteScalar();
                return count > 0;
            }
        }

        public static void DeleteMenuItem(MySqlConnection connection, int itemId)
        {
            string query = "DELETE FROM Item WHERE ItemId = @ItemId";
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@ItemId", itemId);
                command.ExecuteNonQuery();
            }
        }

        public static string ViewMenuItemsQuery()
        {
            string query = "SELECT i.ItemId, i.Name, i.Price, i.AvailabilityStatus, i.DietPreference, i.SpiceLevel, i.FoodPreference, i.SweetTooth, m.MealType AS MealType " +
                   "FROM Item i " +
                   "INNER JOIN MealType m ON i.MealTypeId = m.meal_type_id " +
                   "ORDER BY i.ItemId";
            return query;
        }

        public static string GetRecommendedItems()
        {
            string query = "SELECT s.ItemId, s.OverallRating, s.SentimentScore, s.VoteCount FROM Sentiment s JOIN Item i ON s.ItemId = i.ItemId " +
                           "JOIN MealType m ON i.MealTypeId = m.meal_type_id " +
                           "WHERE m.MealType = @MenuType " +
                           "ORDER BY s.OverallRating DESC, s.SentimentScore DESC, s.VoteCount DESC " +
                           "LIMIT @ReturnItemListSize";
            return query;
        }

        public static string DiscardMenuItemListQuery(string negativeWordsFilePath)
        {
            var negativeWords = File.ReadAllLines(negativeWordsFilePath);

            var likeClauses = new StringBuilder();
            foreach (var word in negativeWords)
            {
                if (likeClauses.Length > 0)
                {
                    likeClauses.Append(" OR ");
                }
                likeClauses.Append($"s.CommentSentiments LIKE '%{word}%'");
            }
            return "SELECT i.ItemId, i.Name, s.OverallRating, s.CommentSentiments " +
                   "FROM Item i " +
                   "INNER JOIN Sentiment s ON i.ItemId = s.ItemId " +
                   $"WHERE s.OverallRating < 2 AND ({likeClauses.ToString()})";
        }

        public static string GetRecommendedItemsForUser()
        {
            string query = @"
                SELECT r.RecommendationId, r.ItemId, i.Name, i.Price, i.AvailabilityStatus, 
                s.OverallRating, s.OverallCommentSentiment, 
                i.DietPreference, i.SpiceLevel, i.FoodPreference, i.SweetTooth
                FROM Recommendation r
                JOIN Item i ON r.ItemId = i.ItemId
                LEFT JOIN Sentiment s ON i.ItemId = s.ItemId
                JOIN MealType mt ON i.MealTypeId = mt.meal_type_id
                JOIN UserPreference up ON up.UserId = @UserId
                WHERE mt.MealType = @MealType
                ORDER BY 
                CASE WHEN up.DietPreference = i.DietPreference THEN 1 ELSE 0 END DESC, 
                CASE WHEN up.SpiceLevel = i.SpiceLevel THEN 1 ELSE 0 END DESC, 
                CASE WHEN up.FoodPreference = i.FoodPreference THEN 1 ELSE 0 END DESC, 
                CASE WHEN up.SweetTooth = i.SweetTooth THEN 1 ELSE 0 END DESC";
            return query;
        }

        public static void InsertFeedback(MySqlConnection connection, int userId, int itemId, string comment, int rating)
        {
            const string query = "INSERT INTO Feedback (UserId, ItemId, Comment, Rating, FeedbackDate) VALUES (@UserId, @ItemId, @Comment, @Rating, NOW())";
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@ItemId", itemId);
                cmd.Parameters.AddWithValue("@Comment", comment);
                cmd.Parameters.AddWithValue("@Rating", rating);
                cmd.ExecuteNonQuery();
            }
        }

        public static int UpdateVoteCount(MySqlConnection connection, int itemId)
        {
            const string query = "SELECT VoteCount FROM Sentiment WHERE ItemId = @ItemId";
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@ItemId", itemId);
                var result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value ? Convert.ToInt32(result) + 1 : 1;
            }
        }

        public static int UpdateVoteCountValue(MySqlConnection connection, int itemId)
        {
            const string query = "UPDATE EmployeeVote SET VoteCount = VoteCount + 1 WHERE ItemId = @ItemId";
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@ItemId", itemId);
                return cmd.ExecuteNonQuery();
            }
        }

        public static void InsertNewSentimentData(MySqlConnection connection, int itemId, int rating, string sentimentComment, float sentimentScore, string commentSentiments, int voteCount)
        {
            const string query = "INSERT INTO Sentiment (ItemId, OverallRating, OverallCommentSentiment, SentimentScore, VoteCount, CommentSentiments) VALUES (@ItemId, @OverallRating, @OverallCommentSentiment, @SentimentScore, @VoteCount, @CommentSentiments)";
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@ItemId", itemId);
                cmd.Parameters.AddWithValue("@OverallRating", rating);
                cmd.Parameters.AddWithValue("@OverallCommentSentiment", sentimentComment);
                cmd.Parameters.AddWithValue("@SentimentScore", sentimentScore);
                cmd.Parameters.AddWithValue("@CommentSentiments", commentSentiments);
                cmd.Parameters.AddWithValue("@VoteCount", voteCount);
                cmd.ExecuteNonQuery();
            }
        }

        public static (int sentimentId, double existingOverallRating, double existingSentimentScore, string existingcommentSentiments)? GetSentimentData(MySqlConnection connection, int itemId)
        {
            const string query = "SELECT SentimentId, OverallRating, SentimentScore, CommentSentiments FROM Sentiment WHERE ItemId = @ItemId";
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@ItemId", itemId);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int sentimentId = reader.GetInt32("SentimentId");
                        double existingOverallRating = reader["OverallRating"] != DBNull.Value ? Convert.ToDouble(reader["OverallRating"]) : 0.0;
                        double existingSentimentScore = reader["SentimentScore"] != DBNull.Value ? Convert.ToDouble(reader["SentimentScore"]) : 0.0;
                        string existingCommentSentiments = reader["CommentSentiments"] != DBNull.Value ? reader["CommentSentiments"].ToString() : string.Empty;
                        return (sentimentId, existingOverallRating, existingSentimentScore, existingCommentSentiments);
                    }
                }
            }
            return null;
        }

        public static string ViewFeedback()
        {
            string query = "SELECT f.FeedbackId, f.UserId, f.ItemId, i.Name AS ItemName, f.Comment, f.Rating, f.FeedbackDate " +
                                   "FROM Feedback f " +
                                   "JOIN User u ON f.UserId = u.UserId " +
                                   "JOIN Item i ON f.ItemId = i.ItemId " +
                                   "WHERE f.FeedbackDate >= DATE_SUB(NOW(), INTERVAL 1 DAY)";
            return query;        
        }

        public static int GetSentimentId(int itemId)
        {
            using (var connection = DatabaseUtility.GetConnection())
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

        public static bool InsertRecommendation(MySqlConnection connection, int itemId, int sentimentId)
        {
            string query = "INSERT INTO Recommendation (ItemId, SentimentId) VALUES (@ItemId, @SentimentId)";
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@ItemId", itemId);
                cmd.Parameters.AddWithValue("@SentimentId", sentimentId);
                int rowsAffected = cmd.ExecuteNonQuery();
                return rowsAffected > 0;
            }
        }

        public static bool IsItemRecommended(MySqlConnection connection, int itemId)
        {
            string query = "SELECT COUNT(*) FROM Recommendation WHERE ItemId = @ItemId";
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@ItemId", itemId);
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return count > 0;
            }
        }
        public static void UpdateSentiment(MySqlConnection connection, Sentiment sentiment)
        {
            string query = "UPDATE Sentiment SET OverallRating = @OverallRating, OverallCommentSentiment = @OverallCommentSentiment, SentimentScore = @SentimentScore, VoteCount = @VoteCount, CommentSentiments = @CommentSentiments WHERE ItemId = @ItemId";

            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@OverallRating", sentiment.OverallRating);
                cmd.Parameters.AddWithValue("@OverallCommentSentiment",sentiment.OverallCommentSentiment);
                cmd.Parameters.AddWithValue("@SentimentScore", sentiment.SentimentScore);
                cmd.Parameters.AddWithValue("@ItemId",sentiment.ItemId);
                cmd.Parameters.AddWithValue("@VoteCount",sentiment.VoteCount);
                cmd.Parameters.AddWithValue("@CommentSentiments",sentiment.CommentSentiments);
                cmd.ExecuteNonQuery();
            }
        }

        public static bool IsItemInMenu(MySqlConnection connection, int itemId)
        {
            const string query = "SELECT COUNT(*) FROM Item WHERE ItemId = @ItemId";
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@ItemId", itemId);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        public static DateTime? GetLastVoteTime(MySqlConnection connection, int userId)
        {
            const string query = "SELECT VoteTime FROM EmployeeVote WHERE UserId = @UserId AND VoteTime > NOW() - INTERVAL 24 HOUR";
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                return cmd.ExecuteScalar() as DateTime?;
            }
        }

        public static int GetVoteCount(MySqlConnection connection, int itemId)
        {
            const string query = "SELECT COUNT(*) FROM EmployeeVote WHERE ItemId = @ItemId";
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@ItemId", itemId);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public static int InsertVote(MySqlConnection connection, int itemId, int userId)
        {
            const string query = "INSERT INTO EmployeeVote (ItemId, UserId, VoteTime, VoteCount) VALUES (@ItemId, @UserId, CURRENT_TIMESTAMP, 1)";
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@ItemId", itemId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                return cmd.ExecuteNonQuery();
            }
        }

        public static int DeleteItemByName(MySqlConnection connection, string itemName)
        {
            const string query = "DELETE FROM Item WHERE Name = @ItemName";
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@ItemName", itemName);
                return cmd.ExecuteNonQuery();
            }
        }

        public static string GetItemNameById(string itemId)
        {
            try
            {
                using (var connection = DatabaseUtility.GetConnection())
                {
                    connection.Open();
                    string selectQuery = "SELECT Name FROM Item WHERE ItemId = @ItemId";
                    using (var cmd = new MySqlCommand(selectQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@ItemId", itemId);
                        object result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            return result.ToString();
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Error retrieving item name: {ex.Message}";
            }
        }
    }
}
