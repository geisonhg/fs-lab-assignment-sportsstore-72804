# SportsStore

A modernised ASP.NET Core web application built as part of the Full Stack Development module assignment. The project started life as a .NET 6 example from *Pro ASP.NET Core 6* and has been upgraded, extended, and hardened to meet professional development standards.

The store lets customers browse sports products, add them to a cart, and pay using Stripe. Everything is logged with Serilog, and the build is validated automatically on every push through a GitHub Actions CI pipeline.

---

## What was done and why

The original codebase was a clean starting point — repository pattern, Entity Framework, pagination — but it needed a lot of work to be production-ready. Here is a summary of what changed and the reasoning behind each decision.

### Upgraded to .NET 8

The first thing to tackle was the framework version. .NET 6 reached end of support, so staying on it is not realistic for any serious project. .NET 8 is the current Long-Term Support release, which means security patches and support for years to come. The upgrade was straightforward — update the target framework in both `.csproj` files, bump the NuGet packages to their .NET 8 equivalents, and remove the `global.json` SDK pin that was locking the project to an old toolchain. All existing tests passed without any changes to the test logic itself.

### Structured logging with Serilog

The default `ILogger` setup in .NET is fine for simple cases, but it does not give you much control over output format or sinks. Serilog fixes that. It writes to the console in a human-readable format during development and to a daily rolling log file for anything that needs to be kept. Each log entry is enriched with the machine name and environment name automatically, which makes it much easier to trace issues when you have more than one environment running.

The logging covers the full application lifecycle — startup, HTTP requests, cart operations, checkout events, Stripe interactions, and any exceptions that occur. All log calls use structured properties rather than string interpolation, so the data stays queryable if you ever hook up a log aggregator.

### Stripe payment integration

Integrating payments is always the part that needs the most care. The approach taken here uses Stripe Checkout Sessions — the user fills in their shipping details on our side, then gets redirected to Stripe's hosted payment page, and Stripe redirects them back when the payment is done. This means we never touch card data directly, which removes a huge amount of PCI compliance burden.

The secret API key is never stored in any file. It lives in .NET User Secrets locally and in GitHub Actions Secrets for CI. The only thing in `appsettings.json` is the placeholder structure with empty values, which documents that the keys are expected without exposing them. On the success callback, the application verifies the Stripe session status before saving anything to the database — a payment that was abandoned or failed does not create an order.

### GitHub Actions CI

The pipeline runs on every push and pull request targeting `main`. It restores packages, builds in Release mode, and runs the full test suite. If a test fails, the build fails. There is nothing clever about it — simple pipelines are easier to debug and easier to trust. Test results are uploaded as artifacts so they can be inspected after the run.

---

## How to run this locally

