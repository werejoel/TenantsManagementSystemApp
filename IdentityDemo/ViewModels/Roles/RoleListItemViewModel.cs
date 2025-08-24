﻿namespace TenantsManagementApp.ViewModels.Roles
{
    public class RoleListItemViewModel
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public bool IsActive { get; init; }
        public DateTime? CreatedOn { get; init; }
    }
}