namespace _2PeopleTB.DAL.Models
{
    public class MessageHistory
    {
        public long Id { get; set; }
        public long FromChatId { get; set; }
        public long ToChatId { get; set; }
        public int MessageId { get; set; }
        public string MessageType { get; set; } = string.Empty;
        public string? TextContent { get; set; }
        public string? FileId { get; set; }
        public DateTime SentAt { get; set; }
    }
}
