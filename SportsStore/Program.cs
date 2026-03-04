using Microsoft.EntityFrameworkCore;
using Serilog;
using SportsStore.Models;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("SportsStore application starting up");

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration)
                     .ReadFrom.Services(services)
                     .Enrich.FromLogContext());

    builder.Services.AddControllersWithViews();

    builder.Services.AddDbContext<StoreDbContext>(opts => {
        opts.UseSqlServer(
            builder.Configuration["ConnectionStrings:SportsStoreConnection"]);
    });

    builder.Services.AddScoped<IStoreRepository, EFStoreRepository>();

    var app = builder.Build();

    app.UseSerilogRequestLogging(options => {
        options.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    app.UseStaticFiles();
    app.MapControllerRoute("pagination",
        "Products/Page{productPage}",
        new { Controller = "Home", action = "Index" });
    app.MapDefaultControllerRoute();

    SeedData.EnsurePopulated(app);

    Log.Information("SportsStore application configured successfully — listening for requests");

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "SportsStore application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}