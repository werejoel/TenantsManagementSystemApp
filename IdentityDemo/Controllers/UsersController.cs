﻿using TenantsManagementApp.Services;
using TenantsManagementApp.ViewModels;
using TenantsManagementApp.ViewModels.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace TenantsManagementApp.Controllers
{
    // Require authentication for ALL actions in this controller
    [Authorize]
    public class UsersController : Controller
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;
        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }
        // GET: /Users
        // Hybrid policy: Admin or Manager role OR Permission=ViewUsers
        [HttpGet]
        [Authorize(Policy = "ViewUsersPolicy")]
        public async Task<IActionResult> Index([FromQuery] UserListFilterViewModel filter)
        {
            try
            {
                var result = await _userService.GetUsersAsync(filter);
                ViewBag.Filter = filter;
                return View(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users list.");
                SetError("We couldn’t load the users right now. Please try again.");
                return View(new PagedResult<UserListItemViewModel>());
            }
        }
        // GET: /Users/Create
        // Hybrid policy: Admin role OR Permission=AddUser
        [HttpGet]
        [Authorize(Policy = "AddUserPolicy")]
        public IActionResult Create()
        {
            return View(new UserCreateViewModel());
        }
        // POST: /Users/Create
        // Hybrid policy: Admin role OR Permission=AddUser
        [HttpPost]
        [Authorize(Policy = "AddUserPolicy")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(model);
                var (result, newId) = await _userService.CreateAsync(model);
                if (result.Succeeded)
                {
                    SetSuccess($"User '{model.Email}' was created successfully.");
                    return RedirectToAction(nameof(Index));
                }
                AddIdentityErrors(result);
                return View(model);
            }
            catch (DbUpdateException dbx)
            {
                _logger.LogError(dbx, $"DB error while creating user {model.Email}");
                SetError("We couldn’t create the user due to a database error. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error creating user {model.Email}");
                SetError("An unexpected error occurred while creating the user.");
                return View(model);
            }
        }
        // GET: /Users/Edit/{id}
        // Hybrid policy: Admin role OR Permission=EditUser
        [HttpGet]
        [Authorize(Policy = "EditUserPolicy")]
        public async Task<IActionResult> Edit(Guid id)
        {
            try
            {
                var userEditViewModel = await _userService.GetForEditAsync(id);
                if (userEditViewModel == null)
                {
                    SetError("The user you’re trying to edit was not found.");
                    return RedirectToAction(nameof(Index));
                }
                return View(userEditViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading edit form for user {id}");
                SetError("We couldn’t load the edit form. Please try again.");
                return RedirectToAction(nameof(Index));
            }
        }
        // POST: /Users/Edit
        // Hybrid policy: Admin role OR Permission=EditUser
        [HttpPost]
        [Authorize(Policy = "EditUserPolicy")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(model);
                var result = await _userService.UpdateAsync(model);
                if (result.Succeeded)
                {
                    SetSuccess("User was updated successfully.");
                    return RedirectToAction(nameof(Index));
                }
                if (result.Errors.Any(e =>
                string.Equals(e.Code, "ConcurrencyFailure", StringComparison.OrdinalIgnoreCase)))
                {
                    SetError("This user was modified by another admin. Please reload the page and try again.");
                }
                AddIdentityErrors(result);
                return View(model);
            }
            catch (DbUpdateConcurrencyException cex)
            {
                _logger.LogWarning(cex, $"Concurrency error updating user {model.Id}");
                SetError("Your changes could not be saved because another update occurred. Please reload and try again.");
                return View(model);
            }
            catch (DbUpdateException dbx)
            {
                _logger.LogError(dbx, $"DB error while updating user {model.Id}");
                SetError("We couldn’t update the user due to a database error. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error updating user {model.Id}");
                SetError("An unexpected error occurred while updating the user.");
                return View(model);
            }
        }
        // GET: /Users/Details/{id}
        // Hybrid policy: Admin or Manager role OR Permission=ViewUsers
        [HttpGet]
        [Authorize(Policy = "ViewUsersPolicy")]
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var userDetailsViewModel = await _userService.GetDetailsAsync(id);
                if (userDetailsViewModel == null)
                {
                    SetError("The requested user was not found.");
                    return RedirectToAction(nameof(Index));
                }
                return View(userDetailsViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading details for user {id}");
                SetError("We couldn’t load the user details. Please try again.");
                return RedirectToAction(nameof(Index));
            }
        }
        // GET: /Users/Delete/{id}
        // Hybrid policy: Admin role OR Permission=DeleteUser
        [HttpGet]
        [Authorize(Policy = "DeleteUserPolicy")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var userDetailsViewModel = await _userService.GetDetailsAsync(id);
                if (userDetailsViewModel == null)
                {
                    SetError("The user you’re trying to delete was not found.");
                    return RedirectToAction(nameof(Index));
                }
                return View(userDetailsViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading delete confirmation for user {id}");
                SetError("We couldn’t load the delete confirmation. Please try again.");
                return RedirectToAction(nameof(Index));
            }
        }
        // POST: /Users/DeleteConfirmed
        // Hybrid policy: Admin role and must have Permission=DeleteUser
        [HttpPost]
        [Authorize(Policy = "DeleteUserPolicy")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            if (id == Guid.Empty) return NotFound();
            try
            {
                var result = await _userService.DeleteAsync(id);
                if (result.Succeeded)
                {
                    SetSuccess("User was deleted successfully.");
                    return RedirectToAction(nameof(Index));
                }
                if (result.Errors.Any(e => string.Equals(e.Code, "LastAdmin", StringComparison.OrdinalIgnoreCase)))
                {
                    SetError("You cannot delete the last user in the ‘Admin’ role.");
                }
                else if (result.Errors.Any(e => string.Equals(e.Code, "NotFound", StringComparison.OrdinalIgnoreCase)))
                {
                    SetError("The user no longer exists.");
                }
                else
                {
                    SetError(string.Join(" ", result.Errors.Select(e => e.Description)));
                }
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException dbx)
            {
                _logger.LogError(dbx, $"DB error while deleting user {id}");
                SetError("We couldn’t delete the user due to a database error. Please try again.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error deleting user {id}");
                SetError("An unexpected error occurred while deleting the user.");
                return RedirectToAction(nameof(Index));
            }
        }
        // GET: /Users/ManageRoles/{id}
        // Hybrid policy: Admin role AND Permission set (AddRole, EditRole, DeleteRole)
        [HttpGet]
        [Authorize(Policy = "ManageRolesPolicy")]
        public async Task<IActionResult> ManageRoles(Guid id)
        {
            try
            {
                var userRolesEditViewModel = await _userService.GetRolesForEditAsync(id);
                if (userRolesEditViewModel == null)
                {
                    SetError("The user was not found.");
                    return RedirectToAction(nameof(Index));
                }
                return View(userRolesEditViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading roles editor for user {id}");
                SetError("We couldn’t load the roles editor. Please try again.");
                return RedirectToAction(nameof(Index));
            }
        }
        // POST: /Users/ManageRoles
        // Hybrid policy: Admin role AND Permission set (AddRole, EditRole, DeleteRole)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "ManageRolesPolicy")]
        public async Task<IActionResult> ManageRoles(UserRolesEditViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            try
            {
                var selected = model.Roles.Where(r => r.IsSelected).Select(r => r.RoleId).ToList();
                var result = await _userService.UpdateRolesAsync(model.UserId, selected);
                if (result.Succeeded)
                {
                    SetSuccess("User roles were updated successfully.");
                    return RedirectToAction(nameof(Details), new { id = model.UserId });
                }
                if (result.Errors.Any(e => string.Equals(e.Code, "RoleNotFound", StringComparison.OrdinalIgnoreCase)))
                {
                    SetError("One or more selected roles no longer exist. Please refresh and try again.");
                }
                AddIdentityErrors(result);
                var userRolesEditViewModel = await _userService.GetRolesForEditAsync(model.UserId);
                return View(userRolesEditViewModel ?? model);
            }
            catch (DbUpdateException dbx)
            {
                _logger.LogError(dbx, $"DB error while updating roles for user {model.UserId}");
                SetError("We couldn’t update roles due to a database error. Please try again.");
                var vm = await _userService.GetRolesForEditAsync(model.UserId);
                return View(vm ?? model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error updating roles for user {model.UserId}");
                SetError("An unexpected error occurred while updating roles.");
                var vm = await _userService.GetRolesForEditAsync(model.UserId);
                return View(vm ?? model);
            }
        }
        // GET: /Users/ManageClaims/{id}
        // Hybrid policy: Admin role OR Permission=ManageUserClaims
        [HttpGet]
        [Authorize(Policy = "ManageUserClaimsPolicy")]
        public async Task<IActionResult> ManageClaims(Guid id)
        {
            var userClaimsEditViewModel = await _userService.GetClaimsForEditAsync(id);
            if (userClaimsEditViewModel == null)
            {
                SetError("The user was not found.");
                return RedirectToAction(nameof(Index));
            }
            return View(userClaimsEditViewModel);
        }
        // POST: /Users/ManageClaims
        // Hybrid policy: Admin role OR Permission=ManageUserClaims
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "ManageUserClaimsPolicy")]
        public async Task<IActionResult> ManageClaims(UserClaimsEditViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            try
            {
                var selected = model.Claims.Where(c => c.IsSelected).Select(c => c.ClaimId).ToList();
                var result = await _userService.UpdateClaimsAsync(model.UserId, selected);
                if (result.Succeeded)
                {
                    SetSuccess("User claims were updated successfully.");
                    return RedirectToAction(nameof(Details), new { id = model.UserId });
                }
                if (result.Errors.Any(e => string.Equals(e.Code, "InvalidClaimSelection", StringComparison.OrdinalIgnoreCase)))
                    SetError("One or more selected claims are not assignable to users. Please refresh and try again.");
                AddIdentityErrors(result);
                var reload = await _userService.GetClaimsForEditAsync(model.UserId);
                return View(reload ?? model);
            }
            catch (DbUpdateException dbx)
            {
                _logger.LogError(dbx, "DB error while updating claims for user {UserId}", model.UserId);
                SetError("We couldn’t update claims due to a database error. Please try again.");
                var reload = await _userService.GetClaimsForEditAsync(model.UserId);
                return View(reload ?? model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating claims for user {UserId}", model.UserId);
                SetError("An unexpected error occurred while updating claims.");
                var reload = await _userService.GetClaimsForEditAsync(model.UserId);
                return View(reload ?? model);
            }
        }
        #region Helpers
        // Helper: Push a success message into TempData (survives redirect)
        private void SetSuccess(string message)
        {
            TempData["Success"] = message;
        }
        // Helper: Push an error message into TempData (survives redirect)
        private void SetError(string message)
        {
            TempData["Error"] = message;
        }
        // Helper: Copy IdentityResult errors into ModelState 
        // so they can be displayed by validation summary in views.
        private void AddIdentityErrors(IdentityResult result)
        {
            if (result == null || result.Succeeded) return;
            foreach (var e in result.Errors)
                ModelState.AddModelError(string.Empty, e.Description);
        }
        #endregion
    }
}