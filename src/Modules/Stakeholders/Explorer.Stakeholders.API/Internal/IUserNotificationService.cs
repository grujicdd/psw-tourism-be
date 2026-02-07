namespace Explorer.Stakeholders.API.Public.Internal;

public interface IUserNotificationService
{
    List<string> GetInterestedUserEmails(int category);
}
