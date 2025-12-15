using System.ComponentModel.DataAnnotations;

namespace api.Models.Requests;

public record CreatePlayerBoardsRequestDto
{
    [Required]
    [MinLength(5)]
    [MaxLength(8)]
    public List<int> SelectedNumbers { get; set; } = new();

    [Range(1, 52)]
    public int RepeatWeeks { get; set; } = 1;
}
