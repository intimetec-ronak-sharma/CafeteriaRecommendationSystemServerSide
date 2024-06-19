using System;
using System.Collections.Generic;
using System.Linq;
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
                    return Server.ViewMenu(parameters);
                case "givefeedback":
                    string[] feedbackParams = parameters.Split(';');
                    return Server.GiveFeedback(parameters).Result;
                default:
                    return "Employee: Unknown action";
            }
        }

    }
}
