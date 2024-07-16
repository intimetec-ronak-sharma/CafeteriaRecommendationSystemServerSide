namespace CafeteriaRecommendationSystem.Services
{
    public static class RoleService
    {
        public static string ExecuteRoleBasedFunctionality(string role, string action, string parameters)
        {
            switch (role)
            {
                case "Admin":
                    return AdminService.AdminFunctionality(action, parameters);
                case "Chef":
                    return ChefService.ChefFunctionality(action, parameters);
                case "Employee":
                    return EmployeeService.EmployeeFunctionality(action, parameters);
                default:
                    return "Please enter a valid option.";
            }
        }
    }
}
