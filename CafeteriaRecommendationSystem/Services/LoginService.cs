using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CafeteriaRecommendationSystem.Server;

namespace CafeteriaRecommendationSystem.Services
{
    internal class LoginService
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

    }
}
