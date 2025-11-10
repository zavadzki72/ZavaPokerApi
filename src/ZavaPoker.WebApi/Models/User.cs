namespace ZavaPoker.WebApi.Models
{
    public class User
    {
        public User(string connectionId, string name)
        {
            ConnectionId = connectionId;
            Name = name;
            IsSpectator = false;
        }

        public string ConnectionId { get; init; }
        public string Name { get; private set; }
        public string? Vote { get; private set; }
        public bool IsSpectator { get; private set; }

        public void SetVote(string vote)
        {
            Vote = vote;
        }

        public void ResetVote()
        {
            Vote = null;
        }
    }
}
