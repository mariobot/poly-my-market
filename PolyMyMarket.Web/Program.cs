using PolyMyMarket.Web.Components;
using Radzen;
using Microsoft.EntityFrameworkCore;
using PolyMyMarket.Context;
using PolyMyMarket.Web.Services;
using PolyMyMarket.Command.Common;
using PolyMyMarket.Command.Market;
using PolyMyMarket.Command.User;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRadzenComponents();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<DialogService>();

// Add database context
builder.Services.AddDbContext<MarketContext>(options =>
{
    // Configure your database provider here
    // For SQL Server:
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));

    // For InMemory (testing):
    //options.UseInMemoryDatabase("PolyMyMarketDb");
});

// Add application services
builder.Services.AddScoped<MarketService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<UserSessionService>();
builder.Services.AddScoped<CommandDispatcher>();

// Register command handlers - Market
builder.Services.AddScoped<ICommandHandler<CreateMarketCommand, CommandResult<int>>, CreateMarketCommandHandler>();
builder.Services.AddScoped<ICommandHandler<PlaceBuyOrderCommand, CommandResult>, PlaceBuyOrderCommandHandler>();
builder.Services.AddScoped<ICommandHandler<PlaceSellOrderCommand, CommandResult>, PlaceSellOrderCommandHandler>();
builder.Services.AddScoped<ICommandHandler<ResolveMarketCommand, CommandResult>, ResolveMarketCommandHandler>();
builder.Services.AddScoped<ICommandHandler<PlaceMultiOutcomeBuyOrderCommand, CommandResult>, PlaceMultiOutcomeBuyOrderCommandHandler>();
builder.Services.AddScoped<ICommandHandler<PlaceMultiOutcomeSellOrderCommand, CommandResult>, PlaceMultiOutcomeSellOrderCommandHandler>();

// Register command handlers - User
builder.Services.AddScoped<ICommandHandler<GetOrCreateUserCommand, CommandResult<int>>, GetOrCreateUserCommandHandler>();
builder.Services.AddScoped<ICommandHandler<UpdateUserCommand, CommandResult>, UpdateUserCommandHandler>();
builder.Services.AddScoped<ICommandHandler<UpdateUserBalanceCommand, CommandResult>, UpdateUserBalanceCommandHandler>();
builder.Services.AddScoped<ICommandHandler<DeleteUserCommand, CommandResult>, DeleteUserCommandHandler>();

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
