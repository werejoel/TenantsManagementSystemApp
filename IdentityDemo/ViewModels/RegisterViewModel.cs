﻿using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace TenantsManagementApp.ViewModels
{
    public class RegisterViewModel
    {        [Required]
        [Display(Name = "First Name")]
        [StringLength(50, ErrorMessage = "First Name cannot be longer than 50 characters.")]
        public string FirstName { get; set; } = null!;

        [Display(Name = "Last Name")]
        [StringLength(50, ErrorMessage = "Last Name cannot be longer than 50 characters.")]
        public string? LastName { get; set; }

        [Required(ErrorMessage = "Email Id is Required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        [Remote(action: "IsEmailAvailable", controller: "RemoteValidation")]
        public string Email { get; set; } = null!;

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "PhoneNumber is Required")]
        [Phone(ErrorMessage = "Please enter a valid Phone number")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = default!;

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
        public string Password { get; set; } = null!;

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Password and confirmation password do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = null!;
    }
}