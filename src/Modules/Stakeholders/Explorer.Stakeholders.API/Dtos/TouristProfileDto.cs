namespace Explorer.Stakeholders.API.Dtos;

public class TouristProfileDto
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Surname { get; set; }
    public string Email { get; set; }
    public List<int> InterestIds { get; set; }
    public bool ReceiveRecommendations { get; set; }

    public TouristProfileDto()
    {
        InterestIds = new List<int>();
    }
}
