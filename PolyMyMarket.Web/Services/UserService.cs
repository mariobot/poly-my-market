using Microsoft.EntityFrameworkCore;
using PolyMyMarket.Context;
using PolyMyMarket.Models;

namespace PolyMyMarket.Web.Services;

public class UserService
{
    private readonly MarketContext _context;
    private readonly MarketService _marketService;

    public UserService(MarketContext context, MarketService marketService)
    {
        _context = context;
        _marketService = marketService;
    }

    // Get or create user by email
    public async Task<User> GetOrCreateUserAsync(string email, string name)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            user = new User
            {
                Email = email,
                Name = name,
                Balance = 10000m, // Starting balance
                CreatedDate = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        return user;
    }

    // Get user by ID
    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _context.Users.FindAsync(userId);
    }

    // Update user balance
    public async Task UpdateUserBalanceAsync(int userId, decimal newBalance)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.Balance = newBalance;
            await _context.SaveChangesAsync();
        }
    }

    // Get all users
    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _context.Users
            .OrderByDescending(u => u.CreatedDate)
            .ToListAsync();
    }

    // Update user details
    public async Task<(bool success, string message)> UpdateUserAsync(int userId, string name, string email, decimal balance)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return (false, "User not found");
            }

            // Check if email is being changed to one that already exists
            if (user.Email != email)
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email && u.Id != userId);
                if (existingUser != null)
                {
                    return (false, "Email already exists");
                }
            }

            user.Name = name;
            user.Email = email;
            user.Balance = balance;

            await _context.SaveChangesAsync();
            return (true, "User updated successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Error updating user: {ex.Message}");
        }
    }

    // Delete user
    public async Task<(bool success, string message)> DeleteUserAsync(int userId)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.Orders)
                .Include(u => u.Positions)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return (false, "User not found");
            }

            // Check if user has active positions
            if (user.Positions.Any(p => p.YesShares > 0 || p.NoShares > 0))
            {
                return (false, "Cannot delete user with active positions");
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return (true, "User deleted successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Error deleting user: {ex.Message}");
        }
    }

    // Get user positions with current values
    public async Task<List<PositionViewModel>> GetUserPositionsAsync(int userId)
    {
        var positions = await _context.Positions
            .Where(p => p.UserId == userId)
            .Include(p => p.Market)
            .ToListAsync();

        var positionViewModels = new List<PositionViewModel>();

        foreach (var position in positions)
        {
            // Only show positions with shares
            if (position.YesShares == 0 && position.NoShares == 0)
                continue;

            var (yesPrice, noPrice) = _marketService.GetCurrentPrices(position.Market);

            var viewModel = new PositionViewModel
            {
                Position = position,
                Market = position.Market,
                CurrentYesPrice = yesPrice,
                CurrentNoPrice = noPrice,
                CurrentValue = position.CalculateCurrentValue(yesPrice, noPrice),
                ProfitLoss = position.CalculateProfitLoss(yesPrice, noPrice)
            };

            positionViewModels.Add(viewModel);
        }

        return positionViewModels.OrderByDescending(p => p.Position.LastUpdated).ToList();
    }

    // Get user order history
    public async Task<List<Order>> GetUserOrdersAsync(int userId, int? marketId = null, int count = 50)
    {
        IQueryable<Order> query = _context.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.Market);

        if (marketId.HasValue)
        {
            query = query.Where(o => o.MarketId == marketId.Value);
        }

        return await query
            .OrderByDescending(o => o.Timestamp)
            .Take(count)
            .ToListAsync();
    }

    // Get user statistics
    public async Task<UserStatistics> GetUserStatisticsAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return new UserStatistics();

        var positions = await GetUserPositionsAsync(userId);
        var orders = await GetUserOrdersAsync(userId);

        decimal totalInvested = positions.Sum(p => p.Position.TotalInvested);
        decimal currentValue = positions.Sum(p => p.CurrentValue);
        decimal totalProfitLoss = currentValue - totalInvested;

        return new UserStatistics
        {
            Balance = user.Balance,
            TotalInvested = totalInvested,
            CurrentPortfolioValue = currentValue,
            TotalProfitLoss = totalProfitLoss,
            TotalOrders = orders.Count,
            ActivePositions = positions.Count,
            MemberSince = user.CreatedDate
        };
    }

    // ==================== MULTI-OUTCOME METHODS ====================

    // Get user multi-outcome positions with current values
    public async Task<List<OutcomePositionViewModel>> GetUserOutcomePositionsAsync(int userId)
    {
        var positions = await _context.OutcomePositions
            .Where(p => p.UserId == userId && p.Shares > 0)
            .Include(p => p.MarketOutcome)
                .ThenInclude(mo => mo.Market)
            .ToListAsync();

        var positionViewModels = new List<OutcomePositionViewModel>();

        // Group by market to calculate prices efficiently
        var marketGroups = positions.GroupBy(p => p.MarketOutcome.MarketId);

        foreach (var marketGroup in marketGroups)
        {
            var market = await _context.Markets
                .Include(m => m.Outcomes)
                .FirstOrDefaultAsync(m => m.Id == marketGroup.Key);

            if (market == null || market.MarketType != MarketType.MultiOutcome)
                continue;

            var prices = _marketService.GetMultiOutcomePrices(market.Outcomes.ToList());

            foreach (var position in marketGroup)
            {
                decimal currentPrice = prices.GetValueOrDefault(position.MarketOutcomeId, 0.5m);
                decimal currentValue = position.CalculateCurrentValue(currentPrice);
                decimal profitLoss = position.CalculateProfitLoss(currentPrice);

                positionViewModels.Add(new OutcomePositionViewModel
                {
                    Position = position,
                    MarketOutcome = position.MarketOutcome,
                    Market = position.MarketOutcome.Market,
                    CurrentPrice = currentPrice,
                    CurrentValue = currentValue,
                    ProfitLoss = profitLoss
                });
            }
        }

        return positionViewModels.OrderByDescending(p => p.Position.LastUpdated).ToList();
    }

    // Get combined user statistics (binary + multi-outcome)
    public async Task<UserStatistics> GetCombinedUserStatisticsAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return new UserStatistics();

        // Binary positions
        var binaryPositions = await GetUserPositionsAsync(userId);
        decimal binaryInvested = binaryPositions.Sum(p => p.Position.TotalInvested);
        decimal binaryValue = binaryPositions.Sum(p => p.CurrentValue);

        // Multi-outcome positions
        var outcomePositions = await GetUserOutcomePositionsAsync(userId);
        decimal outcomeInvested = outcomePositions.Sum(p => p.Position.TotalInvested);
        decimal outcomeValue = outcomePositions.Sum(p => p.CurrentValue);

        // Combined
        decimal totalInvested = binaryInvested + outcomeInvested;
        decimal currentValue = binaryValue + outcomeValue;
        decimal totalProfitLoss = currentValue - totalInvested;

        var orders = await GetUserOrdersAsync(userId);

        return new UserStatistics
        {
            Balance = user.Balance,
            TotalInvested = totalInvested,
            CurrentPortfolioValue = currentValue,
            TotalProfitLoss = totalProfitLoss,
            TotalOrders = orders.Count,
            ActivePositions = binaryPositions.Count + outcomePositions.Count,
            MemberSince = user.CreatedDate
        };
    }
}

// View model for positions with calculated values
public class PositionViewModel
{
    public Position Position { get; set; } = null!;
    public Market Market { get; set; } = null!;
    public decimal CurrentYesPrice { get; set; }
    public decimal CurrentNoPrice { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal ProfitLoss { get; set; }
    public decimal ProfitLossPercent => Position.TotalInvested > 0 
        ? (ProfitLoss / Position.TotalInvested) * 100 
        : 0;
}

// User statistics model
public class UserStatistics
{
    public decimal Balance { get; set; }
    public decimal TotalInvested { get; set; }
    public decimal CurrentPortfolioValue { get; set; }
    public decimal TotalProfitLoss { get; set; }
    public int TotalOrders { get; set; }
    public int ActivePositions { get; set; }
    public DateTime MemberSince { get; set; }
}

// View model for multi-outcome positions
public class OutcomePositionViewModel
{
    public OutcomePosition Position { get; set; } = null!;
    public MarketOutcome MarketOutcome { get; set; } = null!;
    public Market Market { get; set; } = null!;
    public decimal CurrentPrice { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal ProfitLoss { get; set; }
    public decimal ProfitLossPercent => Position.TotalInvested > 0 
        ? (ProfitLoss / Position.TotalInvested) * 100 
        : 0;
}
