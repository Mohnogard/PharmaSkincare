using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PharmaSkincare.Models;

namespace PharmaSkincare.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Subcategory> Subcategories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Shipment> Shipments { get; set; }
        public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Endorsement> Endorsements { get; set; }
        public DbSet<PharmacyPartner> PharmacyPartners { get; set; }
        public DbSet<Campaign> Campaigns { get; set; }
        public DbSet<Certificate> Certificates { get; set; }
        public DbSet<ProductReview> ProductReviews { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<WishlistItem> WishlistItems { get; set; }
        public DbSet<Testimonial> Testimonials { get; set; }
        public DbSet<ContactMessage> ContactMessages { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Category → Subcategories
            builder.Entity<Subcategory>()
                .HasOne(s => s.Category)
                .WithMany(c => c.Subcategories)
                .HasForeignKey(s => s.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Subcategory → Products
            builder.Entity<Product>()
                .HasOne(p => p.Subcategory)
                .WithMany(s => s.Products)
                .HasForeignKey(p => p.SubcategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Product → OrderItems
            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Order → OrderItems
            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // User → Orders
            builder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Order → Shipment
            builder.Entity<Shipment>()
                .HasOne(s => s.Order)
                .WithOne(o => o.Shipment)
                .HasForeignKey<Shipment>(s => s.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Product → InventoryTransactions
            builder.Entity<InventoryTransaction>()
                .HasOne(it => it.Product)
                .WithMany(p => p.InventoryTransactions)
                .HasForeignKey(it => it.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // User → InventoryTransactions
            builder.Entity<InventoryTransaction>()
                .HasOne(it => it.User)
                .WithMany(u => u.InventoryTransactions)
                .HasForeignKey(it => it.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Doctor → Endorsements
            builder.Entity<Endorsement>()
                .HasOne(e => e.Doctor)
                .WithMany(d => d.Endorsements)
                .HasForeignKey(e => e.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Product → Endorsements
            builder.Entity<Endorsement>()
                .HasOne(e => e.Product)
                .WithMany(p => p.Endorsements)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Product → Certificates (optional FK)
            builder.Entity<Certificate>()
                .HasOne(c => c.Product)
                .WithMany(p => p.Certificates)
                .HasForeignKey(c => c.ProductId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // Product → Reviews
            builder.Entity<ProductReview>()
                .HasOne(r => r.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProductReview>()
                .HasOne(r => r.Customer)
                .WithMany()
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // WishlistItem
            builder.Entity<WishlistItem>()
                .HasOne(w => w.Customer).WithMany().HasForeignKey(w => w.CustomerId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<WishlistItem>()
                .HasOne(w => w.Product).WithMany().HasForeignKey(w => w.ProductId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<WishlistItem>()
                .HasIndex(w => new { w.CustomerId, w.ProductId }).IsUnique();

            // Testimonial → User (optional)
            builder.Entity<Testimonial>()
                .HasOne(t => t.Customer).WithMany()
                .HasForeignKey(t => t.CustomerId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // ActivityLog → User
            builder.Entity<ActivityLog>()
                .HasOne(a => a.User)
                .WithMany(u => u.ActivityLogs)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Product → ProductImages
            builder.Entity<ProductImage>()
                .HasOne(pi => pi.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraints
            builder.Entity<Product>()
                .HasIndex(p => p.SKU)
                .IsUnique();

            builder.Entity<Shipment>()
                .HasIndex(s => s.TrackingNumber)
                .IsUnique();

            // Decimal precision
            builder.Entity<Order>()
                .Ignore(o => o.GrandTotal);

            builder.Entity<OrderItem>()
                .Ignore(oi => oi.SubTotal);
        }
    }
}
