namespace AuthAPI.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public bool Active { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}