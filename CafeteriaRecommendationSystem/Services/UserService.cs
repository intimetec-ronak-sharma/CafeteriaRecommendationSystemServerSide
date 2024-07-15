using CafeteriaRecommendationSystem.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace CafeteriaRecommendationSystem.Services
{
     public class UserService
     {
        private static Dictionary<string, LoginResult> activeUsers = new Dictionary<string, LoginResult>();

        public static LoginResult LoginUser(string email)
        {
            try
            {
                using (var connection = DatabaseUtility.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT u.UserId, RoleName FROM User u JOIN Roles r ON u.RoleId = r.RoleId WHERE Email = @Email";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Email", email);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int userId = reader.GetInt32(0);
                                string role = reader.GetString(1);
                                var loginResult = new LoginResult { IsSuccessful = true, UserId = userId, UserRole = role };
                                activeUsers[email] = loginResult;

                                return loginResult;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error logging in user: {ex.Message}");
            }
            return new LoginResult { IsSuccessful = false, UserId = 0, UserRole = "" };
        }
        public static string LogoutUser(string email)
        {
            if (activeUsers.ContainsKey(email))
            {
                activeUsers.Remove(email);
                return "Logout successful";
            }
            else
            {
                return "Logout failed: User not logged in";
            }
        }

        public UserSessionInfo LogUserLogin(int userId)
        {
            using (var connection = DatabaseUtility.GetConnection())
            {
                connection.Open();

                string deleteOldSessionsQuery = "DELETE FROM UserSession WHERE UserID = @UserID AND SessionID NOT IN ( SELECT SessionID FROM ( SELECT SessionID FROM UserSession WHERE UserID = @UserID ORDER BY LoginTime DESC LIMIT 1 ) AS subquery)";
                MySqlCommand deleteCmd = new MySqlCommand(deleteOldSessionsQuery, connection);
                deleteCmd.Parameters.AddWithValue("@UserID", userId);
                deleteCmd.ExecuteNonQuery();

                string insertLoginQuery = "INSERT INTO UserSession (UserID, LoginTime) VALUES (@UserID, NOW())";
                MySqlCommand cmd = new MySqlCommand(insertLoginQuery, connection);
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.ExecuteNonQuery();
                int sessionId = (int)cmd.LastInsertedId;

                string selectLoginTimeQuery = "SELECT LoginTime FROM UserSession WHERE SessionID = @SessionID";
                cmd = new MySqlCommand(selectLoginTimeQuery, connection);
                cmd.Parameters.AddWithValue("@SessionID", sessionId);
                var result = cmd.ExecuteScalar();

                DateTime loginTime = result == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(result);

                return new UserSessionInfo { SessionID = sessionId, LoginTime = loginTime };
            }
        }

        public void LogUserLogout(int sessionId)
        {
            using (var conn = DatabaseUtility.GetConnection())
            {
                conn.Open();
                string updateLogoutQuery = "UPDATE UserSession SET LogoutTime = NOW(), TotalLoginTime = TIMEDIFF(NOW(), LoginTime) WHERE SessionID = @SessionID";
                MySqlCommand cmd = new MySqlCommand(updateLogoutQuery, conn);
                cmd.Parameters.AddWithValue("@SessionID", sessionId);
                cmd.ExecuteNonQuery();
            }
        }

        public List<string> GetUnreadNotifications(DateTime? lastLoginTime)
        {
            List<string> notifications = new List<string>();
            using (var conn = DatabaseUtility.GetConnection())
            {
                conn.Open();
                string selectNotificationsQuery;
                if (lastLoginTime == null)
                {
                    selectNotificationsQuery = @"SELECT Message FROM notification WHERE NotificationDate > DATE_SUB(NOW(), INTERVAL 1 DAY)";
                }

                else
                {
                    selectNotificationsQuery = @"SELECT Message FROM notification WHERE NotificationDate > @LastLoginTime AND NotificationDate > DATE_SUB(NOW(), INTERVAL 1 DAY)";
                }
                MySqlCommand cmd = new MySqlCommand(selectNotificationsQuery, conn);
                cmd.Parameters.AddWithValue("@LastLoginTime", lastLoginTime);
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        notifications.Add(reader.GetString("Message"));
                    }
                }
            }
            return notifications;
        }
        public DateTime? GetLastLoginTime(int userId, DateTime currentLoginTime)
        {
            using (var conn = DatabaseUtility.GetConnection())
            {
                conn.Open();
                string selectLastLoginTimeQuery = @"SELECT MAX(LoginTime) FROM UserSession WHERE UserID = @UserID AND LoginTime <> @CurrentLoginTime";
                MySqlCommand cmd = new MySqlCommand(selectLastLoginTimeQuery, conn);
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.Parameters.AddWithValue("@CurrentLoginTime", currentLoginTime);

                var result = cmd.ExecuteScalar();
                if (result == DBNull.Value || result == null)
                {
                    return null;
                }
                else
                {
                    return Convert.ToDateTime(result);
                }
            }
        }
    }
}
