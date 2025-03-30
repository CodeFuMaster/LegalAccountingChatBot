using System.Collections.Generic;

namespace LegalChatbot.API.Models
{
    public class ChatResponse
    {
        public required string ResponseMessage { get; set; }
        public List<LegalDocument> Sources { get; set; } = new List<LegalDocument>();
        public double ConfidenceScore { get; set; }
        public required string Language { get; set; }
        public required string SessionId { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}