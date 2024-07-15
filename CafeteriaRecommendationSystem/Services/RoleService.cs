namespace CafeteriaRecommendationSystem.Services
{
    public static class RoleService
    {
        public static string ExecuteRoleBasedFunctionality(string role, string action, string parameters)
        {
            switch (role)
            {
                case "Admin":
                    return AdminService.ExecuteAdminAction(action, parameters);
                case "Chef":
                    return ChefService.ExecuteChefAction(action, parameters);
                case "Employee":
                    return EmployeeService.ExecuteEmployeeAction(action, parameters);
                default:
                    return "Please enter a valid option.";
            }
        }
    }
}
