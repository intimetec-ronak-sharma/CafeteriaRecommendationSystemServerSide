using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CafeteriaRecommendationSystem.Services
{
    internal class AdminService
    {
        public static string AdminFunctionality(string action, string parameters)
        {
            switch (action.ToLower())
            {
                case "additem":
                    return Server.AddMenuItem(parameters);
                case "updateitem":
                    return Server.UpdateMenuItem(parameters);
                case "deleteitem":
                    return Server.DeleteMenuItem(parameters);
                case "viewitems":
                    return Server.ViewMenuItems();
                default:
                    return "Please enter a valid option.";
            }
        }
    }
}
