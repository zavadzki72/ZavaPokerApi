namespace ZavaPoker.Api.Models
{
    public class Room
    {
        public Room(string name, VotePackage votePackage, User owner)
        {
            Id = Guid.CreateVersion7();

            Name = name;
            VotePackage = votePackage;
            Owner = owner;

            Users.Add(owner);
        }

        public Guid Id { get; init; }
        public string Name { get; private set; }
        public VotePackage VotePackage { get; private set; }
        public User Owner { get; private set; }

        public List<User> Users { get; private set; } = [];
        public List<Round> Rounds { get; private set; } = [];

        public void AddUser(User user)
        {
            Users.Add(user);
        }

        public void RemoveUser(User user)
        {
            if(user.Id != Owner.Id || Users.Count == 1)
            {
                Users.Remove(user);
                return;
            }

            var newOner = Users.OrderBy(x => x.IsSpec).First(u => u.Id != Owner.Id);
            Owner = newOner;
            Users.Remove(user);
        }

        public void NewRound()
        {
            var currentRound = Rounds.FirstOrDefault(r => r.IsActive);
            currentRound?.EndRound();

            var round = new Round(this);
            Rounds.Add(round);
        }

        public void RevealCards()
        {
            var currentRound = Rounds.FirstOrDefault(r => r.IsActive);
            currentRound?.RevealCards();
        }

        public Round? GetCurrentRound()
        {
            return Rounds.FirstOrDefault(r => r.IsActive);
        }

        public void ToggleOwner(User newOwner)
        {
            Owner = newOwner;
        }

        public void UpdateVotePackage(VotePackage newVotePackage)
        {
            VotePackage = newVotePackage;
        }

        public void Destroy()
        {
            Users.Clear();
            Rounds.Clear();
        }
    }
}
