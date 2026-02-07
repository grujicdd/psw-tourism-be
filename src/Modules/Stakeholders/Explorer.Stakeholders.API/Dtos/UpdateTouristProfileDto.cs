namespace Explorer.Stakeholders.API.Dtos;

public class UpdateTouristProfileDto
{
    public List<int> InterestIds { get; set; }
    public bool ReceiveRecommendations { get; set; }

    public UpdateTouristProfileDto()
    {
        InterestIds = new List<int>();
    }
}
