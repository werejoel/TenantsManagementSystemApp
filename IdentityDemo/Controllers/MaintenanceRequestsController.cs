using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TenantsManagementApp.Data;
using TenantsManagementApp.Models;

namespace TenantsManagementApp.Controllers
{
    public class MaintenanceRequestsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MaintenanceRequestsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: MaintenanceRequests
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.MaintenanceRequests.Include(m => m.House).Include(m => m.Tenant);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: MaintenanceRequests/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var maintenanceRequest = await _context.MaintenanceRequests
                .Include(m => m.House)
                .Include(m => m.Tenant)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (maintenanceRequest == null)
            {
                return NotFound();
            }

            return View(maintenanceRequest);
        }

        // GET: MaintenanceRequests/Create
        public IActionResult Create()
        {
            ViewData["HouseId"] = new SelectList(_context.Houses, "Id", "Name");
            ViewData["TenantId"] = new SelectList(_context.Tenants, "Id", "Id");
            return View();
        }

        // POST: MaintenanceRequests/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TenantId,HouseId,Title,Description,Priority,Status,ManagerNotes,RequestedAt,CompletedAt,Id,CreatedAt,UpdatedAt")] MaintenanceRequest maintenanceRequest)
        {
            if (ModelState.IsValid)
            {
                _context.Add(maintenanceRequest);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["HouseId"] = new SelectList(_context.Houses, "Id", "Name", maintenanceRequest.HouseId);
            ViewData["TenantId"] = new SelectList(_context.Tenants, "Id", "Id", maintenanceRequest.TenantId);
            return View(maintenanceRequest);
        }

        // GET: MaintenanceRequests/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var maintenanceRequest = await _context.MaintenanceRequests.FindAsync(id);
            if (maintenanceRequest == null)
            {
                return NotFound();
            }
            ViewData["HouseId"] = new SelectList(_context.Houses, "Id", "Name", maintenanceRequest.HouseId);
            ViewData["TenantId"] = new SelectList(_context.Tenants, "Id", "Id", maintenanceRequest.TenantId);
            return View(maintenanceRequest);
        }

        // POST: MaintenanceRequests/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("TenantId,HouseId,Title,Description,Priority,Status,ManagerNotes,RequestedAt,CompletedAt,Id,CreatedAt,UpdatedAt")] MaintenanceRequest maintenanceRequest)
        {
            if (id != maintenanceRequest.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(maintenanceRequest);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MaintenanceRequestExists(maintenanceRequest.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["HouseId"] = new SelectList(_context.Houses, "Id", "Name", maintenanceRequest.HouseId);
            ViewData["TenantId"] = new SelectList(_context.Tenants, "Id", "Id", maintenanceRequest.TenantId);
            return View(maintenanceRequest);
        }

        // GET: MaintenanceRequests/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var maintenanceRequest = await _context.MaintenanceRequests
                .Include(m => m.House)
                .Include(m => m.Tenant)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (maintenanceRequest == null)
            {
                return NotFound();
            }

            return View(maintenanceRequest);
        }

        // POST: MaintenanceRequests/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var maintenanceRequest = await _context.MaintenanceRequests.FindAsync(id);
            if (maintenanceRequest != null)
            {
                _context.MaintenanceRequests.Remove(maintenanceRequest);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MaintenanceRequestExists(int id)
        {
            return _context.MaintenanceRequests.Any(e => e.Id == id);
        }
    }
}
