namespace AuthAPI.Models
{
    public class Post
    {
        public int PostId { get; set; }
        public int UserId { get; set; }
        public string PostContent { get; set; } = "";
        public DateTime PostCreated { get; set; }
        public DateTime PostUpdated { get; set; }
    }
}