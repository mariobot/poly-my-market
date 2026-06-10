using PolyMyMarket.Context;
using PolyMyMarket.Models;
using PolyMyMarket.Command.Common;
using PolyMyMarket.Command.User;
using PolyMyMarket.Querie.Queries.User;

namespace PolyMyMarket.Web.Services;

public class UserService
{
    private readonly MarketContext _context;
    private readonly CommandDispatcher _commandDispatcher;
    private readonly QueryDispatcher _queryDispatcher;

    public UserService(MarketContext context, CommandDispatcher commandDispatcher, QueryDispatcher queryDispatcher)
    {
        _context = context;
        _commandDispatcher = commandDispatcher;
        _queryDispatcher = queryDispatcher;
    }

    // Get or create user by email
    public async Task<User> GetOrCreateUserAsync(string email, string name)
    {
        var command = new GetOrCreateUserCommand
        {
            Email = email,
            Name = name,
            InitialBalance = 10000m
        };

        var result = await _commandDispatcher.ExecuteAsync<GetOrCreateUserCommand, CommandResult<int>>(command);

        if (result.Success)
        {
            // Fetch and return the user
            var user = await _context.Users.FindAsync(result.Data);
            return user!;
        }

        // Fallback (should not happen with valid command)
        throw new InvalidOperationException($"Failed to get or create user: {result.Message}");
    }

    // Get user by ID
    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _queryDispatcher.ExecuteAsync<GetUserByIdQuery, User?>(new GetUserByIdQuery(userId));
    }

    // Update user balance
    public async Task UpdateUserBalanceAsync(int userId, decimal newBalance)
    {
        var command = new UpdateUserBalanceCommand
        {
            UserId = userId,
            NewBalance = newBalance
        };

        await _commandDispatcher.ExecuteAsync<UpdateUserBalanceCommand, CommandResult>(command);
    }

    // Get all users
    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _queryDispatcher.ExecuteAsync<GetAllUsersQuery, List<User>>(new GetAllUsersQuery());
    }

    // Update user details
    public async Task<(bool success, string message)> UpdateUserAsync(int userId, string name, string email, decimal balance)
    {
        var command = new UpdateUserCommand
        {
            UserId = userId,
            Name = name,
            Email = email,
            Balance = balance
        };

        var result = await _commandDispatcher.ExecuteAsync<UpdateUserCommand, CommandResult>(command);
        return (result.Success, result.Message);
    }

    // Delete user
    public async Task<(bool success, string message)> DeleteUserAsync(int userId)
    {
        var command = new DeleteUserCommand
        {
            UserId = userId
        };

        var result = await _commandDispatcher.ExecuteAsync<DeleteUserCommand, CommandResult>(command);
        return (result.Success, result.Message);
    }

    // Get user positions with current values
    public async Task<List<PositionViewModel>> GetUserPositionsAsync(int userId)
    {
        return await _queryDispatcher.ExecuteAsync<GetUserPositionsQuery, List<PositionViewModel>>(new GetUserPositionsQuery(userId));
    }

    // Get user position for a specific market
    public async Task<Position?> GetUserPositionForMarketAsync(int userId, int marketId)
    {
        return await _queryDispatcher.ExecuteAsync<GetUserPositionForMarketQuery, Position?>(new GetUserPositionForMarketQuery(userId, marketId));
    }

    // Get user order history
    public async Task<List<Order>> GetUserOrdersAsync(int userId, int? marketId = null, int count = 50)
    {
        return await _queryDispatcher.ExecuteAsync<GetUserOrdersQuery, List<Order>>(new GetUserOrdersQuery(userId, marketId, count));
    }

    // Get user statistics
    public async Task<UserStatistics> GetUserStatisticsAsync(int userId)
    {
        return await _queryDispatcher.ExecuteAsync<GetUserStatisticsQuery, UserStatistics>(new GetUserStatisticsQuery(userId));
    }

    // Get user multi-outcome positions with current values
    public async Task<List<OutcomePositionViewModel>> GetUserOutcomePositionsAsync(int userId)
    {
        return await _queryDispatcher.ExecuteAsync<GetUserOutcomePositionsQuery, List<OutcomePositionViewModel>>(new GetUserOutcomePositionsQuery(userId));
    }

    // Get combined user statistics (binary + multi-outcome)
    public async Task<UserStatistics> GetCombinedUserStatisticsAsync(int userId)
    {
        return await _queryDispatcher.ExecuteAsync<GetCombinedUserStatisticsQuery, UserStatistics>(new GetCombinedUserStatisticsQuery(userId));
    }
}
