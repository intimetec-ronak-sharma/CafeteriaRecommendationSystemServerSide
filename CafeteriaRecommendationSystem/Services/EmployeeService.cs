using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CafeteriaRecommendationSystem.Services
{
    internal class EmployeeService
    {
        public static string EmployeeFunctionality(string action, string parameters)
        {
            switch (action.ToLower())
            {
                case "viewmenu":
                    return ViewMenu(parameters);
                case "givefeedback":
                    string[] feedbackParams = parameters.Split(';');
                    return GiveFeedback(parameters).Result;
                default:
                    return "Employee: Unknown action";
            }
        }

            public static string ViewMenu(string mealType)
            {
            try
            {
                using (MySqlConnection connection = DatabaseUtility.GetConnection())
                {
                    connection.Open();
                    string query = @"
                    SELECT r.RecommendationId, r.ItemId, i.Name, i.Price, i.AvailabilityStatus, s.OverallRating, s.SentimentComment
                    FROM Recommendation r
                    JOIN Item i ON r.ItemId = i.ItemId
                    LEFT JOIN Sentiment s ON i.ItemId = s.ItemId
                    JOIN MealType mt ON i.MealTypeId = mt.meal_type_id
                    WHERE mt.type = @MealType";

                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@MealType", mealType);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            StringBuilder menu = new StringBuilder();
                            while (reader.Read())
                            {
                                int recommendationId = reader.GetInt32("RecommendationId");
                                int itemId = reader.GetInt32("ItemId");
                                string itemName = reader.GetString("Name");
                                decimal price = reader.GetDecimal("Price");
                                bool availabilityStatus = reader.GetBoolean("AvailabilityStatus");
                                float overallRating = reader.GetFloat("OverallRating");
                                string sentimentComment = reader.IsDBNull(reader.GetOrdinal("SentimentComment")) ? string.Empty : reader.GetString("SentimentComment");
                                //  return menu.ToString();
                            }
                            return "Successfully retrieved menu data.";
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

        public static async Task<string> GiveFeedback(string parameters)
        {

            try
            {
                string[] feedbackParams = parameters.Split(';');
                int userId = int.Parse(feedbackParams[0]);
                int itemId = int.Parse(feedbackParams[1]);
                string comment = feedbackParams[2];
                int rating = int.Parse(feedbackParams[3]);

                (string sentimentComment, float sentimentScore) = CalculateSentimentScore(comment);
                using (MySqlConnection connection = DatabaseUtility.GetConnection())
                {
                    connection.Open();
                    try
                    {
                        string selectQuery = "SELECT SentimentId,OverallRating,SentimentScore FROM Sentiment WHERE ItemId = @ItemId";
                        MySqlCommand selectCmd = new MySqlCommand(selectQuery, connection);
                        selectCmd.Parameters.AddWithValue("@ItemId", itemId);
                        MySqlDataReader reader = selectCmd.ExecuteReader();

                        double existingOverallRating = 0.0;
                        double existingSentimentScore = 0.0;
                        int sentimentId = 0;
                        if (reader.Read())
                        {
                            sentimentId = reader.GetInt32("SentimentId");
                            if (!reader["OverallRating"].Equals(DBNull.Value))
                            {
                                existingOverallRating = Convert.ToDouble(reader["OverallRating"]);
                            }

                            if (!reader["SentimentScore"].Equals(DBNull.Value))
                            {
                                existingSentimentScore = Convert.ToDouble(reader["SentimentScore"]);
                            }
                        }
                        reader.Close();

                        string feedbackQuery = "INSERT INTO Feedback (UserId, ItemId, Comment, Rating, FeedbackDate) VALUES (@UserId, @ItemId, @Comment, @Rating, NOW())";
                        using (MySqlCommand insertCmd = new MySqlCommand(feedbackQuery, connection))
                        {
                            insertCmd.Parameters.AddWithValue("@UserId", userId);
                            insertCmd.Parameters.AddWithValue("@ItemId", itemId);
                            insertCmd.Parameters.AddWithValue("@Comment", comment);
                            insertCmd.Parameters.AddWithValue("@Rating", rating);
                            insertCmd.ExecuteNonQuery();
                        }

                        string votedItemsQuery = "INSERT INTO voteditem (UserId, ItemId, VoteDate) VALUES (@UserId, @ItemId, NOW())";
                        using (MySqlCommand insertCmd = new MySqlCommand(votedItemsQuery, connection))
                        {
                            insertCmd.Parameters.AddWithValue("@UserId", userId);
                            insertCmd.Parameters.AddWithValue("@ItemId", itemId);
                            insertCmd.ExecuteNonQuery();
                        }

                        int voteCount = 1;
                        string countQuery = "SELECT COUNT(*) FROM VotedItem WHERE ItemId = @ItemId";
                        using (MySqlCommand countCmd = new MySqlCommand(countQuery, connection))
                        {
                            countCmd.Parameters.AddWithValue("@ItemId", itemId);
                            voteCount = Convert.ToInt32(countCmd.ExecuteScalar());
                        }


                        float overallSentimentScore = 0, overallRating = 0;
                        if (sentimentId != 0)
                        {
                            overallSentimentScore = (float)((existingSentimentScore + sentimentScore) / 2.0);
                            overallRating = (float)((existingOverallRating + rating) / 2.0);
                            string updateQuery = "UPDATE Sentiment SET OverallRating = @OverallRating, SentimentComment = @SentimentComment, SentimentScore = @SentimentScore WHERE ItemId = @ItemId";
                            using (MySqlCommand updateCmd = new MySqlCommand(updateQuery, connection))
                            {
                                updateCmd.Parameters.AddWithValue("@OverallRating", overallRating);
                                updateCmd.Parameters.AddWithValue("@SentimentComment", sentimentComment);
                                updateCmd.Parameters.AddWithValue("@SentimentScore", overallSentimentScore);
                                updateCmd.Parameters.AddWithValue("@ItemId", itemId);
                                updateCmd.Parameters.AddWithValue("@VoteCount", voteCount);
                                updateCmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            string insertQuery = "INSERT INTO Sentiment (ItemId, OverallRating, SentimentComment, SentimentScore, VoteCount) VALUES (@ItemId, @OverallRating, @SentimentComment, @SentimentScore,@VoteCount)";
                            using (MySqlCommand insertCmd = new MySqlCommand(insertQuery, connection))
                            {
                                insertCmd.Parameters.AddWithValue("@ItemId", itemId);
                                insertCmd.Parameters.AddWithValue("@OverallRating", rating);
                                insertCmd.Parameters.AddWithValue("@SentimentComment", sentimentComment);
                                insertCmd.Parameters.AddWithValue("@SentimentScore", sentimentScore);
                                insertCmd.Parameters.AddWithValue("@VoteCount", voteCount);
                                insertCmd.ExecuteNonQuery();
                            }
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


        public static (string sentiment, float sentimentScore) CalculateSentimentScore(string comment)
        {
            try
            {
                var positiveWords = File.ReadAllLines("C:\\Users\\ronak.sharma\\source\\repos\\CafeteriaRecommendationSystem\\CafeteriaRecommendationSystem\\positive_words.txt");
                var negativeWords = File.ReadAllLines("C:\\Users\\ronak.sharma\\source\\repos\\CafeteriaRecommendationSystem\\CafeteriaRecommendationSystem\\negative_words.txt");
                //var words = comment.Split(new char[] { ' ', '.', ',', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

                comment = comment.ToLower();

                int positiveCount = 0;
                int negativeCount = 0;

                foreach (var phrase in positiveWords)
                {
                    if (comment.Contains(phrase.ToLower()))
                    {
                        positiveCount++;
                    }
                }

                foreach (var phrase in negativeWords)
                {
                    if (comment.Contains(phrase.ToLower()))
                    {
                        negativeCount++;
                    }
                }
                float sentimentScore = (positiveCount - negativeCount) / (float)(positiveCount + negativeCount + 1);

                string sentimentComment = sentimentScore >= 0.0 ? "Positive" : "Negative";

                return (sentimentComment, sentimentScore);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error calculating sentiment score: " + ex.Message);
                return (string.Empty, 0);
            }

        }
    }
}