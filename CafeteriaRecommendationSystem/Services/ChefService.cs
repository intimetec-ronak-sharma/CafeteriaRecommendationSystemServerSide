using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CafeteriaRecommendationSystem.Services
{
    internal class ChefService
    {
        public static string ChefFunctionality(string action, string parameters)
        {
            switch (action.ToLower())
            {
                case "recommenditem":
                    return "Chef: Recommend item functionality executed";
                case "viewfeedback":
                    return Server.ViewFeedback();
                case "viewmenuitem":
                    return Server.ViewMenuItems();
                default:
                    return "Please enter a valid option.";
            }
        }
    }
}
