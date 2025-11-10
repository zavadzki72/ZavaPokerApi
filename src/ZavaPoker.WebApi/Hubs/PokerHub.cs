using Microsoft.AspNetCore.SignalR;
using ZavaPoker.WebApi.Models;
using ZavaPoker.WebApi.Services;

namespace ZavaPoker.WebApi.Hubs
{
    public class PokerHub : Hub
    {
        private readonly RoomService _roomService;

        public PokerHub(RoomService roomService)
        {
            _roomService = roomService;
        }

        public async Task JoinRoom(string roomName, string userName)
        {
            var user = new User(Context.ConnectionId, userName);

            var room = _roomService.GetOrCreateRoom(roomName);
            _roomService.AddUserToRoom(room.Name, user);

            await Groups.AddToGroupAsync(Context.ConnectionId, room.Name);

            var sanitizedUserList = GetSanitizedUserList(room);
            await Clients.Group(room.Name).SendAsync("UpdateUserList", sanitizedUserList);
        }

        public async Task SendVote(string roomName, string vote)
        {
            var (allVoted, user) = _roomService.RecordVote(Context.ConnectionId, vote);

            if (user != null)
            {
                await Clients.Group(roomName).SendAsync("UserVoted", user.Name);

                if (allVoted)
                {
                    await Clients.Group(roomName).SendAsync("AllUsersVoted");
                }
            }
        }

        public async Task RevealVotes(string roomName)
        {
            if (_roomService.RevealVotes(roomName))
            {
                var room = _roomService.GetRoom(roomName)!;
                var updatedList = GetSanitizedUserList(room);

                await Clients.Group(roomName).SendAsync("UpdateUserList", updatedList);
            }
        }

        public async Task ResetVotes(string roomName)
        {
            if (_roomService.ResetVotes(roomName))
            {
                var room = _roomService.GetRoom(roomName)!;
                var updatedList = GetSanitizedUserList(room);

                await Clients.Group(roomName).SendAsync("UpdateUserList", updatedList);
                await Clients.Group(roomName).SendAsync("VotesReset");
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var (room, user) = _roomService.RemoveUser(Context.ConnectionId);

            if (room != null && user != null)
            {
                var sanitizedUserList = GetSanitizedUserList(room);
                await Clients.Group(room.Name).SendAsync("UpdateUserList", sanitizedUserList);
            }

            await base.OnDisconnectedAsync(exception);
        }

        private static object GetSanitizedUserList(Room room)
        {
            var userList = room.Users.Values.Select(u => new
            {
                u.ConnectionId,
                u.Name,
                HasVoted = (u.Vote != null),
                Vote = room.VotesRevealed ? u.Vote : null
            }).ToList();

            return userList;
        }
    }
}
