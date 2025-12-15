namespace AppCore.DTOs
{
    public class CreateRoleCommand
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsSystemRole { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class UpdateRoleCommand
    {
        public string RoleId { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class DeleteRoleCommand
    {
        public string RoleId { get; set; } = string.Empty;
        public string DeletedBy { get; set; } = string.Empty;
    }

    public class AssignRoleToUserCommand
    {
        public string UserId { get; set; } = string.Empty;
        public string RoleId { get; set; } = string.Empty;
        public string AssignedBy { get; set; } = string.Empty;
    }

    public class RemoveRoleFromUserCommand
    {
        public string UserId { get; set; } = string.Empty;
        public string RoleId { get; set; } = string.Empty;
        public string RemovedBy { get; set; } = string.Empty;
    }
}
