using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PharmaSkincare.Data;
using PharmaSkincare.Models;
using PharmaSkincare.Services;

namespace PharmaSkincare.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CertificatesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileService _fileService;
        private readonly IActivityLogService _activityLog;
        private readonly UserManager<ApplicationUser> _userManager;

        public CertificatesController(ApplicationDbContext context, IFileService fileService,
            IActivityLogService activityLog, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _fileService = fileService;
            _activityLog = activityLog;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? filter)
        {
            var now = DateTime.UtcNow;
            var query = _context.Certificates.Include(c => c.Product).AsQueryable();

            if (filter == "expiring")
                query = query.Where(c => c.ExpiryDate <= now.AddDays(30) && c.ExpiryDate > now);
            else if (filter == "expired")
                query = query.Where(c => c.ExpiryDate <= now);

            var certs = await query.OrderBy(c => c.ExpiryDate).ToListAsync();
            ViewBag.Filter = filter;
            return View(certs);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateProductsList();
            return View(new Certificate { IssueDate = DateTime.UtcNow, ExpiryDate = DateTime.UtcNow.AddYears(1) });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Certificate model, IFormFile? certFile)
        {
            if (!ModelState.IsValid) { await PopulateProductsList(); return View(model); }

            if (certFile != null)
            {
                try { model.FilePath = await _fileService.SaveDocumentAsync(certFile, "certificates"); }
                catch (Exception ex) { ModelState.AddModelError("", ex.Message); await PopulateProductsList(); return View(model); }
            }

            model.CreatedDate = DateTime.UtcNow;
            _context.Certificates.Add(model);
            await _context.SaveChangesAsync();

            var user = await _userManager.GetUserAsync(User);
            if (user != null) await _activityLog.LogAsync(user.Id, "Certificate Created", "Certificate", model.CertificateId, model.CertificateName);

            TempData["Success"] = "Certificate added successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var cert = await _context.Certificates.FindAsync(id);
            if (cert == null) return NotFound();
            await PopulateProductsList();
            return View(cert);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Certificate model, IFormFile? certFile)
        {
            if (!ModelState.IsValid) { await PopulateProductsList(); return View(model); }

            var cert = await _context.Certificates.FindAsync(model.CertificateId);
            if (cert == null) return NotFound();

            cert.CertificateName = model.CertificateName;
            cert.IssuingOrganization = model.IssuingOrganization;
            cert.IssueDate = model.IssueDate;
            cert.ExpiryDate = model.ExpiryDate;
            cert.CertificateNumber = model.CertificateNumber;
            cert.CertificateType = model.CertificateType;
            cert.ProductId = model.ProductId;
            cert.IsActive = model.IsActive;

            if (certFile != null)
            {
                try { cert.FilePath = await _fileService.SaveDocumentAsync(certFile, "certificates"); }
                catch (Exception ex) { ModelState.AddModelError("", ex.Message); await PopulateProductsList(); return View(model); }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Certificate updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var cert = await _context.Certificates.FindAsync(id);
            if (cert == null) return NotFound();

            cert.IsActive = false;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Certificate deactivated.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateProductsList()
        {
            var products = await _context.Products.Include(p => p.Subcategory!).Where(p => p.IsActive).OrderBy(p => p.Subcategory!.Name).ToListAsync();
            ViewBag.Products = new SelectList(products.Select(p => new { p.ProductId, DisplayName = p.DisplayName }), "ProductId", "DisplayName");
        }
    }
}
