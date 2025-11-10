using System.Collections.Concurrent;
using ZavaPoker.WebApi.Models;

namespace ZavaPoker.WebApi.Services
{
    public class RoomService
    {
        private readonly ConcurrentDictionary<string, Room> _rooms = new();
        private readonly ConcurrentDictionary<string, string> _userRoomMap = new();

        public Room GetOrCreateRoom(string roomName)
        {
            return _rooms.GetOrAdd(roomName, _ => new Room(roomName));
        }

        public void AddUserToRoom(string roomName, User user)
        {
            var room = GetOrCreateRoom(roomName);
            room.Users.TryAdd(user.ConnectionId, user);
            _userRoomMap.TryAdd(user.ConnectionId, roomName);
        }

        public (bool AllVoted, User? User) RecordVote(string connectionId, string vote)
        {
            if (!_userRoomMap.TryGetValue(connectionId, out var roomName))
            {
                return (false, null);
            }

            if (!_rooms.TryGetValue(roomName, out var room))
            {
                return (false, null);
            }

            if (room.Users.TryGetValue(connectionId, out var user))
            {
                if (user.IsSpectator) return (false, user);

                user.SetVote(vote);

                var votingUsers = room.Users.Values.Where(u => !u.IsSpectator);
                bool allVoted = votingUsers.All(u => u.Vote != null);

                return (allVoted, user);
            }

            return (false, null);
        }

        public (Room? Room, User? User) RemoveUser(string connectionId)
        {
            if (_userRoomMap.TryRemove(connectionId, out var roomName) && _rooms.TryGetValue(roomName, out var room) && room.Users.TryRemove(connectionId, out var user))
            {
                return (room, user);
            }

            return (null, null);
        }

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
    }
}
