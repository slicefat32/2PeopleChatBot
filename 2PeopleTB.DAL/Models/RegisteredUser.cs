namespace _2PeopleTB.DAL.Models
{
    public class RegisteredUser
    {
        public long ChatId { get; set; }
        public string Username { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; }
    }
}
