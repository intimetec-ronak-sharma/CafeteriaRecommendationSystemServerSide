using MySql.Data.MySqlClient;
using System;

namespace CafeteriaRecommendationSystem
{
    internal class DatabaseUtility
    {
        private const string connectionString = "Server=localhost;Database=cafeteriarecommenderdb;User ID=root;Password=ronak5840R!;Port=3306";

        public static MySqlConnection GetConnection()
        {
            return new MySqlConnection(connectionString);
        }
    }
}
