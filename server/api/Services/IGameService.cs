using api.Models.Requests;
using dataccess;

namespace api.Services;

public interface IGameService
{
    Task<Player> CreatePlayer(CreatePlayerRequestDto dto);
    Task<List<Playerboard>> CreatePlayerBoards(string playerId, CreatePlayerBoardsRequestDto dto);
    Task<List<Player>> GetPlayers();
    Task<Player?> GetPlayerById(string id);
    Task<Player> UpdatePlayer(string id, UpdatePlayerRequestDto dto);
    Task ChangePassword(string playerId, string currentPassword, string newPassword);
    Task<Player> SoftDeletePlayer(string id);
    Task<Player> RestorePlayer(string id);
    // Admins
    Task<Admin> SoftDeleteAdmin(string id);
    Task<Admin> RestoreAdmin(string id);
    Task<Board?> GetActiveBoard();
    Task<List<Board>> GetRecentBoards(int take = 10);
    Task<List<api.Models.Responses.ActiveParticipantDto>> GetActiveParticipants();
    Task<List<api.Models.Responses.GameHistoryDto>> GetGameHistory(int take = 10, string? playerId = null, bool isAdmin = false);

    // Boards / Draw
    Task DrawWinningNumbersAndAdvance(string adminId, IReadOnlyCollection<int> numbers);
    Task<Board> ActivateBoard(string boardId);
    Task<Board> DeactivateBoard(string boardId);

    // Fund Requests
    Task<dataccess.Fundrequest> CreateFundRequest(string playerId, decimal amount, string transactionNumber);
    Task<List<dataccess.Fundrequest>> GetFundRequests(string? status = null);
    Task<dataccess.Fundrequest> ApproveFundRequest(string requestId, string adminId);
    Task<dataccess.Fundrequest> DenyFundRequest(string requestId, string adminId);
}
