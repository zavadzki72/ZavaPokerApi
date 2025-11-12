using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using System.Text.RegularExpressions;
using ZavaPoker.Api.Models;
using ZavaPoker.Api.Services;

namespace ZavaPoker.Api.Hubs
{
    public class PokerHub : Hub
    {
        private readonly PokerService _pokerService;
        private readonly Logger<PokerHub> _logger;

        public PokerHub(PokerService pokerService, Logger<PokerHub> logger)
        {
            _logger = logger;
            _pokerService = pokerService;
        }

        public async Task CreateRoom(string roomName, Guid votePackageId, string userName)
        {
            var room = _pokerService.CreateRoom(roomName, votePackageId, userName);

            if (room == null)
                return;

            await UpdateUserList(room.Id, _pokerService.GetUsersByRoom(room.Id));
        }

        public async Task JoinRoom(Guid roomId, string userName)
        {
            _pokerService.JoinRoom(roomId, userName);
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());
            await UpdateUserList(roomId, _pokerService.GetUsersByRoom(roomId));
        }

        public async Task LeaveRoom(string userName)
        {
            var room = _pokerService.LeaveRoom(userName);

            if (room == null)
                return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, room.Id.ToString());
            await UpdateUserList(room.Id, _pokerService.GetUsersByRoom(room.Id));
        }

        public async Task StartRound(Guid roomId)
        {
            var round = _pokerService.StartRound(roomId);

            if (round == null)
                return;

            await SendEventSignal("RoundStarted", new { roomId, round }, "group", round.Room.Id);
        }

        public async Task SubmitVote(string userName, string voteValue)
        {
            var vote = _pokerService.SubmitVote(userName, voteValue);

            if (vote == null)
                return;

            await SendEventSignal("VoteSubmitted", new { userName, voteValue, vote }, "group", vote.Round.Room.Id);
        }

        public async Task RevealCards(Guid roomId)
        {
            _pokerService.RevealCards(roomId);
            await SendEventSignal("CardsRevealed", roomId, "group", roomId);
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
            await UpdateUserList(roomId, _pokerService.GetUsersByRoom(roomId));
        }

        public async Task ChangeRole(Guid roomId, string userName)
        {
            _pokerService.ChangeRole(roomId, userName);
            await SendEventSignal("RoleChanged", new { roomId, userName }, "group", roomId);
            await UpdateUserList(roomId, _pokerService.GetUsersByRoom(roomId));
        }

        public async Task ChangeVotePackage(Guid roomId, Guid votePackageId)
        {
            var votePackage = _pokerService.ChangeVotePackage(roomId, votePackageId);

            if (votePackage == null)
                return;

            await SendEventSignal("VotePackageChanged", new { roomId, votePackage }, "group", roomId);
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

        private async Task UpdateUserList(Guid roomId, List<User> users)
        {
            var eventJson = JsonSerializer.Serialize(new {roomId, users});
            _logger.LogInformation("Updating user list: {EventValue}", eventJson);

            await Clients.Group(roomId.ToString()).SendAsync("UpdateUserList", eventJson);
        }
    }
}
