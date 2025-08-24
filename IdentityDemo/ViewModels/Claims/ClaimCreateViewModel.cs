﻿using System.ComponentModel.DataAnnotations;
namespace TenantsManagementApp.ViewModels.Claims
{
    public class ClaimCreateViewModel
    {
        [Display(Name = "Claim Type")]
        [Required(ErrorMessage = "Claim Type is required.")]
        [StringLength(200, ErrorMessage = "Claim Type cannot exceed {1} characters.")]
        public string ClaimType { get; set; } = "Permission";

        [Display(Name = "Claim Value")]
        [Required(ErrorMessage = "Claim Value is required.")]
        [StringLength(200, ErrorMessage = "Claim Value cannot exceed {1} characters.")]
        public string ClaimValue { get; set; } = null!;

        [Display(Name = "Assign To")]
        [Required(ErrorMessage = "Please choose where to assign this claim (User, Role, or Both).")]
        [StringLength(64, ErrorMessage = "Assign To cannot exceed {1} characters.")]
        [RegularExpression("^(User|Role|Both)$", ErrorMessage = "Assign To must be one of: User, Role, Both.")]
        public string Category { get; set; } = null!; // User, Role, Both

        [Display(Name = "Description")]
        [StringLength(500, ErrorMessage = "Description cannot exceed {1} characters.")]
        public string? Description { get; set; }
    }
}