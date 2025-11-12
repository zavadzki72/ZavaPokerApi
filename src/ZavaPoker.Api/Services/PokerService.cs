using ZavaPoker.Api.Hubs;
using ZavaPoker.Api.Models;

namespace ZavaPoker.Api.Services
{
    public class PokerService
    {
        private readonly List<VotePackage> _votePackages;

        private readonly List<User> _users = [];
        private readonly List<Room> _rooms = [];

        private readonly Logger<PokerHub> _logger;

        public PokerService(Logger<PokerHub> logger)
        {
            _votePackages = PopulateVotePackages();
            _logger = logger;
        }

        public async Task<List<VotePackage>> GetVotePackages()
        {
            return _votePackages;
        }

        public async Task<Room?> CreateRoom(string roomName, Guid votePackageId, string userName)
        {
            _logger.LogInformation("Creating room {RoomName} with vote package {VotePackageId} for user {UserName}", roomName, votePackageId, userName);

            var votePackage = _votePackages.First(vp => vp.Id == votePackageId);
            var user = GetOrCreateUser(userName);

            if (user.IsInRoom())
            {
                _logger.LogWarning("User {UserName} is already in a room and cannot create a new one", userName);
                return null;
            }

            var room = new Room(roomName, votePackage, user);
            _rooms.Add(room);

            _logger.LogInformation("Room {RoomName} created successfully", roomName);

            return room;
        }

        public async Task JoinRoom(Guid roomId, string userName)
        {
            _logger.LogInformation("User {UserName} is joining room {RoomId}", userName, roomId);

            var room = _rooms.FirstOrDefault(x => x.Id == roomId);
            if (room == null)
            {
                _logger.LogWarning("Room {RoomId} not found", roomId);
                return;
            }

            var user = GetOrCreateUser(userName);
            if (user.IsInRoom())
            {
                _logger.LogWarning("User {UserName} is already in a room and cannot join another", userName);
                return;
            }

            room.AddUser(user);
            _logger.LogInformation("User {UserName} joined room {RoomName} successfully", userName, room.Name);
        }

        public async Task LeaveRoom(string userName)
        {
            _logger.LogInformation("User {UserName} is leaving their current room", userName);

            var user = GetOrCreateUser(userName);
            if (!user.IsInRoom())
            {
                _logger.LogWarning("User {UserName} is not in any room and cannot leave", userName);
                return;
            }

            user.CurrentRoom!.RemoveUser(user);
            _logger.LogInformation("User {UserName} removed from room {RoomName}", userName, user.CurrentRoom.Name);

            if (user.CurrentRoom!.Users.Count == 1)
            {
                _rooms.Remove(user.CurrentRoom);
                _logger.LogInformation("Room {RoomName} deleted as it had no more users", user.CurrentRoom.Name);
            }

            _logger.LogInformation("User {UserName} left room {RoomName} successfully", userName, user.CurrentRoom!.Name);
        }

        public async Task<Round?> StartRound(Guid roomId)
        {
            var room = _rooms.FirstOrDefault(x => x.Id == roomId);
            if (room == null)
            {
                _logger.LogWarning("Room {RoomId} not found", roomId);
                return null;
            }

            room.NewRound();

            _logger.LogInformation("New round started in room {RoomName}", room.Name);

            return room.GetCurrentRound();
        }

        public async Task<Vote?> SubmitVote(string userName, string voteValue)
        {
            _logger.LogInformation("User {UserName} is submitting vote {VoteValue}", userName, voteValue);

            var user = GetOrCreateUser(userName);
            if (!user.IsInRoom())
            {
                _logger.LogWarning("User {UserName} is not in any room and cannot submit a vote", userName);
                return null;
            }

            var room = user.CurrentRoom!;
            var round = room.GetCurrentRound();
            if (round == null)
            {
                _logger.LogWarning("No active round in room {RoomName} for user {UserName} to submit a vote", room.Name, userName);
                return null;
            }

            var vote = new Vote(user, round, voteValue);

            _logger.LogInformation("User {UserName} submitted vote {VoteValue} successfully in room {RoomName}", userName, voteValue, room.Name);

            return vote;
        }

        public async Task RevealCards(Guid roomId)
        {
            var room = _rooms.FirstOrDefault(x => x.Id == roomId);
            if (room == null)
            {
                _logger.LogWarning("Room {RoomId} not found", roomId);
                return;
            }

            room.RevealCards();

            _logger.LogInformation("Cards revealed in room {RoomName}", room.Name);
        }

        public async Task DestroyRoom(Guid roomId)
        {
            var room = _rooms.FirstOrDefault(x => x.Id == roomId);
            if (room == null)
            {
                _logger.LogWarning("Room {RoomId} not found", roomId);
                return;
            }

            room.Destroy();
            _rooms.Remove(room);

            _logger.LogInformation("Room {RoomName} destroyed successfully", room.Name);
        }

        private User GetOrCreateUser(string userName)
        {
            var user = _users.FirstOrDefault(u => u.Name.Equals(userName, StringComparison.InvariantCultureIgnoreCase));

            if (user != null)
            {
                return user;
            }

            user = new User(userName);
            _users.Add(user);

            return user;
        }

        private static List<VotePackage> PopulateVotePackages()
        {
            return [
                new VotePackage(Guid.CreateVersion7(), "Fibonacci", ["1", "2", "3", "5", "8", "13", "21", "34", "55", "89", "144"])
            ];
        }
    }
}
