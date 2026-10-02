# PharmaSkincare — Mineral Skincare E-Commerce Platform

- Full-stack e-commerce platform for magnesium- and zinc-based skincare products, with a customer storefront and an admin back office for inventory, orders, shipments and marketing.

<img width="1885" height="850" alt="Pharma0" src="https://github.com/user-attachments/assets/b6c03a95-4277-413f-96b3-df3faf7ec755" />

## Features
- Product catalog with categories, product lines and size variants
- Cart, wishlist and Stripe checkout (test mode)
- Sign-in with email, Google or Facebook (ASP.NET Core Identity, role-based access)
- Admin back office: inventory, orders, shipments, users and roles, reports
- Excel inventory export (ClosedXML) and email notifications (MailKit)
- Activity log of admin actions

- Admin dashboard

<img width="1886" height="863" alt="Pharma2" src="https://github.com/user-attachments/assets/df38b57a-ce84-4d9d-825d-b27c2cc63c67" />

## Tech stack
ASP.NET Core MVC (.NET 10) · C# · Entity Framework Core · SQL Server · ASP.NET Core Identity · Stripe · MailKit · ClosedXML · Bootstrap · xUnit

## Architecture
Controllers → Services (business logic) → EF Core `ApplicationDbContext` → SQL Server.
Services are registered with dependency injection in `Program.cs`; the cart is stored in the session.

## Running locally

**Prerequisites:** .NET 10 SDK, SQL Server Express or LocalDB, Visual Studio 2026 or the `dotnet` CLI

```bash
git clone https://github.com/Mohnogard/PharmaSkincare.git
cd PharmaSkincare
```

1. **Database:** check the connection string in `appsettings.json` (defaults to `.\SQLEXPRESS`).
   The database is created and seeded automatically on first run.
2. **Stripe test keys** (optional, needed for checkout). Get free test keys at dashboard.stripe.com:
```bash
   dotnet user-secrets set "Stripe:PublishableKey" "pk_test_..."
   dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
```
   Pay with test card `4242 4242 4242 4242`, any future date, any CVC.
3. **Run:**
```bash
   dotnet run
```
   Open https://localhost:7012

**Demo accounts (seeded):** Admin `admin@pharmaskincare.com` · Customer `customer@example.com` (passwords: see `Data/DbInitializer.cs`)

## Tests
```bash
dotnet test
```
