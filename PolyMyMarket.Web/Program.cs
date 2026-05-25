using PolyMyMarket.Web.Components;
using Radzen;
using Microsoft.EntityFrameworkCore;
using PolyMyMarket.Context;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRadzenComponents();

// Add database context
builder.Services.AddDbContext<AppContext>(options =>
{
    // Configure your database provider here
    // For SQL Server:
    // options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));

    // For SQLite (development):
    // options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));

    // For InMemory (testing):
    options.UseInMemoryDatabase("PolyMyMarketDb");
});

var app = builder.Build();

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
