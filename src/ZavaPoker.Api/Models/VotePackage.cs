namespace ZavaPoker.Api.Models
{
    public class VotePackage
    {
        public VotePackage(Guid id, string name, List<string> items)
        {
            Id = id;
            Name = name;
            Items = items;
        }

        public Guid Id { get; init; }
        public string Name { get; init; }
        public List<string> Items { get; init; }
    }
}
