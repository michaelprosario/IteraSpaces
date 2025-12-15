namespace AppCore.DTOs
{
    public class GetRoleByIdQuery
    {
        public string RoleId { get; set; } = string.Empty;
    }

    public class GetRoleByNameQuery
    {
        public string Name { get; set; } = string.Empty;
    }

    public class GetUserRolesQuery
    {
        public string UserId { get; set; } = string.Empty;
    }

    public class GetUsersInRoleQuery
    {
        public string RoleId { get; set; } = string.Empty;
    }
}
