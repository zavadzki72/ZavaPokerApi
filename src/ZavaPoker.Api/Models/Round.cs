namespace ZavaPoker.Api.Models
{
    public class Round
    {
        public Round(Room room)
        {
            Id = Guid.CreateVersion7();
            IsVisible = false;
            IsActive = true;

            Room = room;
        }

        public Guid Id { get; init; }
        public bool IsVisible { get; private set; }
        public bool IsActive { get; private set; }
        public Room Room { get; init; }

        public void RevealCards()
        {
            IsVisible = true;
        }

        public void EndRound()
        {
            IsActive = false;
        }
    }
}
