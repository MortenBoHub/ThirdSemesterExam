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
    public List<PlayerBoardHistoryDto> PlayerBoards { get; set; } = new();
    public List<WinnerDetailDto> WinnerDetails { get; set; } = new();
}

public class WinnerDetailDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phonenumber { get; set; } = string.Empty;
}

public class PlayerBoardHistoryDto
{
    public string Id { get; set; } = string.Empty;
    public List<int> SelectedNumbers { get; set; } = new();
    public bool IsWinner { get; set; }
}
