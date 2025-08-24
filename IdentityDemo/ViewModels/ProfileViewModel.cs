﻿namespace TenantsManagementApp.ViewModels
{
    public class ProfileViewModel
    {
        public string Email { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string? LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime? LastLoggedIn { get; set; }
        public DateTime? CreatedOn { get; set; }
    }
}