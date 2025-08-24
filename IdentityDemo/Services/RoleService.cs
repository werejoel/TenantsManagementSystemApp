﻿using TenantsManagementApp.Data;
using TenantsManagementApp.ViewModels.Roles;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TenantsManagementApp.Models;
using TenantsManagementApp.ViewModels;

namespace TenantsManagementApp.Services
{
    public class RoleService : IRoleService
    {
        private readonly RoleManager<ApplicationRole> _roleManager; // Manages roles in ASP.NET Identity
        private readonly ApplicationDbContext _dbContext; // For direct DB access

        public RoleService(RoleManager<ApplicationRole> roleManager,
        ApplicationDbContext dbContext)
        {
            _roleManager = roleManager;
            _dbContext = dbContext;
        }
        // Retrieves a paginated list of roles based on filter criteria.
        public async Task<PagedResult<RoleListItemViewModel>> GetRolesAsync(RoleListFilterViewModel filter)
        {
            // Start with all roles (No Tracking = better performance for read-only queries)
            var query = _roleManager.Roles.AsNoTracking();
            // Apply search filter (if provided)
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var s = filter.Search.Trim();
                query = query.Where(r => r.Name!.Contains(s) || (r.Description ?? "").Contains(s));
            }
            // Apply Active/Inactive filter (if provided)
            if (filter.IsActive.HasValue)
                query = query.Where(r => r.IsActive == filter.IsActive.Value);
            // Get total role count for pagination
            var total = await query.CountAsync();
            // Get current page of roles
            var items = await query
            .OrderBy(r => r.Name) // Sort alphabetically
            .Skip((filter.PageNumber - 1) * filter.PageSize) // Skip previous pages
            .Take(filter.PageSize) // Take only required items
            .Select(r => new RoleListItemViewModel
            {
                Id = r.Id,
                Name = r.Name!,
                Description = r.Description,
                IsActive = r.IsActive,
                CreatedOn = r.CreatedOn
            })
            .ToListAsync();
            // Return paginated result
            return new PagedResult<RoleListItemViewModel>
            {
                Items = items,
                TotalCount = total,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }
        // Creates a new role.
        public async Task<(IdentityResult Result, Guid? RoleId)> CreateAsync(RoleCreateViewModel model)
        {
            // Ensure the role name is unique
            bool roleExists = await _roleManager.RoleExistsAsync(model.Name);
            if (roleExists)
            {
                return (IdentityResult.Failed(new IdentityError { Description = "Role name already exists." }), null);
            }
            // Create new ApplicationRole entity
            var role = new ApplicationRole
            {
                Id = Guid.NewGuid(),
                Name = model.Name.Trim(),
                NormalizedName = model.Name.Trim().ToUpperInvariant(), // For case-insensitive comparison
                Description = model.Description?.Trim(),
                IsActive = model.IsActive,
                CreatedOn = DateTime.UtcNow,
                ModifiedOn = DateTime.UtcNow
            };
            // Save to database
            var result = await _roleManager.CreateAsync(role);
            return (result, result.Succeeded ? role.Id : null);
        }
        // Retrieves a role for editing.
        public async Task<RoleEditViewModel?> GetForEditAsync(Guid id)
        {
            var role = await _roleManager.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            if (role == null)
                return null;
            // Map to edit view model
            return new RoleEditViewModel
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                Description = role.Description,
                IsActive = role.IsActive,
                ConcurrencyStamp = role.ConcurrencyStamp // For concurrency checks
            };
        }
        // Updates an existing role.
        public async Task<IdentityResult> UpdateAsync(RoleEditViewModel model)
        {
            var role = await _roleManager.FindByIdAsync(model.Id.ToString());
            if (role == null)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "NotFound",
                    Description = "Role not found."
                });
            }
            // Concurrency check — prevents overwriting changes made by others
            if (!string.Equals(role.ConcurrencyStamp, model.ConcurrencyStamp, StringComparison.Ordinal))
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "ConcurrencyFailure",
                    Description = "This role was modified by another user while you were editing. Please reload the page and try again."
                });
            }
            // Ensure name is still unique (excluding current role)
            if (!string.Equals(role.Name, model.Name, StringComparison.Ordinal))
            {
                var dup = await _roleManager.FindByNameAsync(model.Name);
                if (dup != null && dup.Id != role.Id)
                {
                    return IdentityResult.Failed(new IdentityError
                    {
                        Code = "DuplicateRoleName",
                        Description = $"Another role already uses this name: {model.Name}"
                    });
                }
            }
            // Update properties
            role.Name = model.Name.Trim();
            role.NormalizedName = role.Name.ToUpperInvariant();
            role.Description = model.Description;
            role.IsActive = model.IsActive;
            role.ModifiedOn = DateTime.UtcNow;
            // Save changes — updates ConcurrencyStamp automatically
            return await _roleManager.UpdateAsync(role);
        }
        // Deletes a role if it has no assigned users.
        public async Task<IdentityResult> DeleteAsync(Guid id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
                return IdentityResult.Failed(new IdentityError { Description = "Role not found." });
            // Prevent deletion if any users are assigned to this role
            var hasUsers = await _dbContext.Set<IdentityUserRole<Guid>>()
            .AsNoTracking()
            .AnyAsync(ur => ur.RoleId == id);
            if (hasUsers)
                return IdentityResult.Failed(new IdentityError { Description = "Cannot delete a role that still has users. Remove users from the role first." });
            // Delete role
            return await _roleManager.DeleteAsync(role);
        }
        // Retrieves details of a role, including paginated list of users in that role.
        public async Task<RoleDetailsViewModel?> GetDetailsAsync(Guid id, int pageNumber, int pageSize)
        {
            var role = await _roleManager.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            if (role == null)
                return null;

            // Query all users in this role via junction table (IdentityUserRole)
            var usersQuery =
                from ur in _dbContext.Set<IdentityUserRole<Guid>>().AsNoTracking() //Left table - User Roles
                join u in _dbContext.Set<ApplicationUser>().AsNoTracking() //Right table - Users
                on ur.UserId equals u.Id
                where ur.RoleId == id
                select new UserInRoleViewModel
                {
                    Id = u.Id,
                    Email = u.Email!,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    IsActive = u.IsActive,
                    PhoneNumber = u.PhoneNumber
                };

            // Get total user count
            var total = await usersQuery.CountAsync();

            // Get current page of users
            var users = await usersQuery
                .OrderBy(u => u.Email)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            //fetch the role claims
            var claims = await _roleManager.GetClaimsAsync(role);
            var claimTexts = claims
                .OrderBy(c => c.Type).ThenBy(c => c.Value)
                .Select(c => $"{c.Type}: {c.Value}")
                .ToList();

            // Return role details with users
            return new RoleDetailsViewModel
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                Description = role.Description,
                IsActive = role.IsActive,
                CreatedOn = role.CreatedOn,
                ModifiedOn = role.ModifiedOn,
                Claims = claimTexts, //Populate the Role Claims in the RoleDetailsViewModel
                Users = new PagedResult<UserInRoleViewModel>
                {
                    Items = users,
                    TotalCount = total,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                }
            };
        }

        //GetClaimsForEditAsync
        public async Task<RoleClaimsEditViewModel?> GetClaimsForEditAsync(Guid roleId)
        {
            var role = await _roleManager.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == roleId);
            if (role == null) return null;

            // Allowed options: Active + (Role or Both) from ClaimMasters
            var allClaims = await _dbContext.ClaimMasters.AsNoTracking()
                .Where(c => c.IsActive && (c.Category == "Role" || c.Category == "Both"))
                .OrderBy(c => c.ClaimType).ThenBy(c => c.ClaimValue)
                .ToListAsync();

            var current = await _roleManager.GetClaimsAsync(role);

            return new RoleClaimsEditViewModel
            {
                RoleId = role.Id,
                RoleName = role.Name ?? "",
                Claims = allClaims.Select(c => new RoleClaimCheckboxItem
                {
                    ClaimId = c.Id,
                    ClaimType = c.ClaimType,
                    ClaimValue = c.ClaimValue,
                    Category = c.Category,
                    Description = c.Description,
                    IsSelected = current.Any(rc => rc.Type == c.ClaimType && rc.Value == c.ClaimValue)
                }).ToList()
            };
        }

        //UpdateClaimsAsync
        public async Task<IdentityResult> UpdateClaimsAsync(Guid roleId, IEnumerable<Guid> selectedClaimIds)
        {
            var role = await _roleManager.FindByIdAsync(roleId.ToString());
            if (role == null)
                return IdentityResult.Failed(new IdentityError { Code = "RoleNotFound", Description = "Role not found." });

            // Only accept active Role/Both claims
            var allowed = await _dbContext.ClaimMasters
                .Where(c => c.IsActive && (c.Category == "Role" || c.Category == "Both"))
                .ToListAsync();

            var selected = allowed.Where(c => selectedClaimIds.Contains(c.Id)).ToList();

            // Replace all current role claims (simple, matches user-claims flow)
            var current = await _roleManager.GetClaimsAsync(role);
            foreach (var c in current)
            {
                var rm = await _roleManager.RemoveClaimAsync(role, c);
                if (!rm.Succeeded) return rm;
            }

            foreach (var c in selected)
            {
                var add = await _roleManager.AddClaimAsync(role, new Claim(c.ClaimType, c.ClaimValue));
                if (!add.Succeeded) return add;
            }

            return IdentityResult.Success;
        }

    }
}