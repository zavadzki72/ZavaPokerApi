using Microsoft.AspNetCore.SignalR;
using ZavaPoker.WebApi.Models;
using ZavaPoker.WebApi.Services;

namespace ZavaPoker.WebApi.Hubs
{
    public record UserJoinDto(string UserId, string UserName, string RoomId, string UserRole);

    public class PokerHub : Hub
    {
        private readonly RoomService _roomService;
        private readonly AdoService _adoService;

        public PokerHub(RoomService roomService, AdoService adoService)
        {
            _roomService = roomService;
            _adoService = adoService;
        }

        public async Task JoinRoom(UserJoinDto userDto)
        {
            User user;
            var room = _roomService.GetOrCreateRoom(userDto.RoomId);

            // R4: Lógica de F5 / Reconnect
            var existingUser = _roomService.GetUserInRoomByUserId(room.Name, userDto.UserId);

            if (existingUser != null)
            {
                // É um F5! Apenas atualiza o ConnectionId
                user = _roomService.ReconnectUser(existingUser, Context.ConnectionId);
            }
            else
            {
                // É um utilizador novo na sala
                user = new User(Context.ConnectionId, userDto.UserId, userDto.UserName, userDto.UserRole);
                _roomService.AddUserToRoom(room.Name, user); // R1 (Admin) é tratado aqui
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, room.Name);

            var sanitizedUserList = GetSanitizedUserList(room);

            // --- Pacote de Boas-Vindas (SÓ para o chamador) ---
            await Clients.Caller.SendAsync("ReceiveAdminStatus", user.IsAdm);
            await Clients.Caller.SendAsync("ReceiveRevealState", room.VotesRevealed);
            await Clients.Caller.SendAsync("UpdateUserList", sanitizedUserList);

            // --- Atualização (SÓ para os outros) ---
            await Clients.OthersInGroup(room.Name).SendAsync("UpdateUserList", sanitizedUserList);
        }

        public async Task SubmitVote(string roomName, string vote)
        {
            var (allVoted, user) = _roomService.RecordVote(Context.ConnectionId, vote);

            if (user != null)
            {
                var room = _roomService.GetRoom(roomName)!;
                var updatedList = GetSanitizedUserList(room);
                await Clients.Group(roomName).SendAsync("UpdateUserList", updatedList);
            }
        }

        public async Task ToggleVotes(string roomName)
        {
            var user = _roomService.GetUser(Context.ConnectionId);
            var room = _roomService.GetRoom(roomName);

            if (user == null || !user.IsAdm || room == null) return;

            bool newState;

            if (room.VotesRevealed)
            {
                // Estava REVELADO, agora vai RESETAR
                _roomService.ResetVotes(roomName);
                newState = false;

                // NOVO: Limpa o item ADO para todos quando a votação é resetada
                await Clients.Group(roomName).SendAsync("ReceiveWorkItem", null);
            }
            else
            {
                // Estava ESCONDIDO, agora vai REVELAR
                _roomService.RevealVotes(roomName);
                newState = true;
            }

            await Clients.Group(roomName).SendAsync("ReceiveRevealState", newState);

            var updatedList = GetSanitizedUserList(room);
            await Clients.Group(roomName).SendAsync("UpdateUserList", updatedList);
        }

        public async Task ClearWorkItem(string roomName)
        {
            var user = _roomService.GetUser(Context.ConnectionId);
            if (user == null || !user.IsAdm) 
                return; // Só Admin

            // Limpa o item ADO para todos
            await Clients.Group(roomName).SendAsync("ReceiveWorkItem", null);
        }

        public async Task ChangeRole(string roomName, string newRole)
        {
            var user = _roomService.UpdateUserRole(Context.ConnectionId, newRole);

            if (user != null)
            {
                var room = _roomService.GetRoom(roomName)!;
                var updatedList = GetSanitizedUserList(room);
                await Clients.Group(roomName).SendAsync("UpdateUserList", updatedList);
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
                u.ConnectionId, // Ainda é útil para debug
                UserId = u.UserId, // NOVO: O ID persistente
                Name = u.Name,
                u.IsAdm,
                Role = u.IsSpectator ? "espectador" : "votante",
                HasVoted = (u.Vote != null),
                Vote = room.VotesRevealed ? u.Vote : null
            }).ToList();

            return userList;
        }

        public async Task ChangeVotePack(string roomName, string newPackName)
        {
            var user = _roomService.GetUser(Context.ConnectionId);
            if (user == null || !user.IsAdm) 
                return;

            _roomService.ResetVotes(roomName);
            await Clients.Group(roomName).SendAsync("ReceiveVotePack", newPackName);
            await Clients.Group(roomName).SendAsync("ReceiveRevealState", false);

            var room = _roomService.GetRoom(roomName)!;
            var updatedList = GetSanitizedUserList(room);
            await Clients.Group(roomName).SendAsync("UpdateUserList", updatedList);
        }

        public async Task LoadWorkItem(string roomName, string workItemId)
        {
            var user = _roomService.GetUser(Context.ConnectionId);
            if (user == null || !user.IsAdm) 
                return;

            _roomService.ResetVotes(roomName);

            var item = await _adoService.GetWorkItemDetails(workItemId);
            await Clients.Group(roomName).SendAsync("ReceiveWorkItem", item);
            await Clients.Group(roomName).SendAsync("ReceiveRevealState", false);

            var room = _roomService.GetRoom(roomName)!;
            var updatedList = GetSanitizedUserList(room);
            await Clients.Group(roomName).SendAsync("UpdateUserList", updatedList);
        }

        public async Task TransferAdmin(string roomName, string newAdminUserId)
        {
            // O ID do novo admin agora é o 'UserId', não o 'ConnectionId'
            bool success = _roomService.TransferAdminRole(Context.ConnectionId, newAdminUserId);

            if (success)
            {
                var room = _roomService.GetRoom(roomName)!;
                var updatedList = GetSanitizedUserList(room);
                await Clients.Group(roomName).SendAsync("UpdateUserList", updatedList);
            }
        }
    }
}