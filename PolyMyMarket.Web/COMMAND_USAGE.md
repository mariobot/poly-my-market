# Command Usage Guide

## Overview
All command handlers are registered in the Web project's dependency injection container. You can use them in two ways:

1. **Direct Injection**: Inject specific handlers where needed
2. **CommandDispatcher**: Use the dispatcher service for simplified command execution

## Using CommandDispatcher (Recommended)

The `CommandDispatcher` service simplifies command execution by automatically resolving handlers from DI.

### Example: Create a Market

```csharp
@inject CommandDispatcher CommandDispatcher

private async Task CreateMarketAsync()
{
	var command = new CreateMarketCommand
	{
		Title = "Will it rain tomorrow?",
		Description = "Resolves YES if it rains in Seattle on the specified date",
		Category = MarketCategory.Weather,
		EndDate = DateTime.UtcNow.AddDays(1),
		InitialLiquidity = 1000m,
		MarketType = MarketType.Binary
	};

	var result = await CommandDispatcher.ExecuteAsync<CreateMarketCommand, CommandResult<int>>(command);

	if (result.Success)
	{
		var marketId = result.Data;
		// Handle success
	}
	else
	{
		// Handle error: result.Message
	}
}
```

### Example: Place Buy Order

```csharp
@inject CommandDispatcher CommandDispatcher

private async Task PlaceBuyOrderAsync(int marketId, int userId, Outcome outcome, decimal shares)
{
	var command = new PlaceBuyOrderCommand
	{
		MarketId = marketId,
		UserId = userId,
		Outcome = outcome,
		Shares = shares
	};

	var result = await CommandDispatcher.ExecuteAsync<PlaceBuyOrderCommand, CommandResult>(command);

	if (result.Success)
	{
		// Order placed successfully
	}
	else
	{
		// Handle error: result.Message
	}
}
```

### Example: Update User

```csharp
@inject CommandDispatcher CommandDispatcher

private async Task UpdateUserAsync(int userId, string name, string email, decimal balance)
{
	var command = new UpdateUserCommand
	{
		UserId = userId,
		Name = name,
		Email = email,
		Balance = balance
	};

	var result = await CommandDispatcher.ExecuteAsync<UpdateUserCommand, CommandResult>(command);

	if (result.Success)
	{
		// User updated
	}
	else
	{
		// Handle error: result.Message
	}
}
```

## Using Direct Handler Injection

Inject specific handlers when you need more control or want explicit dependencies.

### Example: Create Market with Direct Handler

```csharp
@inject ICommandHandler<CreateMarketCommand, CommandResult<int>> CreateMarketHandler

private async Task CreateMarketAsync()
{
	var command = new CreateMarketCommand
	{
		Title = "Will it rain tomorrow?",
		Description = "Resolves YES if it rains in Seattle on the specified date",
		Category = MarketCategory.Weather,
		EndDate = DateTime.UtcNow.AddDays(1),
		InitialLiquidity = 1000m,
		MarketType = MarketType.Binary
	};

	var result = await CreateMarketHandler.HandleAsync(command);

	if (result.Success)
	{
		var marketId = result.Data;
		// Handle success
	}
}
```

## Available Commands

### Market Commands

#### CreateMarketCommand
Creates a new prediction market (binary or multi-outcome).

**Properties:**
- `Title` (string, required, max 200)
- `Description` (string, required, max 1000)
- `Category` (MarketCategory, required)
- `EndDate` (DateTime, required, must be future)
- `InitialLiquidity` (decimal, required, > 0)
- `MarketType` (MarketType, required)
- `OutcomeNames` (List<string>, required for multi-outcome, >= 2 outcomes)

**Returns:** `CommandResult<int>` with market ID

#### PlaceBuyOrderCommand
Places a buy order for shares in a binary market.

**Properties:**
- `MarketId` (int, required)
- `UserId` (int, required)
- `Outcome` (Outcome, required: Yes or No)
- `Shares` (decimal, required, > 0)

**Returns:** `CommandResult`

#### PlaceSellOrderCommand
Sells shares from an existing position.

**Properties:**
- `MarketId` (int, required)
- `UserId` (int, required)
- `Outcome` (Outcome, required: Yes or No)
- `Shares` (decimal, required, > 0)

