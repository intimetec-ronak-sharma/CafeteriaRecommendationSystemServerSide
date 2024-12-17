using MySql.Data.MySqlClient;
using System;
using System.Text;
using CafeteriaRecommendationSystem.Models;

namespace CafeteriaRecommendationSystem.Services
{
    public class AdminService
    {
        private const string NegativeWordsFilePath = @"C:\Users\ronak.sharma\source\repos\CafeteriaRecommendationSystem\CafeteriaRecommendationSystem\Data\negative_words.txt";

        public static string ExecuteAdminAction(string action, string parameters)
        {
            try 
            {
                switch (action.ToLower())
                {
                    case "additem":
                        return AddMenuItem(parameters);
                    case "updateitem":
                        return UpdateMenuItem(parameters);
                    case "deleteitem":
                        return DeleteMenuItem(parameters);
                    case "viewitems":
                        return ViewMenuItems();
                    case "discardmenuitems":
                        return DiscardMenuItemList();
                    default:
                        return "Please enter a valid option.";
                }
            }
            catch (Exception ex)
            {
                return "An error occurred: " + ex.Message;
            }
        }

        public static string DiscardMenuItemList()
        {
            if (DateTime.Now.Day != 1)
            {
                return "Food items can only be removed on the first day of the month.";
            }

            try
            {
                string query = SqlService.DiscardMenuItemListQuery(NegativeWordsFilePath);

                using (MySqlConnection connection = SqlService.GetOpenConnection())
                {
                    using (MySqlDataReader reader = SqlService.ExecuteReader(query, connection))
                    {       
                        if (!reader.HasRows)
                        {
                            return "Discard Menu Item List";
                        }

                        var result = new StringBuilder();
                        result.AppendLine("\nItems to be discarded:");
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
                        return result.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                return "Error retrieving discard menu items: " + ex.Message;
            }
        }

        public static string AddMenuItem(string parameters)
        {
            string[] paramParts = parameters.Split(';');
            if (paramParts.Length < 8)
            {
                return "Invalid parameters for adding item";
            }

            string name = paramParts[0];
            if (!decimal.TryParse(paramParts[1], out decimal price) || !bool.TryParse(paramParts[2], out bool availabilityStatus))
            {
                return "Invalid price or availability status.";
            }
            string mealType = paramParts[3];
            string dietPreference = paramParts[4];
            string spiceLevel = paramParts[5];
            string foodPreference = paramParts[6];
            string sweetTooth = paramParts[7];

            try
            {
                using (MySqlConnection connection = SqlService.GetOpenConnection())
                {
                    int mealTypeId = SqlService.GetMealTypeId(connection, mealType);
                    if (mealTypeId == -1)
                    {
                        return "Invalid meal type";
                    }
                    Item item = new Item
                    {
                        Name = name,
                        Price = price,
                        AvailabilityStatus = availabilityStatus,
                        MealTypeId = mealTypeId,
                        DietPreference = dietPreference,
                        SpiceLevel = spiceLevel,
                        FoodPreference = foodPreference,
                        SweetTooth = sweetTooth
                    };

                    int itemId = SqlService.InsertMenuItem(connection, item);
                    SqlService.InsertNotification(connection, $"Item '{item.Name}' added to the menu.");

                    return "Item added successfully";
                }
            }
            catch (Exception ex)
            {
                return "Failed to add item";
            }
        }

        public static string UpdateMenuItem(string parameters)
        {
            string[] paramParts = parameters.Split(';');
            if (paramParts.Length < 3)
            {
                return "Invalid parameters for updating item";
            }

            if (!int.TryParse(paramParts[0], out int itemId) || !decimal.TryParse(paramParts[1], out decimal price) || !bool.TryParse(paramParts[2], out bool availabilityStatus))
            {
                return "Invalid parameters for updating item";
            }

            try
            {
                using (MySqlConnection connection = SqlService.GetOpenConnection())
                {
                    SqlService.UpdateMenuItem(connection, itemId, price, availabilityStatus);
                    return "Item updated successfully";
                }
            }
            catch (Exception ex)
            {
                return "Failed to update item";
            }
        }

        public static string DeleteMenuItem(string parameters)
        {
            int itemId;

            if (!int.TryParse(parameters, out itemId))
            {
                return "Invalid item ID for deletion";
            }
            try
            {
                using (MySqlConnection connection = SqlService.GetOpenConnection())
                {
                    if (!SqlService.CheckIfItemExists(connection, itemId))
                    {
                        return "Item does not exist";
                    }

                    SqlService.DeleteMenuItem(connection, itemId);
                    return "Item deleted successfully";
                }
            }
            catch (Exception ex)
            {
                return "Failed to delete item";
            }
        }

        public static string ViewMenuItems()
        {
            try
            {
                string query = SqlService.ViewMenuItemsQuery();
                using (MySqlConnection connection = SqlService.GetOpenConnection())
                {
                    using (MySqlDataReader reader = SqlService.ExecuteReader(query, connection))
                    {
                        if (reader.HasRows)
                            {
                                StringBuilder result = new StringBuilder();
                                result.AppendLine("\nItems List:");
                                result.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
                                result.AppendLine($"{"ID",-5} {"Name",-20} {"Price",-12} {"Availability",-15} {"Meal Type",-12} {"Diet Preference",-20} {"Spice Level",-12} {"Food Preference",-20} {"Sweet Tooth",-12}");
                                result.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");

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
                                result.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------\n");
                                return result.ToString();
                            }
                            else
                            {
                                return "No items found";
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                return "Failed to retrieve items";
            }
        }
    }
}
