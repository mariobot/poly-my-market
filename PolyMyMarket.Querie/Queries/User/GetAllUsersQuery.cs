using MediatR;
using Microsoft.EntityFrameworkCore;
using PolyMyMarket.Context;
using PolyMyMarket.Models;

namespace PolyMyMarket.Querie.Queries.User;

/// <summary>
/// Query to get all users
/// </summary>
public class GetAllUsersQuery : IRequest<List<Models.User>>
{
}

/// <summary>
/// Handler for GetAllUsersQuery
/// </summary>
public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, List<Models.User>>
{
    private readonly MarketContext _context;

    public GetAllUsersQueryHandler(MarketContext context)
    {
        _context = context;
    }

    public async Task<List<Models.User>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        return await _context.Users
            .OrderByDescending(u => u.CreatedDate)
            .ToListAsync(cancellationToken);
    }
}
