using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegalChatbot.API.Models;
using LegalChatbot.API.Repositories;
using LegalChatbot.API.Services.LLM;

namespace LegalChatbot.API.Services
{
    public interface IChatRAGService
    {
        Task<ChatResponse> ProcessChatRequestAsync(ChatRequest request);
    }

    public class ChatRAGService : IChatRAGService
    {
        private readonly ILegalDocumentRepository _repository;
        private readonly ILLMService _llmService;

        public ChatRAGService(ILegalDocumentRepository repository, ILLMService llmService)
        {
            _repository = repository;
            _llmService = llmService;
        }

        public async Task<ChatResponse> ProcessChatRequestAsync(ChatRequest request)
        {
            // 1. Retrieve relevant documents based on the query
            var relevantDocuments = await _repository.SearchDocumentsAsync(request.UserMessage, request.Language);
            
            // 2. Generate a response using the LLM service
            string responseMessage = await _llmService.GenerateResponseAsync(
                request.UserMessage, 
                relevantDocuments, 
                request.Language
            );
            
            // 3. Create and return the ChatResponse
            return new ChatResponse
            {
                ResponseMessage = responseMessage,
                Sources = relevantDocuments,
                ConfidenceScore = CalculateConfidenceScore(relevantDocuments, request.UserMessage),
                Language = request.Language,
                SessionId = Guid.NewGuid().ToString(), // Generate a new session ID
                Metadata = request.Metadata // Pass through any metadata
            };
        }

        private double CalculateConfidenceScore(List<LegalDocument> documents, string query)
        {
            // Simple confidence score calculation (0.0 to 1.0)
            if (documents == null || !documents.Any())
                return 0.0;
                
            // Calculate based on the number of documents and their relevance
            double maxRelevance = documents.Max(d => CalculateRelevanceScore(d, query));
            double normalizedScore = Math.Min(maxRelevance / 10.0, 1.0); // Normalize to 0-1 range
            
            return normalizedScore;
        }

        private double CalculateRelevanceScore(LegalDocument document, string query)
        {
            // Simple relevance scoring based on term frequency
            // In a real system, this would use vector embeddings and semantic similarity
            int titleMatches = CountOccurrences(document.Title.ToLower(), query.ToLower());
            int contentMatches = CountOccurrences(document.Content.ToLower(), query.ToLower());
            
            // Title matches are weighted more heavily
            return titleMatches * 2 + contentMatches;
        }

        private int CountOccurrences(string text, string query)
        {
            int count = 0;
            int index = 0;
            
            while ((index = text.IndexOf(query, index, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                count++;
                index += query.Length;
            }
            
            return count;
        }
    }
}