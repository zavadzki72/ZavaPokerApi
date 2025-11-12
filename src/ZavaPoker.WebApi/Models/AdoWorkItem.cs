namespace ZavaPoker.WebApi.Models
{
    public class AdoWorkItem
    {
        public AdoWorkItem(string id, string type, string title, string url, string description)
        {
            Id = id;
            Type = type;
            Title = title;
            Url = url;
            Description = description;
        }

        public string Id { get; private set; }
        public string Type { get; private set; }
        public string Title { get; private set; }
        public string Url { get; private set; }
        public string Description { get; private set; }
    }
}