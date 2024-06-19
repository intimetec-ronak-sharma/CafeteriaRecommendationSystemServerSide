using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

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
                    string query = "SELECT ItemId, Name, Price, AvailabilityStatus FROM Items i JOIN Meal_Type mt ON i.MealTypeId = mt.meal_type_id WHERE mt.type = @MealType";
                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
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
            
            try
            {
                string[] feedbackParams = parameters.Split(';');
                int userId = int.Parse(feedbackParams[0]);
                int itemId = int.Parse(feedbackParams[1]);
                string comment = feedbackParams[2];
                int rating = int.Parse(feedbackParams[3]);

                (string sentimentComment, string sentimentScore) = await ExtractKeywordWithAPI(comment);
                using (MySqlConnection connection = DatabaseUtility.GetConnection())
                {
                    connection.Open();
                    try
                    {
                        string selectQuery = "SELECT SentimentId,OverallRating,SentimentScore FROM Sentiments WHERE ItemId = @ItemId";
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
                        float overallSentimentScore = 0, overallRating = 0;
                        if (sentimentId != 0)
                        {
                            overallSentimentScore = (float)((existingSentimentScore + double.Parse(sentimentScore)) / 2.0);
                            overallRating = (float)((existingOverallRating + rating) / 2.0);
                            string updateQuery = "UPDATE Sentiments SET OverallRating = @OverallRating, SentimentComment = @SentimentComment, SentimentScore = @SentimentScore WHERE ItemId = @ItemId";
                            using (MySqlCommand updateCmd = new MySqlCommand(updateQuery, connection))
                            {
                                updateCmd.Parameters.AddWithValue("@OverallRating", overallRating);
                                updateCmd.Parameters.AddWithValue("@SentimentComment", sentimentComment);
                                updateCmd.Parameters.AddWithValue("@SentimentScore", overallSentimentScore);
                                updateCmd.Parameters.AddWithValue("@ItemId", itemId);
                                updateCmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            string insertQuery = "INSERT INTO Sentiments (ItemId, OverallRating, SentimentComment, SentimentScore) VALUES (@ItemId, @OverallRating, @SentimentComment, @SentimentScore)";
                            using (MySqlCommand insertCmd = new MySqlCommand(insertQuery, connection))
                            {
                                insertCmd.Parameters.AddWithValue("@ItemId", itemId);
                                insertCmd.Parameters.AddWithValue("@OverallRating", rating);
                                insertCmd.Parameters.AddWithValue("@SentimentComment", sentimentComment);
                                insertCmd.Parameters.AddWithValue("@SentimentScore", sentimentScore);
                                insertCmd.ExecuteNonQuery();
                            }
                        }


                        string feedbackQuery = "INSERT INTO Feedback (UserId, ItemId, Comment, Rating, FeedbackDate) VALUES (@UserId, @ItemId, @Comment, @Rating, NOW())";
                        using (MySqlCommand insertCmd = new MySqlCommand(feedbackQuery, connection))
                        {
                            insertCmd.Parameters.AddWithValue("@UserId", userId);
                            insertCmd.Parameters.AddWithValue("@ItemId", itemId);
                            insertCmd.Parameters.AddWithValue("@Comment", comment);
                            insertCmd.Parameters.AddWithValue("@Rating", rating);
                            insertCmd.ExecuteNonQuery();
                        }

                        string votedItemsQuery = "INSERT INTO voteditems (UserId, ItemId, VoteDate) VALUES (@UserId, @ItemId, NOW())";
                        using (MySqlCommand insertCmd = new MySqlCommand(votedItemsQuery, connection))
                        {
                            insertCmd.Parameters.AddWithValue("@UserId", userId);
                            insertCmd.Parameters.AddWithValue("@ItemId", itemId);
                            insertCmd.ExecuteNonQuery();
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

        private static async Task<(string sentiment, string sentimentScore)> ExtractKeywordWithAPI(string comment)
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
                    (string sentiment, string sentimentScore) = ParseKeywordResponse(responseBody);

                    return (sentiment, sentimentScore);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error calling keyword extraction API: " + ex.Message);
                return (string.Empty, string.Empty);
            }
        }

        public static (string sentiment, string sentimentScore) ParseKeywordResponse(string responseBody)
        {
            try
            {
                JObject json = JObject.Parse(responseBody);
                string sentiment = json["sentiment"]?.ToString();
                string sentimentScore = json["score"]?.ToString();
                return (sentiment ?? "", sentimentScore ?? "");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error parsing keyword extraction response: " + ex.Message);
                return (string.Empty, string.Empty);
            }
        }


    }
}
