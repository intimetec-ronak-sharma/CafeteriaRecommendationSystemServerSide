using CafeteriaRecommendationSystem.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CafeteriaRecommendationSystem.Services
{
    internal class EmployeeService
    {
        public static string ExecuteEmployeeAction(string action, string parameters)
        {
            switch (action.ToLower())
            {
                case "viewmenu":
                    return ViewMenu(parameters);
                case "givefeedback":
                    return GiveFeedback(parameters);
                case "voteitem":
                    return GiveVoteForItem(parameters);
                case "updateprofile":
                    return UpdateUserProfile(parameters);
                default:
                    return "Employee: Unknown action";
            }
        }

        public static string UpdateUserProfile(string parameters)
        {
            try
            {
                string[] paramParts = parameters.Split(';');
                if (paramParts.Length < 5 || !int.TryParse(paramParts[0], out int userId))
                {
                    return "Invalid parameters for updating profile";
                }

                string dietPreference = paramParts[1];
                string spicePreference = paramParts[2];
                string foodPreference = paramParts[3];
                string sweetToothPreference = paramParts[4];

                using (var connection = DatabaseUtility.GetConnection())
                {
                    connection.Open();
                    Item item = new Item
                    {
                        DietPreference = dietPreference,
                        SpiceLevel = spicePreference,
                        FoodPreference = foodPreference,
                        SweetTooth = sweetToothPreference
                    };

                    string checkQuery = "SELECT COUNT(*) FROM UserPreference WHERE UserId = @UserId";
                    using (var checkCommand = new MySqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@UserId", userId);
                        int count = Convert.ToInt32(checkCommand.ExecuteScalar());
                        if (count == 0)
                        {
                            SqlService.InsertUserPreference(connection, userId,item);
                            return "Profile created successfully";
                        }
                        else
                        {
                            SqlService.UpdateUserPreference(connection, userId, item);
                                return "Profile updated successfully";
                            }
                        }
                    }
                }
            catch (Exception ex)
            {
                return "Failed to update profile";
            }
        }

        public static string ViewNotification()
        {
            var notifications = new StringBuilder();
            try
            {
                using (var connection = DatabaseUtility.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT Message FROM notification WHERE NotificationDate >= NOW() - INTERVAL 1 DAY";

                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                notifications.AppendLine(reader["Message"].ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                  return $"An error occurred: {ex.Message}";
            }
            return notifications.Length > 0 ? notifications.ToString() : "No new notifications.";
        }

        public static string GiveVoteForItem(string parameters)
        {
            try
            {
                string[] feedbackParams = parameters.Split(';');
                int userId = int.Parse(feedbackParams[0]);
                int itemId = int.Parse(feedbackParams[1]);
                using (var connection = DatabaseUtility.GetConnection())
                {
                    connection.Open();
                    if (!IsItemInMenu(connection, itemId))
                    {
                        return "Given ItemId is not available in the menu, Please enter a valid Item ID";
                    }

                    DateTime? lastVoteTime = SqlService.GetLastVoteTime(connection, userId);
                    if (lastVoteTime.HasValue)
                    {
                        return "You have already voted in the last 24 hours. You cannot vote again at this time.";
                    }

                    int existingCount = SqlService.GetVoteCount(connection, itemId);
                    if (existingCount > 0)
                    {
                        int updateResult = SqlService.UpdateVoteCountValue(connection, itemId);
                        return updateResult > 0 ? "Vote successfully recorded." : "Failed to update vote count.";
                    }
                    else
                    {
                        int insertResult = SqlService.InsertVote(connection, itemId, userId);
                        return insertResult > 0 ? "Vote successfully recorded." : "Failed to record vote.";
                    }
                }
            }
            catch (Exception ex)
            {
                return "Failed to record vote.";
            }
        }

        public static string ViewMenu(string parameters)
        {
            try
            {
                string[] paramParts = parameters.Split(';');
                int userId;
                string mealType = paramParts[1];
                if (!int.TryParse(paramParts[0], out userId))
                {
                    return "Invalid user ID";
                }
                using (var connection = DatabaseUtility.GetConnection())
                {
                    connection.Open();
                    string query = SqlService.GetRecommendedItemsForUser();

                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@MealType", mealType);
                        using (var reader = cmd.ExecuteReader())
                        {
                            var menuList = new StringBuilder();
                            menuList.AppendLine("\nMenu Items:");
                            menuList.AppendLine("---------------------------------------------------------------------------------------------------------------------------------------------");
                            menuList.AppendLine($"{"Item ID",-10} {"Name",-15} {"Price",-10} {"Availability",-15} {"Rating",-10} {"Sentiment Comment",-20} {"Diet Preference",-15} {"Spice Level",-10} {"Food Preference",-15} {"Sweet Tooth",-10}");
                            menuList.AppendLine("---------------------------------------------------------------------------------------------------------------------------------------------");

                            while (reader.Read())
                            {
                                int itemId = reader.GetInt32("ItemId");
                                string itemName = reader.GetString("Name");
                                decimal price = reader.GetDecimal("Price");
                                bool availabilityStatus = reader.GetBoolean("AvailabilityStatus");
                                float overallRating = reader.GetFloat("OverallRating");
                                string overallCommentSentiment = reader.IsDBNull(reader.GetOrdinal("OverallCommentSentiment")) ? string.Empty : reader.GetString("OverallCommentSentiment");
                                string dietPreference = reader.IsDBNull(reader.GetOrdinal("DietPreference")) ? string.Empty : reader.GetString("DietPreference");
                                string spiceLevel = reader.IsDBNull(reader.GetOrdinal("SpiceLevel")) ? string.Empty : reader.GetString("SpiceLevel");
                                string foodPreference = reader.IsDBNull(reader.GetOrdinal("FoodPreference")) ? string.Empty : reader.GetString("FoodPreference");
                                string sweetTooth = reader.IsDBNull(reader.GetOrdinal("SweetTooth")) ? string.Empty : reader.GetString("SweetTooth");

                                menuList.AppendLine($"{itemId,-10} {itemName,-15} Rs.{price,-10} {(availabilityStatus ? "Available" : "Not Available"),-15} {overallRating,-10} {overallCommentSentiment,-20} {dietPreference,-15} {spiceLevel,-10} {foodPreference,-15} {sweetTooth,-10}");
                            }
                            menuList.AppendLine("---------------------------------------------------------------------------------------------------------------------------------------------\n");
                            return menuList.ToString();
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

        public static bool IsItemInMenu(MySqlConnection connection, int itemId)
        {
            string query = "SELECT COUNT(*) FROM Recommendation WHERE ItemId = @ItemId";

            using (MySqlCommand cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@ItemId", itemId);
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return count > 0;
            }
        }

        public static string GiveFeedback(string parameters)
        {
            try
            {
                string[] feedbackParams = parameters.Split(';');
                int userId = int.Parse(feedbackParams[0]);
                int itemId = int.Parse(feedbackParams[1]);
                string comment = feedbackParams[2];
                int rating = int.Parse(feedbackParams[3]);
                
                using (var connection = DatabaseUtility.GetConnection())
                {
                    connection.Open();
                    if (!IsItemInMenu(connection, itemId))
                    {
                        return "Given ItemId is not available in the menu, Please enter a valid Item ID";
                    }

                    string feedbackCheckQuery = "SELECT FeedbackDate FROM Feedback WHERE UserId = @UserId AND FeedbackDate > NOW() - INTERVAL 24 HOUR";
                    using (var feedbackCheckCmd = new MySqlCommand(feedbackCheckQuery, connection))
                    {
                        feedbackCheckCmd.Parameters.AddWithValue("@UserId", userId);
                        var lastFeedbackTime = feedbackCheckCmd.ExecuteScalar() as DateTime?;
                        if (lastFeedbackTime.HasValue)
                        {
                            return "You have already give feedback in the last 24 hours. You cannot give feedback again at this time.";
                        }
                    }

                    (string sentimentComment, float sentimentScore, string commentSentiments) = CalculateSentimentScore(comment);
                    var sentimentData = SqlService.GetSentimentData(connection, itemId);
                    SqlService.InsertFeedback(connection, userId, itemId, comment, rating);
                    int voteCount = SqlService.UpdateVoteCount(connection, itemId);
                    if (sentimentData.HasValue)
                    {
                        UpdateSentimentData(connection, itemId, rating, sentimentScore, commentSentiments, sentimentData.Value, voteCount);
                    }
                    else
                    {
                       SqlService.InsertNewSentimentData(connection, itemId, rating, sentimentComment, sentimentScore, commentSentiments, voteCount);
                    }

                    return "Feedback submitted successfully.";
                }
            }
            catch (Exception ex)
            {
                return "Failed to submit feedback.";
            }
        }

        public static void UpdateSentimentData(MySqlConnection connection, int itemId, int rating, float sentimentScore, string commentSentiments, (int sentimentId, double existingOverallRating, double existingSentimentScore, string existingCommentSentiments) sentimentData, int voteCount)
        {
            float overallSentimentScore = (float)((sentimentData.existingSentimentScore + sentimentScore) / 2.0);
            float overallRating = (float)((sentimentData.existingOverallRating + rating) / 2.0);

            string sentimentComment;
            if (overallSentimentScore > 0)
            {
                sentimentComment = "Positive";
            }
            else if (overallSentimentScore < 0)
            {
                sentimentComment = "Negative";
            }
            else
            {
                sentimentComment = "Neutral";
            }
            string updatedCommentSentiments = !string.IsNullOrEmpty(sentimentData.existingCommentSentiments) ? $"{sentimentData.existingCommentSentiments}, {commentSentiments}" : commentSentiments;

            var sentiment = new Sentiment
            {
                ItemId = itemId,
                OverallRating = overallRating,
                OverallCommentSentiment = sentimentComment,
                SentimentScore = overallSentimentScore,
                VoteCount = voteCount,
                CommentSentiments = updatedCommentSentiments
            };

            SqlService.UpdateSentiment(connection, sentiment);
        }

        public static (string sentiment, float sentimentScore, string commentSentiments) CalculateSentimentScore(string comment)
        {
            try
            {
                var positiveWords = File.ReadAllLines(@"C:\Users\ronak.sharma\source\repos\CafeteriaRecommendationSystem\CafeteriaRecommendationSystem\Data\positive_words.txt");
                var negativeWords = File.ReadAllLines(@"C:\Users\ronak.sharma\source\repos\CafeteriaRecommendationSystem\CafeteriaRecommendationSystem\Data\negative_words.txt");
                var negationWords = new string[] { "not", "never", "no", "nothing", "neither" };
                comment = comment.ToLower();
                var words = comment.Split(' ');
                int sentimentScore = 0;
                bool isNegation = false;
                List<string> matchedWords = new List<string>();

                for (int i = 0; i < words.Length; i++)
                {
                    if (negationWords.Contains(words[i]))
                    {
                        isNegation = true;
                        continue;
                    }

                    if (positiveWords.Contains(words[i]))
                    {
                        sentimentScore += isNegation ? -1 : 1;
                        isNegation = false;
                        matchedWords.Add(words[i]);
                    }
                    else if (negativeWords.Contains(words[i]))
                    {
                        sentimentScore += isNegation ? 1 : -1;
                        isNegation = false;
                        matchedWords.Add(words[i]);
                    }
                }

                string sentiment = sentimentScore > 0 ? "Positive" : sentimentScore < 0 ? "Negative" : "Neutral";
                string commentSentiments = string.Join(", ", matchedWords);
                return (sentiment, sentimentScore, commentSentiments);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error calculating sentiment score: " + ex.Message);
                return (string.Empty, 0, string.Empty);
            }
        }
    }
}