using MySql.Data.MySqlClient;
using System;

namespace CafeteriaRecommendationSystem.Services
{
     public class LoginService
     {
        public static LoginResult LoginUser(string email)
        {
            try
            {
                using (MySqlConnection connection = DatabaseUtility.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT u.UserId, RoleName FROM User u JOIN Roles r ON u.RoleId = r.RoleId WHERE Email = @Email";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Email", email);
                        using (MySqlDataReader reader = command.ExecuteReader())
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

    }
}
