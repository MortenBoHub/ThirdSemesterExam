using dataccess;

namespace api.Models.Responses;

public class GameHistoryDto
{
    public string BoardId { get; set; } = string.Empty;
    public int Week { get; set; }
    public int Year { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<int> Numbers { get; set; } = new();
    public int Participants { get; set; }
    public int Winners { get; set; }
}
