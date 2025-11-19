using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using ZavaPoker.Api.Models;
using ZavaPoker.Api.Services;

namespace ZavaPoker.Api.Hubs
{
    public class PokerHub : Hub
    {
        private readonly PokerService _pokerService;
        private readonly ILogger<PokerHub> _logger;

        public PokerHub(PokerService pokerService, ILogger<PokerHub> logger)
        {
            _logger = logger;
            _pokerService = pokerService;
        }

        public List<VotePackage> GetVotePackages()
        {
            return _pokerService.GetVotePackages();
        }

        public object? GetUsersInRoom(Guid roomId)
        {
            var users = _pokerService.GetUsersByRoom(roomId);
            var room = _pokerService.GetRoomById(roomId);

            if (room == null)
                return null;

            var currentRound = room.GetCurrentRound();
            var userDtoList = users.Select(x =>
            {
                var vote = currentRound?.Votes.FirstOrDefault(v => v.Voter.Id == x.Id);

                return new UserDto(
                    x.Id,
                    x.Name,
                    room.Owner.Id == x.Id,
                    x.IsSpec ? "Spectator" : "Player",
                    vote != null,
                    currentRound?.IsVisible == true ? vote?.Value : null
                );
            }).ToList();

            return new
            {
                roomId,
                roomName = room.Name,
                votePackage = room.VotePackage,
                areCardsRevealed = currentRound?.IsVisible ?? false,
                users = userDtoList
            };
        }

        public async Task<Guid> CreateRoom(string roomName, Guid votePackageId, string userName)
        {
            var room = _pokerService.CreateRoom(roomName, votePackageId, userName) 
                ?? throw new HubException($"Não foi possível criar a sala. O usuário '{userName}' já pode estar em outra sala.");

            await Groups.AddToGroupAsync(Context.ConnectionId, room.Id.ToString());
            await UpdateUserList(room.Id);

            return room.Id;
        }

        public async Task JoinRoom(Guid roomId, string userName)
        {
            var room = _pokerService.JoinRoom(roomId, userName);
            if (room == null)
                return;

            await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());
            await UpdateUserList(roomId);
        }

        public async Task LeaveRoom(string userName)
        {
            var roomId = _pokerService.LeaveRoom(userName);

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId.ToString());
            await UpdateUserList(roomId);
        }

        public async Task StartRound(Guid roomId)
        {
            var round = _pokerService.StartRound(roomId);

            if (round == null)
                return;

            await UpdateUserList(roomId);
        }

        public async Task SubmitVote(string userName, string voteValue)
        {
            var vote = _pokerService.SubmitVote(userName, voteValue);

            if (vote == null)
                return;

            await SendEventSignal("VoteSubmitted", new { userName, voteValue, vote }, "group", vote.Round.Room.Id);
            await UpdateUserList(vote.Round.Room.Id);
        }

        public async Task RevealCards(Guid roomId)
        {
            _pokerService.RevealCards(roomId);
            await UpdateUserList(roomId);
        }

        public async Task DestroyRoom(Guid roomId)
        {
            _pokerService.DestroyRoom(roomId);
            await SendEventSignal("RoomDestroyed", roomId, "group", roomId);
        }

        public async Task ToggleOwner(Guid roomId, string newOwnerUserName)
        {
            _pokerService.ToggleOwner(roomId, newOwnerUserName);
            await SendEventSignal("OwnerToggled", new { roomId, newOwnerUserName }, "group", roomId);
            await UpdateUserList(roomId);
        }

        public async Task ChangeRole(Guid roomId, string userName)
        {
            _pokerService.ChangeRole(roomId, userName);
            await SendEventSignal("RoleChanged", new { roomId, userName }, "group", roomId);
            await UpdateUserList(roomId);
        }

        public async Task ChangeVotePackage(Guid roomId, Guid votePackageId)
        {
            var votePackage = _pokerService.ChangeVotePackage(roomId, votePackageId);

            if (votePackage == null)
                return;

            await UpdateUserList(roomId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }

        private async Task SendEventSignal(string eventName, object eventValue, string destination, Guid? groupId = null)
        {
            var eventJson = JsonSerializer.Serialize(eventValue);
            _logger.LogInformation("Sending event {EventName} with value {EventValue}", eventName, eventJson);

            switch (destination)
            {
                case "caller":
                    await Clients.Caller.SendAsync(eventName, eventJson);
                    break;
                case "group":
                    await Clients.Group(groupId.ToString()!).SendAsync(eventName, eventJson);
                    break;
                default:
                    await Clients.All.SendAsync(eventName, eventJson);
                    break;
            }
        }

        private async Task UpdateUserList(Guid roomId)
        {
            var users = _pokerService.GetUsersByRoom(roomId);
            var room = _pokerService.GetRoomById(roomId);

            if (room == null) 
                return;

            var currentRound = room.GetCurrentRound();
            var userDtoList = users.Select(x =>
            {
                var vote = currentRound?.Votes.FirstOrDefault(v => v.Voter.Id == x.Id);

                return new UserDto(
                    x.Id,
                    x.Name,
                    room.Owner.Id == x.Id,
                    x.IsSpec ? "Spectator" : "Player",
                    vote != null,
                    currentRound?.IsVisible == true ? vote?.Value : null
                );
            }).ToList();

            var roomStatePayload = new
            {
                roomId,
                roomName = room.Name,
                votePackage = room.VotePackage,
                areCardsRevealed = currentRound?.IsVisible ?? false,
                users = userDtoList
            };

            var eventJson = JsonSerializer.Serialize(roomStatePayload);
            _logger.LogInformation("Updating user list (full state): {EventValue}", eventJson);

            await Clients.Group(roomId.ToString()).SendAsync("UpdateUserList", eventJson);
        }
    }

    public class UserDto
    {
        public UserDto(Guid userId, string name, bool isOwner, string role, bool hasVoted, string? vote)
        {
            UserId = userId;
            Name = name;
            IsOwner = isOwner;
            Role = role;
            HasVoted = hasVoted;
            Vote = vote;
        }

        public Guid UserId { get; init; }
        public string Name { get; init; }
        public bool IsOwner { get; init; }
        public string Role { get; init; }
        public bool HasVoted { get; init; } 
        public string? Vote { get; init; }
    }
}