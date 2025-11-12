// Ficheiro: ZavaPoker.WebApi/Models/Room.cs

using System.Collections.Concurrent;

namespace ZavaPoker.WebApi.Models
{
    public class Room
    {
        public Room(string name)
        {
            Name = name;
            VotesRevealed = false;
        }

        public string Name { get; init; }
        public bool VotesRevealed { get; private set; }

        // ATUALIZADO: A chave (Key) agora é o 'UserId'
        public ConcurrentDictionary<string, User> Users { get; set; } = new();

        public void SetVotesRevealed(bool votesRevealed)
        {
            VotesRevealed = votesRevealed;
        }
    }
}