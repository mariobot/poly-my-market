using MediatR;
using Microsoft.EntityFrameworkCore;
using PolyMyMarket.Context;
using PolyMyMarket.Models;

namespace PolyMyMarket.Querie.Queries.User;

/// <summary>
/// Query to get a user by ID
/// </summary>
public class GetUserByIdQuery : IRequest<Models.User?>
{
    public int UserId { get; set; }

    public GetUserByIdQuery(int userId)
    {
        UserId = userId;
    }
}

/// <summary>
/// Handler for GetUserByIdQuery
/// </summary>
public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Models.User?>
{
    private readonly MarketContext _context;

    public GetUserByIdQueryHandler(MarketContext context)
    {
        _context = context;
    }

    public async Task<Models.User?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
    }
}
