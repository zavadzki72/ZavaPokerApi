// Ficheiro: ZavaPoker.WebApi/Services/RoomService.cs
// (Corrigido: Adicionado o método GetUser(connectionId) em falta)

using System.Collections.Concurrent;
using ZavaPoker.WebApi.Models;

namespace ZavaPoker.WebApi.Services
{
    public class RoomService
    {
        private readonly ConcurrentDictionary<string, Room> _rooms = new();

        // Mapeamento de 'ConnectionId' (volátil) para 'UserId' (persistente)
        private readonly ConcurrentDictionary<string, string> _connectionToUserIdMap = new();
        // Mapeamento de 'UserId' (persistente) para 'RoomName'
        private readonly ConcurrentDictionary<string, string> _userRoomMap = new();

        public Room GetOrCreateRoom(string roomName)
        {
            return _rooms.GetOrAdd(roomName, _ => new Room(roomName));
        }

        // Lógica para encontrar um utilizador na sala pelo UserId (para F5)
        public User? GetUserInRoomByUserId(string roomName, string userId)
        {
            if (_rooms.TryGetValue(roomName, out var room) && room.Users.TryGetValue(userId, out var user))
            {
                return user;
            }
            return null;
        }

        // Lógica para "reconectar" um utilizador que deu F5
        public User ReconnectUser(User user, string newConnectionId)
        {
            // Remove o mapeamento antigo de 'ConnectionId'
            var oldConnectionId = _connectionToUserIdMap.FirstOrDefault(x => x.Value == user.UserId).Key;
            if (oldConnectionId != null)
            {
                _connectionToUserIdMap.TryRemove(oldConnectionId, out _);
            }

            // Adiciona o novo mapeamento e atualiza o ConnectionId
            _connectionToUserIdMap[newConnectionId] = user.UserId;
            user.ConnectionId = newConnectionId;

            return user;
        }

        // Lógica de Adicionar Utilizador (R1: Criador é ADM)
        public void AddUserToRoom(string roomName, User user)
        {
            var room = GetOrCreateRoom(roomName);

            // Requisito 1: Se a sala está vazia, o primeiro a entrar é ADM
            if (room.Users.IsEmpty)
            {
                user.IsAdm = true;
            }

            // A CHAVE da sala agora é o UserId, não o ConnectionId
            room.Users.TryAdd(user.UserId, user);
            _connectionToUserIdMap[user.ConnectionId] = user.UserId;
            _userRoomMap[user.UserId] = roomName;
        }

        // Lógica de Votar (usa ConnectionId para encontrar UserId)
        public (bool AllVoted, User? User) RecordVote(string connectionId, string vote)
        {
            if (!_connectionToUserIdMap.TryGetValue(connectionId, out var userId) ||
                !_userRoomMap.TryGetValue(userId, out var roomName) ||
                !_rooms.TryGetValue(roomName, out var room) ||
                !room.Users.TryGetValue(userId, out var user))
            {
                return (false, null);
            }

            if (user.IsSpectator) return (false, user);
            user.SetVote(vote);

            var votingUsers = room.Users.Values.Where(u => !u.IsSpectator);
            bool allVoted = votingUsers.All(u => u.Vote != null);
            return (allVoted, user);
        }

        // Lógica de Remover Utilizador (R3: Lógica de promoção)
        public (Room? Room, User? User) RemoveUser(string connectionId)
        {
            // Encontra o UserId e o RoomName usando o ConnectionId
            if (!_connectionToUserIdMap.TryRemove(connectionId, out var userId) ||
                !_userRoomMap.TryRemove(userId, out var roomName) ||
                !_rooms.TryGetValue(roomName, out var room) ||
                !room.Users.TryRemove(userId, out var user)) // Remove da sala usando o UserId
            {
                return (null, null);
            }

            // Se a sala ficar vazia, remove-a
            if (room.Users.IsEmpty)
            {
                _rooms.TryRemove(roomName, out _);
            }
            // Requisito 3: Se o Admin saiu, passa o bastão
            else if (user.IsAdm)
            {
                // R3: "dando preferencia aos espectadores"
                var nextAdm = room.Users.Values.FirstOrDefault(u => u.IsSpectator)
                              ?? room.Users.Values.FirstOrDefault(); // Se não houver espectador, pega o primeiro

                if (nextAdm != null)
                {
                    nextAdm.IsAdm = true;
                }
            }

            return (room, user);
        }

        // Lógica de Transferir Admin (R5: Lógica de transferência)
        public bool TransferAdminRole(string callerConnectionId, string newAdminUserId)
        {
            // Encontra o chamador (Admin atual)
            if (!_connectionToUserIdMap.TryGetValue(callerConnectionId, out var callerUserId) ||
                !_userRoomMap.TryGetValue(callerUserId, out var roomName) ||
                !_rooms.TryGetValue(roomName, out var room) ||
                !room.Users.TryGetValue(callerUserId, out var currentAdmin))
            {
                return false; // Chamador não encontrado
            }

            // Encontra o alvo (Novo Admin)
            if (!room.Users.TryGetValue(newAdminUserId, out var newAdmin))
            {
                return false; // Alvo não encontrado
            }

            // R5: "posso transferir... desde que ela seja espectadora"
            // E o chamador tem que ser Admin
            if (!currentAdmin.IsAdm || !newAdmin.IsSpectator)
            {
                return false;
            }

            // R5: "eu devo perder o ADM"
            currentAdmin.IsAdm = false;
            newAdmin.IsAdm = true;

            return true;
        }

        // --- Outros Métodos ---

        public bool RevealVotes(string roomName)
        {
            if (_rooms.TryGetValue(roomName, out var room))
            {
                room.SetVotesRevealed(true);
                return true;
            }
            return false;
        }

        public bool ResetVotes(string roomName)
        {
            if (_rooms.TryGetValue(roomName, out var room))
            {
                room.SetVotesRevealed(false);
                foreach (var user in room.Users.Values)
                {
                    user.ResetVote();
                }
                return true;
            }
            return false;
        }

        public Room? GetRoom(string roomName)
        {
            _rooms.TryGetValue(roomName, out var room);
            return room;
        }

        public User? UpdateUserRole(string connectionId, string role)
        {
            if (!_connectionToUserIdMap.TryGetValue(connectionId, out var userId) ||
                !_userRoomMap.TryGetValue(userId, out var roomName) ||
                !_rooms.TryGetValue(roomName, out var room) ||
                !room.Users.TryGetValue(userId, out var user))
            {
                return null;
            }

            bool isSpectator = (role == "espectador");
            user.SetSpectator(isSpectator);
            if (isSpectator)
            {
                user.ResetVote();
            }
            return user;
        }

        // NOVO MÉTODO (O que faltava e causou o erro CS1061)
        public User? GetUser(string connectionId)
        {
            if (!_connectionToUserIdMap.TryGetValue(connectionId, out var userId) ||
                !_userRoomMap.TryGetValue(userId, out var roomName) ||
                !_rooms.TryGetValue(roomName, out var room) ||
                !room.Users.TryGetValue(userId, out var user))
            {
                return null;
            }
            return user;
        }
    }
}