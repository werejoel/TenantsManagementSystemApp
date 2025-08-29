using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TenantsManagementApp.Data;
using TenantsManagementApp.Models;

namespace TenantsManagementApp.Controllers
{
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Payments
        public async Task<IActionResult> Index()
        {
            var payments = _context.Payments
                .Include(p => p.House)
                .Include(p => p.Tenant)
                .AsNoTracking();
            return View(await payments.ToListAsync());
        }

        // GET: Payments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
                .Include(p => p.House)
                .Include(p => p.Tenant)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // GET: Payments/Create
        public IActionResult Create()
        {
            var tenants = _context.Tenants.Include(t => t.User).ToList();
            var houses = _context.Houses.ToList();

            ViewBag.TenantId = new SelectList(tenants, "Id", "FullName");
            ViewBag.HouseId = new SelectList(houses, "Id", "Name");
            return View();
        }

        // POST: Payments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TenantId,HouseId,AmountPaid,PaymentDate,PeriodStart,PeriodEnd,PaymentMethod,TransactionReference,Notes")] Payment payment)
        {
            var tenants = _context.Tenants.Include(t => t.User).ToList();
            var houses = _context.Houses.ToList();

            ViewBag.TenantId = new SelectList(tenants, "Id", "FullName", payment.TenantId);
            ViewBag.HouseId = new SelectList(houses, "Id", "Name", payment.HouseId);

            // No need for manual ModelState.AddModelError, let DataAnnotations handle it

            payment.CreatedAt = DateTime.Now;
            payment.UpdatedAt = DateTime.Now;

            if (ModelState.IsValid)
            {
                _context.Add(payment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Payment created successfully.";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = "Invalid payment data. Please check the form and try again.";
            return View(payment);
        }

        // GET: Payments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (payment == null)
            {
                return NotFound();
            }

            var tenants = _context.Tenants.Include(t => t.User).AsNoTracking().ToList();
            var houses = _context.Houses.AsNoTracking().ToList();

            if (!tenants.Any() || !houses.Any())
            {
                TempData["ErrorMessage"] = "Cannot edit payment: No tenants or houses available.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.TenantId = new SelectList(tenants, "Id", "FullName", payment.TenantId);
            ViewBag.HouseId = new SelectList(houses, "Id", "Name", payment.HouseId);
            return View(payment);
        }

        // POST: Payments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,TenantId,HouseId,AmountPaid,PaymentDate,PeriodStart,PeriodEnd,PaymentMethod,TransactionReference,Notes,CreatedAt")] Payment payment)
        {
            if (id != payment.Id)
            {
                return NotFound();
            }

            var tenants = _context.Tenants.Include(t => t.User).AsNoTracking().ToList();
            var houses = _context.Houses.AsNoTracking().ToList();

            ViewBag.TenantId = new SelectList(tenants, "Id", "FullName", payment.TenantId);
            ViewBag.HouseId = new SelectList(houses, "Id", "Name", payment.HouseId);

            // Validate foreign keys
            if (payment.TenantId > 0 && !_context.Tenants.Any(t => t.Id == payment.TenantId))
            {
                ModelState.AddModelError("TenantId", "Selected tenant does not exist.");
            }
            if (payment.HouseId > 0 && !_context.Houses.Any(h => h.Id == payment.HouseId))
            {
                ModelState.AddModelError("HouseId", "Selected house does not exist.");
            }

            // Validate period dates if provided
            if (payment.PeriodStart.HasValue && payment.PeriodEnd.HasValue && payment.PeriodEnd < payment.PeriodStart)
            {
                ModelState.AddModelError("PeriodEnd", "Period End cannot be before Period Start.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    payment.UpdatedAt = DateTime.Now;
                    _context.Update(payment);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Payment updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PaymentExists(payment.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                catch (DbUpdateException ex)
                {
                    Console.WriteLine($"DbUpdateException: {ex.InnerException?.Message ?? ex.Message}");
                    ModelState.AddModelError("", "An error occurred while updating the payment. Please ensure all data is valid.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected error: {ex.Message}");
                    ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                }
            }
            else
            {
                // Log ModelState errors
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                Console.WriteLine("ModelState Errors: " + string.Join("; ", errors));
            }

            return View(payment);
        }

        // GET: Payments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
                .Include(p => p.House)
                .Include(p => p.Tenant)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // POST: Payments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var payment = await _context.Payments.FindAsync(id);
                if (payment == null)
                {
                    TempData["ErrorMessage"] = "Payment not found.";
                }
                else
                {
                    _context.Payments.Remove(payment);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Payment deleted successfully.";
                }
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"DbUpdateException: {ex.InnerException?.Message ?? ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while deleting the payment.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                TempData["ErrorMessage"] = "An unexpected error occurred.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool PaymentExists(int id)
        {
            return _context.Payments.Any(e => e.Id == id);
        }
    }
}