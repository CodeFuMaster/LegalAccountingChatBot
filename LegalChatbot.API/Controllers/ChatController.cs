using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LegalChatbot.API.Models;
using LegalChatbot.API.Repositories;
using LegalChatbot.API.Services.LLM;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LegalChatbot.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly ILLMService _llmService;
        private readonly ILegalDocumentRepository _documentRepository;
        private readonly ILogger<ChatController> _logger;

        public ChatController(
            ILLMService llmService, 
            ILegalDocumentRepository documentRepository, 
            ILogger<ChatController> logger)
        {
            _llmService = llmService;
            _documentRepository = documentRepository;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<ChatResponse>> PostChatMessage([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("Message cannot be empty");
            }

            try
            {
                _logger.LogInformation($"Received chat request: {request.Message} (Language: {request.Language})");
                
                // Search for relevant documents based on the user's query
                var relevantDocuments = await _documentRepository.SearchDocumentsAsync(
                    request.Message, 
                    request.Language
                );
                
                _logger.LogInformation($"Found {relevantDocuments.Count} relevant documents for query");
                
                // Get response from LLM service
                var responseText = await _llmService.GenerateResponseAsync(
                    request.Message,
                    relevantDocuments,
                    request.Language ?? "en"
                );
                
                // Get document categories for suggestions
                var categories = await _documentRepository.GetAvailableCategoriesAsync(request.Language);
                
                // Create response with suggested topics if no documents were found
                var response = new ChatResponse
                {
                    Message = responseText,
                    Timestamp = DateTime.UtcNow,
                    SourceDocuments = relevantDocuments.Count > 0 
                        ? relevantDocuments.ConvertAll(d => new DocumentReference 
                        { 
                            Id = d.Id, 
                            Title = d.Title, 
                            Year = d.Year,
                            Category = d.Category
                        }) 
                        : new List<DocumentReference>(),
                    SuggestedTopics = relevantDocuments.Count == 0 
                        ? categories.ConvertAll(c => $"Questions about {c}")
                        : new List<string>()
                };
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat request");
                return StatusCode(500, new ChatResponse 
                { 
                    Message = "An error occurred while processing your request.",
                    Error = true,
                    Timestamp = DateTime.UtcNow
                });
            }
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public string? Language { get; set; }
    }

    public class ChatResponse
    {
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool Error { get; set; } = false;
        public List<DocumentReference> SourceDocuments { get; set; } = new List<DocumentReference>();
        public List<string> SuggestedTopics { get; set; } = new List<string>();
    }

    public class DocumentReference
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Category { get; set; } = string.Empty;
    }
}