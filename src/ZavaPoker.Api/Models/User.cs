namespace ZavaPoker.Api.Models
{
    public class User
    {
        public User(string name)
        {
            Id = Guid.CreateVersion7();

            Name = name;
            IsSpec = true;
        }

        public Guid Id { get; init; }
        public string Name { get; private set; }
        public bool IsSpec { get; private set; }

        public Room? CurrentRoom { get; private set; }

        public void SetCurrentRoom(Room? room)
        {
            CurrentRoom = room;
        }

        public bool IsInRoom()
        {
            return CurrentRoom != null;
        }

        public void ChangeRole()
        {
            IsSpec = !IsSpec;
        }
    }
}
