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
    public class MarketingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IActivityLogService _activityLog;
        private readonly IFileService _fileService;
        private readonly UserManager<ApplicationUser> _userManager;

        public MarketingController(ApplicationDbContext context, IActivityLogService activityLog,
            IFileService fileService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _activityLog = activityLog;
            _fileService = fileService;
            _userManager = userManager;
        }

        // --- CAMPAIGNS ---
        public async Task<IActionResult> Campaigns()
        {
            var campaigns = await _context.Campaigns.OrderByDescending(c => c.StartDate).ToListAsync();
            return View(campaigns);
        }

        [HttpGet]
        public IActionResult CreateCampaign() => View(new Campaign { StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(30) });

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCampaign(Campaign model, IFormFile? imageFile)
        {
            if (!ModelState.IsValid) return View(model);

            if (imageFile != null)
            {
                try { model.ImageUrl = await _fileService.SaveImageAsync(imageFile, "campaigns"); }
                catch (Exception ex) { ModelState.AddModelError("", ex.Message); return View(model); }
            }

            _context.Campaigns.Add(model);
            await _context.SaveChangesAsync();
            var user = await _userManager.GetUserAsync(User);
            if (user != null) await _activityLog.LogAsync(user.Id, "Campaign Created", "Campaign", model.CampaignId, model.Name);
            TempData["Success"] = "Campaign created successfully.";
            return RedirectToAction(nameof(Campaigns));
        }

        [HttpGet]
        public async Task<IActionResult> EditCampaign(int id)
        {
            var campaign = await _context.Campaigns.FindAsync(id);
            if (campaign == null) return NotFound();
            return View(campaign);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCampaign(Campaign model, IFormFile? imageFile)
        {
            if (!ModelState.IsValid) return View(model);

            var campaign = await _context.Campaigns.FindAsync(model.CampaignId);
            if (campaign == null) return NotFound();

            campaign.Name = model.Name;
            campaign.Description = model.Description;
            campaign.StartDate = model.StartDate;
            campaign.EndDate = model.EndDate;
            campaign.Budget = model.Budget;
            campaign.AmountSpent = model.AmountSpent;
            campaign.Status = model.Status;
            campaign.TargetAudience = model.TargetAudience;
            campaign.Platform = model.Platform;
            campaign.Impressions = model.Impressions;
            campaign.Clicks = model.Clicks;
            campaign.Conversions = model.Conversions;

            if (imageFile != null)
            {
                try { campaign.ImageUrl = await _fileService.SaveImageAsync(imageFile, "campaigns"); }
                catch (Exception ex) { ModelState.AddModelError("", ex.Message); return View(model); }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Campaign updated.";
            return RedirectToAction(nameof(Campaigns));
        }

        // --- DOCTORS ---
        public async Task<IActionResult> Doctors()
        {
            var doctors = await _context.Doctors.Include(d => d.Endorsements).OrderBy(d => d.Name).ToListAsync();
            return View(doctors);
        }

        [HttpGet]
        public IActionResult CreateDoctor() => View(new Doctor());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDoctor(Doctor model, IFormFile? imageFile)
        {
            if (!ModelState.IsValid) return View(model);

            if (imageFile != null)
            {
                try { model.ProfileImageUrl = await _fileService.SaveImageAsync(imageFile, "profiles"); }
                catch (Exception ex) { ModelState.AddModelError("", ex.Message); return View(model); }
            }

            _context.Doctors.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Dr. {model.Name} added successfully.";
            return RedirectToAction(nameof(Doctors));
        }

        [HttpGet]
        public async Task<IActionResult> EditDoctor(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null) return NotFound();
            return View(doctor);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDoctor(Doctor model, IFormFile? imageFile)
        {
            if (!ModelState.IsValid) return View(model);

            var doctor = await _context.Doctors.FindAsync(model.DoctorId);
            if (doctor == null) return NotFound();

            doctor.Name = model.Name;
            doctor.Specialty = model.Specialty;
            doctor.Qualification = model.Qualification;
            doctor.Email = model.Email;
            doctor.Phone = model.Phone;
            doctor.LicenseNumber = model.LicenseNumber;
            doctor.Hospital = model.Hospital;
            doctor.City = model.City;
            doctor.Country = model.Country;
            doctor.Bio = model.Bio;
            doctor.IsActive = model.IsActive;

            if (imageFile != null)
            {
                try { doctor.ProfileImageUrl = await _fileService.SaveImageAsync(imageFile, "profiles"); }
                catch (Exception ex) { ModelState.AddModelError("", ex.Message); return View(model); }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Doctor updated.";
            return RedirectToAction(nameof(Doctors));
        }

        // --- ENDORSEMENTS ---
        public async Task<IActionResult> Endorsements()
        {
            var endorsements = await _context.Endorsements
                .Include(e => e.Doctor)
                .Include(e => e.Product).ThenInclude(p => p!.Subcategory)
                .OrderByDescending(e => e.EndorsementDate)
                .ToListAsync();
            return View(endorsements);
        }

        [HttpGet]
        public async Task<IActionResult> CreateEndorsement()
        {
            ViewBag.Doctors = new SelectList(await _context.Doctors.Where(d => d.IsActive).ToListAsync(), "DoctorId", "Name");
            ViewBag.Products = new SelectList(await _context.Products.Include(p => p.Subcategory).Where(p => p.IsActive).ToListAsync(), "ProductId", "DisplayName");
            return View(new Endorsement { EndorsementDate = DateTime.UtcNow });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEndorsement(Endorsement model, IFormFile? documentFile)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Doctors = new SelectList(await _context.Doctors.Where(d => d.IsActive).ToListAsync(), "DoctorId", "Name");
                ViewBag.Products = new SelectList(await _context.Products.Include(p => p.Subcategory).Where(p => p.IsActive).ToListAsync(), "ProductId", "DisplayName");
                return View(model);
            }

            if (documentFile != null)
            {
                try { model.DocumentPath = await _fileService.SaveDocumentAsync(documentFile, "endorsements"); }
                catch (Exception ex) { ModelState.AddModelError("", ex.Message); return View(model); }
            }

            _context.Endorsements.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Endorsement added.";
            return RedirectToAction(nameof(Endorsements));
        }

        // --- PHARMACY PARTNERS ---
        public async Task<IActionResult> Partners()
        {
            var partners = await _context.PharmacyPartners.OrderBy(p => p.Name).ToListAsync();
            return View(partners);
        }

        [HttpGet]
        public IActionResult CreatePartner() => View(new PharmacyPartner { AgreementDate = DateTime.UtcNow });

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePartner(PharmacyPartner model)
        {
            if (!ModelState.IsValid) return View(model);
            _context.PharmacyPartners.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Partner '{model.Name}' added.";
            return RedirectToAction(nameof(Partners));
        }

        [HttpGet]
        public async Task<IActionResult> EditPartner(int id)
        {
            var partner = await _context.PharmacyPartners.FindAsync(id);
            if (partner == null) return NotFound();
            return View(partner);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPartner(PharmacyPartner model)
        {
            if (!ModelState.IsValid) return View(model);

            var partner = await _context.PharmacyPartners.FindAsync(model.PharmacyPartnerId);
            if (partner == null) return NotFound();

            partner.Name = model.Name;
            partner.ContactPerson = model.ContactPerson;
            partner.Email = model.Email;
            partner.Phone = model.Phone;
            partner.Address = model.Address;
            partner.City = model.City;
            partner.Country = model.Country;
            partner.AgreementDate = model.AgreementDate;
            partner.AgreementExpiry = model.AgreementExpiry;
            partner.CommissionRate = model.CommissionRate;
            partner.Status = model.Status;
            partner.Notes = model.Notes;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Partner updated.";
            return RedirectToAction(nameof(Partners));
        }
    }
}
