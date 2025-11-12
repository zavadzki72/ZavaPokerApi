namespace ZavaPoker.Api.Models
{
    public class Vote
    {
        public Vote(User voter, Round round, string value)
        {
            Voter = voter;
            Round = round;
            Value = value;
        }

        public User Voter { get; init; }
        public Round Round { get; init; }
        public string Value { get; private set; }
    }
}
