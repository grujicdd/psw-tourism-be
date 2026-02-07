using Explorer.Stakeholders.Core.Domain;

namespace Explorer.Stakeholders.Core.Domain.RepositoryInterfaces;

public interface IUserInterestRepository
{
    List<UserInterest> GetByUserId(long userId);
    void DeleteAllByUserId(long userId);
    void CreateUserInterest(long userId, long interestId);
}
