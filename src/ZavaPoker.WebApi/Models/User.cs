// Ficheiro: ZavaPoker.WebApi/Models/User.cs

namespace ZavaPoker.WebApi.Models
{
    public class User
    {
        // ATUALIZADO: Construtor aceita UserId
        public User(string connectionId, string userId, string name, string role)
        {
            ConnectionId = connectionId; // Este vai mudar a cada F5
            UserId = userId; // Este é o ID persistente
            Name = name;
            IsSpectator = (role == "espectador");
            IsAdm = false;
        }

        public string ConnectionId { get; set; } // Volátil (para o SignalR)
        public string UserId { get; init; } // Persistente (para a "pessoa")
        public string Name { get; private set; }
        public string? Vote { get; private set; }
        public bool IsSpectator { get; private set; }
        public bool IsAdm { get; set; }

        public void SetVote(string vote)
        {
            Vote = vote;
        }

        public void ResetVote()
        {
            Vote = null;
        }

        public void SetSpectator(bool isSpectator)
        {
            IsSpectator = isSpectator;
        }
    }
}