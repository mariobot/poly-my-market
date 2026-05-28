using PolyMyMarket.Web.Components;
using Radzen;
using Microsoft.EntityFrameworkCore;
using PolyMyMarket.Context;
using PolyMyMarket.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRadzenComponents();
builder.Services.AddScoped<NotificationService>();

// Add database context
builder.Services.AddDbContext<MarketContext>(options =>
{
    // Configure your database provider here
    // For SQL Server:
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));

    // For SQLite (development):
    // options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));

    // For InMemory (testing):
    //options.UseInMemoryDatabase("PolyMyMarketDb");
});

// Add application services
builder.Services.AddScoped<MarketService>();
builder.Services.AddScoped<UserService>();

var app = builder.Build();

// Initialize database with seed data
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MarketContext>();
    dbContext.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