You will need:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- SQL Server or SQL Server LocalDB (comes with Visual Studio)
- A free [Stripe account](https://stripe.com) for test keys

**Clone and set up secrets**

```bash
git clone https://github.com/geisonhg/fs-lab-assignment-sportsstore-72804.git
cd fs-lab-assignment-sportsstore-72804
```

The application reads its Stripe keys from .NET User Secrets. Run these two commands to set them up — replace the placeholders with your own keys from the Stripe dashboard under Developers → API Keys:

```bash
dotnet user-secrets set "Stripe:PublishableKey" "pk_test_YOUR_KEY_HERE" --project SportsStore/SportsStore.csproj
dotnet user-secrets set "Stripe:SecretKey" "sk_test_YOUR_KEY_HERE" --project SportsStore/SportsStore.csproj
```

User Secrets are stored outside the project folder on your machine, so there is no risk of accidentally committing them.

**Check the connection string**

Open `SportsStore/appsettings.json` and check the `SportsStoreConnection` value. The default points to SQL Server LocalDB:

```
Server=(localdb)\MSSQLLocalDB;Database=SportsStore;MultipleActiveResultSets=true
```

If you are using a different SQL Server instance, update the connection string here. Do not commit real credentials — use environment variables or a local `appsettings.Development.json` (which is in `.gitignore`).

**Run the application**

```bash
cd SportsStore
dotnet run
```

The first time it starts, EF Core will apply all migrations and seed the database with nine sample products automatically. You should see Serilog output in the console confirming startup. Open your browser at `https://localhost:5001`.

**Run the tests**

```bash
dotnet test SportsSln.sln
```

All four tests should pass. They use mocked dependencies so no database or Stripe connection is required.

---

## Stripe test payments

Stripe provides test card numbers that simulate different payment outcomes. When you reach the Stripe checkout page, use these:

| Scenario | Card number | Expiry | CVC |
|---|---|---|---|
| Successful payment | `4242 4242 4242 4242` | Any future date | Any 3 digits |
| Payment declined | `4000 0000 0000 0002` | Any future date | Any 3 digits |
| Insufficient funds | `4000 0000 0000 9995` | Any future date | Any 3 digits |

After a successful test payment, you will be redirected back to the order confirmation page with the order ID and Stripe payment reference.

---

## Logging

Logs are written to two places:

- **Console** — appears in the terminal while the app is running. Formatted for readability with timestamp and log level.
- **Rolling file** — written to the `logs/` folder in the project root. A new file is created each day and files older than seven days are removed automatically.

Each log entry includes the machine name and environment name. HTTP requests are logged by Serilog middleware rather than by the controllers, which keeps the request lifecycle separate from business logic. Key events like adding a product to the cart, initiating checkout, creating a Stripe session, and saving a confirmed order are all logged with structured properties.

Log levels can be tuned per namespace in `appsettings.json` under the `Serilog` section. By default, Microsoft framework noise is filtered to Warning so you only see what matters.

---

## Project structure

```
SportsSln/
├── SportsStore/
│   ├── Controllers/
│   │   ├── HomeController.cs       # Product listing with pagination
│   │   ├── CartController.cs       # Add/remove items, view cart
│   │   └── OrderController.cs      # Checkout, Stripe redirect, confirmation
│   ├── Infrastructure/
│   │   ├── PageLinkTagHelper.cs    # Custom tag helper for page links
│   │   └── SessionExtensions.cs    # Typed JSON session helpers
│   ├── Models/
│   │   ├── Cart.cs                 # Session-based cart
│   │   ├── Order.cs                # Order and OrderLine for persistence
│   │   ├── Product.cs              # Product entity
│   │   ├── StoreDbContext.cs       # EF Core DbContext
│   │   ├── EFStoreRepository.cs    # Repository implementation
│   │   ├── IStoreRepository.cs     # Repository interface
│   │   └── SeedData.cs             # Database seeding
│   ├── Services/
│   │   ├── IPaymentService.cs      # Payment abstraction
│   │   └── StripePaymentService.cs # Stripe Checkout Session implementation
│   ├── Views/
│   │   ├── Cart/                   # Cart view
│   │   ├── Home/                   # Product listing
│   │   └── Order/                  # Checkout, success, cancel, failed
│   ├── appsettings.json            # Configuration (no secrets)
│   └── Program.cs                  # App entry point and DI setup
├── SportsStore.Tests/
│   ├── HomeControllerTests.cs      # Pagination and repository tests
│   └── PageLinkTagHelperTests.cs   # Tag helper output tests
└── .github/
    └── workflows/
        └── ci.yml                  # GitHub Actions CI pipeline
```

---

## CI pipeline

The pipeline at `.github/workflows/ci.yml` runs on every push and pull request to `main`. Steps:

1. Check out the repository
2. Set up .NET 8 SDK
3. Restore NuGet packages
4. Build in Release configuration
5. Run all tests — pipeline fails if any test fails
6. Upload test results as a build artifact

Stripe keys are injected from GitHub repository secrets (`STRIPE_PUBLISHABLE_KEY` and `STRIPE_SECRET_KEY`) so the pipeline never reads them from any committed file.

---

## Security notes

- Stripe API keys are stored in .NET User Secrets locally and GitHub Actions Secrets in CI — never in source control
- `appsettings.Development.json` is excluded by `.gitignore`
- The `wwwroot/lib/` vendor folder is excluded by `.gitignore` — Bootstrap is managed via LibMan
- Payment status is verified server-side via Stripe API before any order is saved to the database
- Session cookies are marked `HttpOnly` and `IsEssential`
