using Explorer.BuildingBlocks.Core.Domain;

namespace Explorer.Stakeholders.Core.Domain;

public class User : Entity
{
    public string Username { get; private set; }
    public string Password { get; private set; }
    public UserRole Role { get; private set; }
    public bool IsActive { get; set; }
    public bool ReceiveRecommendations { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public int BlockCount { get; private set; }

    public User(string username, string password, UserRole role, bool isActive, bool receiveRecommendations = true)
    {
        Username = username;
        Password = password;
        Role = role;
        IsActive = isActive;
        ReceiveRecommendations = receiveRecommendations;
        FailedLoginAttempts = 0;
        BlockCount = 0;
        Validate();
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Username)) throw new ArgumentException("Invalid Username");
        if (string.IsNullOrWhiteSpace(Password)) throw new ArgumentException("Invalid Password");
        if (Password.Length < 3) throw new ArgumentException("Password must be at least 3 characters long");
    }

    public void UpdateRecommendationPreference(bool receiveRecommendations)
    {
        ReceiveRecommendations = receiveRecommendations;
    }

    public string GetPrimaryRoleName()
    {
        return Role.ToString().ToLower();
    }

    public void IncrementFailedLoginAttempts()
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= 5)
        {
            BlockUser();
        }
    }

    public void ResetFailedLoginAttempts()
    {
        FailedLoginAttempts = 0;
    }

    private void BlockUser()
    {
        IsActive = false;
        BlockCount++;
        FailedLoginAttempts = 0;
    }

    public bool CanBeUnblocked()
    {
        return BlockCount < 3;
    }

    public void Unblock()
    {
        if (!CanBeUnblocked())
        {
            throw new InvalidOperationException("User has been blocked 3 times and cannot be unblocked.");
        }
        IsActive = true;
        FailedLoginAttempts = 0;
    }
}

public enum UserRole
{
    Administrator,
    Author,
    Tourist
}