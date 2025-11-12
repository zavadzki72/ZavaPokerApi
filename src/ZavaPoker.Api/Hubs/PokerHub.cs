using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
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
            var room = await _pokerService.CreateRoom(roomName, votePackageId, userName);
            await SendEventSignal("RoomCreated", room);
        }

        public async Task JoinRoom(Guid roomId, string userName)
        {
            await _pokerService.JoinRoom(roomId, userName);
            await SendEventSignal("UserJoined", new { roomId, userName });
        }

        public async Task LeaveRoom(string userName)
        {
            await _pokerService.LeaveRoom(userName);
            await SendEventSignal("UserLeft", userName);
        }

        public async Task StartRound(Guid roomId)
        {
            var round = await _pokerService.StartRound(roomId);
            await SendEventSignal("RoundStarted", new { roomId, round });
        }

        public async Task SubmitVote(string userName, string voteValue)
        {
            var vote = await _pokerService.SubmitVote(userName, voteValue);
            await SendEventSignal("VoteSubmitted", new { userName, voteValue, vote });
        }

        public async Task RevealCards(Guid roomId)
        {
            await _pokerService.RevealCards(roomId);
            await SendEventSignal("CardsRevealed", roomId);
        }

        public async Task DestroyRoom(Guid roomId)
        {
            await _pokerService.DestroyRoom(roomId);
            await SendEventSignal("RoomDestroyed", roomId);
        }

        private async Task SendEventSignal(string eventName, object eventValue)
        {
            var eventJson = JsonSerializer.Serialize(eventValue);
            _logger.LogInformation("Sending event {EventName} with value {EventValue}", eventName, eventJson);

            await Clients.Caller.SendAsync(eventName, eventJson);
        }
    }
}
