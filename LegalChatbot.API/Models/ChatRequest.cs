using System.Collections.Generic;

namespace LegalChatbot.API.Models
{
    public class ChatRequest
    {
        public required string UserId { get; set; }
        public required string UserMessage { get; set; }
        public required string Language { get; set; }
        public required Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}