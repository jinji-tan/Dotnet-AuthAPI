namespace AuthAPI.Models
{
    public class Comment
    {
        public int CommentId { get; set; }
        public int PostId { get; set; }
        public int UserId { get; set; }
        public string CommentContent { get; set; } = "";
        public DateTime CommentCreated { get; set; }
    }
}