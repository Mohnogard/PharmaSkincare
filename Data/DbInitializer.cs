using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PharmaSkincare.Models;

namespace PharmaSkincare.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Seeder");
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            await context.Database.MigrateAsync();

            // Seed roles
            string[] roles = { "Admin", "Customer", "Demo" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // Seed admin user
            var adminPassword = config["Seed:AdminPassword"];
            if (string.IsNullOrWhiteSpace(adminPassword))
            {
                logger.LogWarning("Seed:AdminPassword not set, skipping admin user.");
            }
            else if (await userManager.FindByEmailAsync("admin@pharmaskincare.com") == null)   // admin@nexusgear.com
            {
                var admin = new ApplicationUser { /* same fields as now */ };
                var result = await userManager.CreateAsync(admin, adminPassword);
                if (result.Succeeded) await userManager.AddToRoleAsync(admin, "Admin");
                else logger.LogError("Admin seed failed: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
            if (await userManager.FindByEmailAsync("demo-admin@pharmaskincare.com") == null)   // demo-admin@nexusgear.com
            {
                var demo = new ApplicationUser
                {
                    UserName = "demo-admin@pharmaskincare.com",
                    Email = "demo-admin@pharmaskincare.com",
                    FirstName = "Demo",
                    LastName = "Admin",
                    EmailConfirmed = true,
                    IsActive = true,                 // PharmaSkincare only, remove in NexusGear
                    CreatedDate = DateTime.UtcNow    // PharmaSkincare only, remove in NexusGear
                };
                var result = await userManager.CreateAsync(demo, "Demo@2026");
                if (result.Succeeded) await userManager.AddToRolesAsync(demo, new[] { "Admin", "Demo" });
                else logger.LogError("Demo admin seed failed: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            // Seed demo customer
            if (await userManager.FindByEmailAsync("customer@example.com") == null)
            {
                var customer = new ApplicationUser
                {
                    UserName = "customer@example.com",
                    Email = "customer@example.com",
                    FirstName = "Emma",
                    LastName = "Thompson",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(customer, "Customer@123456");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(customer, "Customer");
            }

            // Seed categories
            if (!context.Categories.Any())
            {
                var categories = new List<Category>
                {
                    new() { Name = "Magnesium", Description = "Magnesium-based topical wellness products", IsActive = true },
                    new() { Name = "Zinc", Description = "Zinc-infused skincare and recovery products", IsActive = true },
                    new() { Name = "Recovery", Description = "Post-workout and muscle recovery products", IsActive = true },
                    new() { Name = "Sleep Support", Description = "Products formulated to support restful sleep", IsActive = true },
                    new() { Name = "Wellness", Description = "General wellness and everyday skincare", IsActive = true }
                };
                context.Categories.AddRange(categories);
                await context.SaveChangesAsync();
            }

            // Seed subcategories + products
            if (!context.Subcategories.Any())
            {
                var magCatId     = context.Categories.First(c => c.Name == "Magnesium").CategoryId;
                var recoveryCatId = context.Categories.First(c => c.Name == "Recovery").CategoryId;
                var sleepCatId   = context.Categories.First(c => c.Name == "Sleep Support").CategoryId;
                var wellnessCatId = context.Categories.First(c => c.Name == "Wellness").CategoryId;
                var zincCatId    = context.Categories.First(c => c.Name == "Zinc").CategoryId;

                // Each subcategory = one product "line". Products = individual sizes.
                var subcategories = new List<Subcategory>
                {
                    new() { CategoryId = magCatId, Name = "MagEase Roll-On", SizeType = SizeType.Ml,
                        FormulaType = "Magnesium Chloride Hexahydrate", ProductFormat = ProductFormat.RollOn,
                        HealthBenefits = "Muscle Recovery, Pain Relief, Joint Health", IsActive = true,
                        Description = "Targeted muscle relief. Smooth, non-sticky and fast-absorbing." },

                    new() { CategoryId = magCatId, Name = "MagEase Spray", SizeType = SizeType.Ml,
                        FormulaType = "Magnesium Chloride", ProductFormat = ProductFormat.Spray,
                        HealthBenefits = "Muscle Recovery, Stress Relief", IsActive = true,
                        Description = "Ultra-fine mist magnesium spray for full-body application. Enhanced with aloe vera." },

                    new() { CategoryId = magCatId, Name = "MagPro Sports Spray", SizeType = SizeType.Ml,
                        FormulaType = "Magnesium Chloride + Menthol", ProductFormat = ProductFormat.Spray,
                        HealthBenefits = "Muscle Recovery, Pain Relief", IsActive = true,
                        Description = "High-concentration sports formula with cooling menthol for athletes and active users." },

                    new() { CategoryId = recoveryCatId, Name = "RecoveryPlus Cream", SizeType = SizeType.Ml,
                        FormulaType = "Magnesium + Arnica", ProductFormat = ProductFormat.Cream,
                        HealthBenefits = "Muscle Recovery, Pain Relief, Joint Health", IsActive = true,
                        Description = "Professional-grade recovery cream combining transdermal magnesium with arnica extract." },

                    new() { CategoryId = sleepCatId, Name = "DreamEase Sleep Oil", SizeType = SizeType.Ml,
                        FormulaType = "Magnesium + Lavender", ProductFormat = ProductFormat.Oil,
                        HealthBenefits = "Sleep Support, Stress Relief", IsActive = true,
                        Description = "Calming bedtime oil infused with magnesium and lavender for deeper, more restful sleep." },

                    new() { CategoryId = zincCatId, Name = "ZincShield Gel", SizeType = SizeType.Ml,
                        FormulaType = "Zinc Pyrithione + Vitamin E", ProductFormat = ProductFormat.Gel,
                        HealthBenefits = "Skin Health", IsActive = true,
                        Description = "Daily zinc protective gel for skin barrier support. Lightweight and non-greasy." },

                    new() { CategoryId = wellnessCatId, Name = "WellnessBoost Patch", SizeType = SizeType.None,
                        FormulaType = "Transdermal Magnesium", ProductFormat = ProductFormat.Patch,
                        HealthBenefits = "Stress Relief, Sleep Support, Muscle Recovery", IsActive = true,
                        Description = "24-hour release magnesium patches for sustained wellness support." },

                    new() { CategoryId = wellnessCatId, Name = "SkinRestore Serum", SizeType = SizeType.Ml,
                        FormulaType = "Magnesium + Hyaluronic Acid", ProductFormat = ProductFormat.Serum,
                        HealthBenefits = "Skin Health, Stress Relief", IsActive = true,
                        Description = "Premium anti-aging serum combining magnesium with hyaluronic acid." }
                };
                context.Subcategories.AddRange(subcategories);
                await context.SaveChangesAsync();

                // Products = individual sizes per subcategory
                var rollOnId    = context.Subcategories.First(s => s.Name == "MagEase Roll-On").SubcategoryId;
                var sprayId     = context.Subcategories.First(s => s.Name == "MagEase Spray").SubcategoryId;
                var sportsId    = context.Subcategories.First(s => s.Name == "MagPro Sports Spray").SubcategoryId;
                var creamId     = context.Subcategories.First(s => s.Name == "RecoveryPlus Cream").SubcategoryId;
                var oilId       = context.Subcategories.First(s => s.Name == "DreamEase Sleep Oil").SubcategoryId;
                var gelId       = context.Subcategories.First(s => s.Name == "ZincShield Gel").SubcategoryId;
                var patchId     = context.Subcategories.First(s => s.Name == "WellnessBoost Patch").SubcategoryId;
                var serumId     = context.Subcategories.First(s => s.Name == "SkinRestore Serum").SubcategoryId;

                var products = new List<Product>
                {
                    new() { SubcategoryId = rollOnId,  SKU = "MAG-RO-075", Size = "75ml",  Price = 24.99m, StockQuantity = 150, MinimumStockLevel = 20, IsActive = true },
                    new() { SubcategoryId = rollOnId,  SKU = "MAG-RO-150", Size = "150ml", Price = 39.99m, StockQuantity = 80,  MinimumStockLevel = 15, IsActive = true },
                    new() { SubcategoryId = sprayId,   SKU = "MAG-SP-100", Size = "100ml", Price = 19.99m, StockQuantity = 8,   MinimumStockLevel = 15, IsActive = true },
                    new() { SubcategoryId = sprayId,   SKU = "MAG-SP-200", Size = "200ml", Price = 29.99m, StockQuantity = 60,  MinimumStockLevel = 15, IsActive = true },
                    new() { SubcategoryId = sportsId,  SKU = "MAG-SPO-200", Size = "200ml", Price = 27.99m, StockQuantity = 75, MinimumStockLevel = 15, IsActive = true },
                    new() { SubcategoryId = creamId,   SKU = "REC-CR-150", Size = "150g",  Price = 32.99m, StockQuantity = 95,  MinimumStockLevel = 15, IsActive = true },
                    new() { SubcategoryId = oilId,     SKU = "SLP-OL-050", Size = "50ml",  Price = 29.99m, StockQuantity = 60,  MinimumStockLevel = 10, IsActive = true },
                    new() { SubcategoryId = gelId,     SKU = "ZNC-GE-100", Size = "100ml", Price = 18.99m, StockQuantity = 5,   MinimumStockLevel = 10, IsActive = true },
                    new() { SubcategoryId = patchId,   SKU = "WEL-PT-012", Size = null,    Price = 39.99m, StockQuantity = 120, MinimumStockLevel = 25, IsActive = true },
                    new() { SubcategoryId = serumId,   SKU = "WEL-SR-030", Size = "30ml",  Price = 45.99m, StockQuantity = 40,  MinimumStockLevel = 8,  IsActive = true },
                    new() { SubcategoryId = serumId,   SKU = "WEL-SR-050", Size = "50ml",  Price = 69.99m, StockQuantity = 25,  MinimumStockLevel = 8,  IsActive = true },
                };
                context.Products.AddRange(products);
                await context.SaveChangesAsync();
            }

            // Seed doctors
            if (!context.Doctors.Any())
            {
                var doctors = new List<Doctor>
                {
                    new() { Name = "Dr. Layla Al-Khatib", Specialty = "Dermatology", Qualification = "MD, JBA",
                        Email = "l.alkhatib@juh.jo", Phone = "+962 6 535 1000",
                        LicenseNumber = "DRM-2018-0031", Hospital = "Jordan University Hospital",
                        City = "Amman", Country = "Jordan", IsActive = true,
                        Bio = "Board-certified dermatologist with 14 years of clinical experience in topical therapeutics and skin wellness." },

                    new() { Name = "Dr. Tariq Mansour", Specialty = "Sports Medicine", Qualification = "MD, CAQSM",
                        Email = "t.mansour@rjmh.jo", Phone = "+962 6 560 2200",
                        LicenseNumber = "SPM-2015-0089", Hospital = "Royal Jordanian Medical Hospital",
                        City = "Amman", Country = "Jordan", IsActive = true,
                        Bio = "Sports medicine physician specialising in muscle recovery, magnesium therapy, and athletic rehabilitation." },

                    new() { Name = "Dr. Hana Abu-Zeid", Specialty = "Integrative Medicine", Qualification = "MBBS, FJMC",
                        Email = "h.abuzeid@integrativehealth.jo", Phone = "+962 3 215 0770",
                        LicenseNumber = "INT-2020-0055", Hospital = "Aqaba Integrative Health Clinic",
                        City = "Aqaba", Country = "Jordan", IsActive = true,
                        Bio = "Integrative medicine practitioner with expertise in mineral supplementation, holistic skincare, and wellness protocols." }
                };
                context.Doctors.AddRange(doctors);
                await context.SaveChangesAsync();
            }

            // Seed pharmacy partners
            if (!context.PharmacyPartners.Any())
            {
                var partners = new List<PharmacyPartner>
                {
                    new() { Name = "Al-Hikma Pharmacy", ContactPerson = "Samir Eid",
                        Email = "orders@alhikma-pharmacy.jo", Phone = "+962 6 464 2000",
                        Address = "14 Wasfi Al-Tal Street", City = "Amman", Country = "Jordan",
                        AgreementDate = DateTime.UtcNow.AddMonths(-6), AgreementExpiry = DateTime.UtcNow.AddMonths(6),
                        CommissionRate = 12.5m, Status = PartnershipStatus.Active },

                    new() { Name = "Dar Al-Shifa Wellness", ContactPerson = "Nour Haddad",
                        Email = "partnerships@daralshifa.jo", Phone = "+962 6 581 3100",
                        Address = "7 Queen Noor Street", City = "Amman", Country = "Jordan",
                        AgreementDate = DateTime.UtcNow.AddMonths(-3), AgreementExpiry = DateTime.UtcNow.AddMonths(9),
                        CommissionRate = 10.0m, Status = PartnershipStatus.Active }
                };
                context.PharmacyPartners.AddRange(partners);
                await context.SaveChangesAsync();
            }

            // Seed demo partner-linked pharmacy (independent of other pharmacy seeding)
            if (!context.PharmacyPartners.Any(p => p.Email == "partner@pharmaskincare.com"))
            {
                context.PharmacyPartners.Add(new PharmacyPartner
                {
                    Name = "Zain Health Pharmacy",
                    ContactPerson = "Ahmad Zain",
                    Email = "partner@pharmaskincare.com",
                    Phone = "+962 3 201 5500",
                    Address = "18 King Hussein Street",
                    City = "Aqaba",
                    Country = "Jordan",
                    AgreementDate = DateTime.UtcNow.AddMonths(-1),
                    AgreementExpiry = DateTime.UtcNow.AddMonths(11),
                    CommissionRate = 11.0m,
                    Status = PartnershipStatus.Active
                });
                await context.SaveChangesAsync();
            }

            // Seed campaigns
            if (!context.Campaigns.Any())
            {
                var campaigns = new List<Campaign>
                {
                    new() { Name = "Summer Recovery Campaign", Description = "Target athletes and sports enthusiasts during peak summer season.",
                        StartDate = DateTime.UtcNow.AddDays(-30), EndDate = DateTime.UtcNow.AddDays(60),
                        Budget = 5000m, AmountSpent = 1800m, Status = CampaignStatus.Active,
                        TargetAudience = "Athletes 18-45", Platform = "Instagram, Google Ads",
                        Impressions = 45000, Clicks = 2300, Conversions = 180 },

                    new() { Name = "Sleep Wellness Winter Drive", Description = "Promote sleep support products ahead of the winter wellness season.",
                        StartDate = DateTime.UtcNow.AddDays(15), EndDate = DateTime.UtcNow.AddDays(90),
                        Budget = 3500m, AmountSpent = 0m, Status = CampaignStatus.Draft,
                        TargetAudience = "Adults 30-60", Platform = "Facebook, Email" }
                };
                context.Campaigns.AddRange(campaigns);
                await context.SaveChangesAsync();
            }

            // Seed certificates
            if (!context.Certificates.Any())
            {
                var magProductId = context.Products.FirstOrDefault(p => p.SKU == "MAG-RO-001")?.ProductId;
                var certs = new List<Certificate>
                {
                    new() { CertificateName = "ISO 22716 - Cosmetics GMP", IssuingOrganization = "Jordan Institution for Standards and Metrology",
                        IssueDate = DateTime.UtcNow.AddYears(-1), ExpiryDate = DateTime.UtcNow.AddMonths(8),
                        CertificateNumber = "BSI-22716-2024-001", CertificateType = "Manufacturing", IsActive = true },

                    new() { CertificateName = "Dermatologically Tested", IssuingOrganization = "DermTest Labs",
                        IssueDate = DateTime.UtcNow.AddMonths(-8), ExpiryDate = DateTime.UtcNow.AddMonths(4),
                        CertificateNumber = "DT-2024-0892", CertificateType = "Safety", ProductId = magProductId, IsActive = true },

                    new() { CertificateName = "Cruelty-Free Certification", IssuingOrganization = "Leaping Bunny",
                        IssueDate = DateTime.UtcNow.AddYears(-2), ExpiryDate = DateTime.UtcNow.AddYears(1),
                        CertificateNumber = "LB-CF-2023-445", CertificateType = "Ethics", IsActive = true },

                    new() { CertificateName = "Jordan FDA - Product Safety Approval", IssuingOrganization = "Jordan Food and Drug Administration",
                        IssueDate = DateTime.UtcNow.AddDays(-20), ExpiryDate = DateTime.UtcNow.AddDays(10),
                        CertificateNumber = "JFDA-2024-1102", CertificateType = "Regulatory", IsActive = true }
                };
                context.Certificates.AddRange(certs);
                await context.SaveChangesAsync();
            }
        }
    }
}