**Returns:** `CommandResult`

### User Commands

#### GetOrCreateUserCommand
Retrieves existing user by email or creates a new one.

**Properties:**
- `Email` (string, required)
- `Name` (string, required)
- `InitialBalance` (decimal, default 10000)

**Returns:** `CommandResult<int>` with user ID

#### UpdateUserCommand
Updates user profile information.

**Properties:**
- `UserId` (int, required)
- `Name` (string, required)
- `Email` (string, required, must be unique)
- `Balance` (decimal, required, >= 0)

**Returns:** `CommandResult`

#### UpdateUserBalanceCommand
Updates only the user's balance.

**Properties:**
- `UserId` (int, required)
- `NewBalance` (decimal, required, >= 0)

**Returns:** `CommandResult`

#### DeleteUserCommand
Deletes a user account (only if no active positions).

**Properties:**
- `UserId` (int, required)

**Returns:** `CommandResult`

## Error Handling

All commands return `CommandResult` or `CommandResult<T>` with:
- `Success` (bool): Indicates if the operation succeeded
- `Message` (string): Success message or error description
- `Data` (T, for generic result): The returned data

### Example Error Handling Pattern

```csharp
var result = await CommandDispatcher.ExecuteAsync<CreateMarketCommand, CommandResult<int>>(command);

if (result.Success)
{
	NotificationService.Notify(NotificationSeverity.Success, "Success", result.Message);
	var marketId = result.Data;
	NavigationManager.NavigateTo($"/market/{marketId}");
}
else
{
	NotificationService.Notify(NotificationSeverity.Error, "Error", result.Message);
}
```

## Integration with Existing Services

The commands are now available but **do not replace** the existing `MarketService` and `UserService` yet. You can:

1. **Gradual Migration**: Start using commands in new code while keeping existing service calls
2. **Parallel Operation**: Use commands for writes and services for reads (CQRS pattern)
3. **Full Refactor**: Update services to internally use command handlers

### Example: MarketService Refactoring (Future Step)

```csharp
public class MarketService
{
	private readonly CommandDispatcher _dispatcher;
	private readonly MarketContext _context;

	public MarketService(CommandDispatcher dispatcher, MarketContext context)
	{
		_dispatcher = dispatcher;
		_context = context;
	}

	// Write operation using command
	public async Task<(bool success, string message, int marketId)> CreateMarketAsync(Market market)
	{
		var command = new CreateMarketCommand
		{
			Title = market.Title,
			Description = market.Description,
			// ... map properties
		};

		var result = await _dispatcher.ExecuteAsync<CreateMarketCommand, CommandResult<int>>(command);
		return (result.Success, result.Message, result.Data ?? 0);
	}

	// Read operation stays as-is with EF Core
	public async Task<Market?> GetMarketByIdAsync(int marketId)
	{
		return await _context.Markets
			.Include(m => m.Outcomes)
			.FirstOrDefaultAsync(m => m.Id == marketId);
	}
}
```

## Dependency Injection Registration

All handlers are registered in `Program.cs`:

```csharp
// CommandDispatcher
builder.Services.AddScoped<CommandDispatcher>();

// Market Command Handlers
builder.Services.AddScoped<ICommandHandler<CreateMarketCommand, CommandResult<int>>, CreateMarketCommandHandler>();
builder.Services.AddScoped<ICommandHandler<PlaceBuyOrderCommand, CommandResult>, PlaceBuyOrderCommandHandler>();
builder.Services.AddScoped<ICommandHandler<PlaceSellOrderCommand, CommandResult>, PlaceSellOrderCommandHandler>();

// User Command Handlers
builder.Services.AddScoped<ICommandHandler<GetOrCreateUserCommand, CommandResult<int>>, GetOrCreateUserCommandHandler>();
builder.Services.AddScoped<ICommandHandler<UpdateUserCommand, CommandResult>, UpdateUserCommandHandler>();
builder.Services.AddScoped<ICommandHandler<UpdateUserBalanceCommand, CommandResult>, UpdateUserBalanceCommandHandler>();
builder.Services.AddScoped<ICommandHandler<DeleteUserCommand, CommandResult>, DeleteUserCommandHandler>();
```

All handlers use `Scoped` lifetime to work with EF Core's scoped `DbContext`.
