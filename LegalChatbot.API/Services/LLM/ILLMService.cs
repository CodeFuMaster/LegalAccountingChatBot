using System.Threading.Tasks;
using LegalChatbot.API.Models;

namespace LegalChatbot.API.Services.LLM
{
    public interface ILLMService
    {
        /// <summary>
        /// Generate a response using an LLM with the given context and query
        /// </summary>
        Task<string> GenerateResponseAsync(string userQuery, List<LegalDocument> documents, string language);
    }
}