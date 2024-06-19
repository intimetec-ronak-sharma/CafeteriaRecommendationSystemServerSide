using CafeteriaRecommendationSystem.Services;

namespace CafeteriaRecommendationSystem
{
    public class Server
    {
        public static LoginResult LoginUser(string email)
        {
            return LoginService.LoginUser(email);
        }

        public static string ExecuteRoleBasedFunctionality(string role, string action, string parameters)
        {
            return RoleService.ExecuteRoleBasedFunctionality(role, action, parameters);
        }
    }
}

   