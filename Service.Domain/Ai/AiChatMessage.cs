namespace Service.Domain.Ai
{
    public class AiChatMessage
    {
        public string? Role { get; set; }
        public string? Content { get; set; }
        public long SentAt { get; set; }
    }
}
