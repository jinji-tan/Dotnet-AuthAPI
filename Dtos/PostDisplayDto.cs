namespace AuthAPI.Dtos
{
    public class PostDisplayDto
    {
        public int PostId { get; set; }
        public string PostTitle { get; set; } = "";
        public string PostContent { get; set; } = "";
        public int CommentCount { get; set; }
        public DateTime PostCreated { get; set; }

    }
}