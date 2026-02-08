namespace Explorer.Stakeholders.API.Dtos;

public class BlockedUserDto
{
    public long Id { get; set; }
    public string Username { get; set; }
    public string Name { get; set; }
    public string Surname { get; set; }
    public string Role { get; set; }
    public int BlockCount { get; set; }
    public bool CanBeUnblocked { get; set; }
}
