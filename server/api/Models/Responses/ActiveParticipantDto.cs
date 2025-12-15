namespace api.Models.Responses;

public class ActiveParticipantDto
{
    public string PlayerId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<int> Numbers { get; set; } = new();
    public int Matches { get; set; }
}
