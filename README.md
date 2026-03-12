# SportsStore

This is my ASP.NET Core web application for the Full Stack Development module. It started as a .NET 6 example from the Pro ASP.NET Core book and I upgraded and extended it to make it ready for production use.

The store lets customers browse products, add them to a cart and pay using Stripe. Everything is logged with Serilog and there is a GitHub Actions pipeline that runs the tests automatically on every push.

---

## What I did and why

### Upgraded to .NET 8

The first thing I did was upgrade the framework. .NET 6 is no longer supported so staying on it was not a good idea. .NET 8 is the current Long Term Support release which means it gets security updates for several more years. The upgrade was pretty straighforward, I updated the target framework in both csproj files, updated the NuGet packages and removed the old SDK pin from global.json. All the existing tests still passed after the upgrade with no changes needed.

### Structured logging with Serilog

The default logging in .NET works but it does not give you much control. With Serilog I can write logs to the console while developing and also save them to a file that rotates every day. Each log entry automatically includes the machine name and enviroment name which makes it easier to track down problems.

The important thing about Serilog is structured logging. Instead of just writing a text message I write something like `"Product added, ProductId: {ProductId}, Name: {Name}"` and Serilog saves those as seperate properties. That means you can actually search and filter logs later if you connect a tool like Seq or Elasticsearch.

### Stripe payment integration

For payments I used Stripe Checkout Sessions. The way it works is the user fills in their details on our site, then gets redirected to a Stripe hosted payment page, and after paying Stripe sends them back to our site. This is good because we never touch the card data directly.

The API keys are never saved in any file in the project. Locally I used .NET User Secrets and in GitHub Actions I used repository secrets. The appsettings.json file only has empty placeholders to show where the keys go. When the user comes back from Stripe I verify the payment status directly with the Stripe API before saving the order, so a failed or cancelled payment will never create an order in the database.

### GitHub Actions CI

The pipeline runs on every push and pull request to main. It restores packages, builds in Release mode and runs all the tests. If a test fails the whole build fails. I kept it simple on purpose because simple pipelines are easier to understand and debug. The test results get uploaded as an artifact so you can check them after the run.

---

## How to run it locally

You will need:

- .NET 8 SDK
- SQL Server or SQL Server LocalDB (comes with Visual Studio)
- A Stripe account for test keys (free to sign up)

**Clone the repo**

```bash
git clone https://github.com/geisonhg/fs-lab-assignment-sportsstore-72804.git
cd fs-lab-assignment-sportsstore-72804
```

**Set up Stripe keys**

The app reads the Stripe keys from .NET User Secrets. Run these commands and replace the placeholders with your own keys from the Stripe dashboard under Developers > API Keys:

```bash
dotnet user-secrets set "Stripe:PublishableKey" "pk_test_YOUR_KEY_HERE" --project SportsStore/SportsStore.csproj
dotnet user-secrets set "Stripe:SecretKey" "sk_test_YOUR_KEY_HERE" --project SportsStore/SportsStore.csproj
```

User Secrets are stored outside the project folder so there is no risk of commiting them by accident.

**Check the connection string**

Open `SportsStore/appsettings.json` and check the `SportsStoreConnection` value. The default is SQL Server LocalDB:

```
Server=(localdb)\MSSQLLocalDB;Database=SportsStore;MultipleActiveResultSets=true
```

If you are using a different SQL Server instance just update that value. Do not commit real credentials, use environment variables or a local appsettings.Development.json file which is already in .gitignore.

**Run the app**

```bash
cd SportsStore
dotnet run
```

The first time it starts EF Core will create the database and seed it with nine sample products. You should see Serilog output in the console. Open your browser at `http://localhost:5000`.

**Run the tests**

```bash
dotnet test SportsSln.sln
```

All four tests should pass. They use mocked dependancies so no database or Stripe connection is needed.

---

## Stripe test cards

When you get to the Stripe checkout page you can use these test card numbers:

| Scenario | Card number | Expiry | CVC |
|---|---|---|---|
| Payment works | `4242 4242 4242 4242` | Any future date | Any 3 digits |
| Payment declined | `4000 0000 0000 0002` | Any future date | Any 3 digits |
| Insufficient funds | `4000 0000 0000 9995` | Any future date | Any 3 digits |

After a succesful payment you will be redirected to the order confirmation page with the order details and the Stripe payment reference.

---

## Logging

Logs go to two places:

- **Console** - shows up in the terminal while the app is running, easy to read format with timestamp and log level
- **File** - saved in the `logs/` folder, a new file gets created each day and files older than 7 days are deleted automatically

Each entry includes the machine name and enviroment. HTTP requests are logged by Serilog middleware so the controllers stay clean. Things like adding items to the cart, starting checkout, creating a Stripe session and saving an order are all logged with structured properties.

You can change the log levels in appsettings.json under the Serilog section. By default Microsoft framework logs are set to Warning so the output is not too noisy.

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
│   │   └── SessionExtensions.cs    # JSON helpers for session storage
│   ├── Models/
│   │   ├── Cart.cs                 # Cart stored in session
│   │   ├── Order.cs                # Order and OrderLine entities
│   │   ├── Product.cs              # Product entity
│   │   ├── StoreDbContext.cs       # EF Core DbContext
│   │   ├── EFStoreRepository.cs    # Repository implementation
│   │   ├── IStoreRepository.cs     # Repository interface
│   │   └── SeedData.cs             # Database seeding on startup
│   ├── Services/
│   │   ├── IPaymentService.cs      # Payment interface
│   │   └── StripePaymentService.cs # Stripe implementation
│   ├── Views/
│   │   ├── Cart/                   # Cart page
│   │   ├── Home/                   # Product listing
│   │   └── Order/                  # Checkout, success, cancel, failed
│   ├── appsettings.json            # Config file (no secrets in here)
│   └── Program.cs                  # Entry point and DI setup
├── SportsStore.Tests/
│   ├── HomeControllerTests.cs      # Pagination and product tests
│   └── PageLinkTagHelperTests.cs   # Tag helper tests
└── .github/
    └── workflows/
        └── ci.yml                  # GitHub Actions pipeline
```

---

## CI pipeline

The pipeline in `.github/workflows/ci.yml` runs on every push and pull request to main:

1. Check out the code
2. Set up .NET 8 SDK
3. Restore NuGet packages
4. Build in Release mode
5. Run all tests, pipeline fails if any test fails
6. Upload test results as a build artifact

Stripe keys come from GitHub repository secrets so they are never in any file that gets committed.

---

## Security notes

- Stripe API keys are in .NET User Secrets locally and GitHub Actions Secrets in CI, never in source control
- appsettings.Development.json is in .gitignore
- The wwwroot/lib/ folder is in .gitignore, Bootstrap is loaded via LibMan
- Payment status is verified with the Stripe API on the server before any order is saved
- Session cookies are marked HttpOnly and IsEssential
