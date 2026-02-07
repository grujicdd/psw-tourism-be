using Explorer.Stakeholders.Core.Domain;
using Explorer.Stakeholders.Core.Domain.RepositoryInterfaces;
using Explorer.Stakeholders.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Explorer.Stakeholders.Infrastructure.Database.Repositories;

public class UserInterestRepository : IUserInterestRepository
{
    private readonly StakeholdersContext _context;

    public UserInterestRepository(StakeholdersContext context)
    {
        _context = context;
    }

    public List<UserInterest> GetByUserId(long userId)
    {
        return _context.UserInterests
            .Where(ui => ui.UserId == userId)
            .ToList();
    }

    public void DeleteAllByUserId(long userId)
    {
        var userInterests = _context.UserInterests
            .Where(ui => ui.UserId == userId)
            .ToList();

        _context.UserInterests.RemoveRange(userInterests);
        _context.SaveChanges();
    }

    public void CreateUserInterest(long userId, long interestId)
    {
        _context.UserInterests.Add(new UserInterest(userId, interestId));
        _context.SaveChanges();
    }
}
