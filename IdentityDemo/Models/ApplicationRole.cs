﻿using Microsoft.AspNetCore.Identity;

namespace TenantsManagementApp.Models
{
    public class ApplicationRole :IdentityRole<Guid>
    {
        //Extended Properties
        public string? Description { get; set; }
        public bool IsActive { get; set; }

        //Audit Columns
        public DateTime? CreatedOn { get; set; }
        public DateTime? ModifiedOn { get;  set; }


    }
}
