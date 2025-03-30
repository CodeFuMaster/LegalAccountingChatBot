using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegalChatbot.API.Models;
using Microsoft.Extensions.Configuration;
using OpenAI.Extensions;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.Interfaces;

namespace LegalChatbot.API.Services.LLM
{
  public class LegalChatbotAIService : ILLMService
  {
    private readonly IOpenAIService _openAIService;
    private readonly IConfiguration _configuration;
    private readonly string _modelName;

    // Store recent conversation history
    private readonly List<ChatMessage> _conversationHistory;
    private readonly int _maxHistoryLength;

    public LegalChatbotAIService(IOpenAIService openAIService, IConfiguration configuration)
    {
      _openAIService = openAIService;
      _configuration = configuration;
      _modelName = _configuration["ChatbotSettings:ModelName"] ?? "gpt-3.5-turbo";
      _conversationHistory = new List<ChatMessage>();
      _maxHistoryLength = int.Parse(_configuration["ChatbotSettings:MaxHistoryLength"] ?? "10");
      
      Console.WriteLine($"LegalChatbotAIService initialized with model: {_modelName}");
    }

    public async Task<string> GenerateResponseAsync(string userQuery, List<LegalDocument> documents, string language)
    {
      // Classify the query type
      var queryType = ClassifyQuery(userQuery);

      try
      {
        switch (queryType)
        {
          case QueryType.Greeting:
            return HandleGreeting(language);

          case QueryType.GeneralQuestion:
            return await HandleGeneralQuestion(userQuery, language);

          case QueryType.LegalQuestion:
          default:
            return await HandleLegalQuestion(userQuery, documents, language);
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error generating response: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");

        if (ex.InnerException != null)
        {
          Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
        }

        // Fallback to a simple response if there's an API error
        return language?.ToLower() == "en"
            ? "Sorry, an error occurred while processing your request. Please try again later."
            : "Жал ми е, се случи грешка при обработката на вашето барање. Ве молиме обидете се повторно подоцна.";
      }
    }

    private QueryType ClassifyQuery(string query)
    {
      // Simple classification logic for now
      query = query.ToLower().Trim();

      // Check for greetings
      string[] greetings = new[] { "hi", "hello", "hey", "здраво", "добар ден", "здрава", "поздрав" };
      if (greetings.Any(g => query.Contains(g)) && query.Split().Length <= 3)
      {
        return QueryType.Greeting;
      }

      // Check for general questions (not specific to legal documents)
      string[] generalIndicators = new[] { "what can you do", "who are you", "how does this work", "што можеш да правиш", "кој си ти", "како работи ова" };
      if (generalIndicators.Any(g => query.Contains(g)))
      {
        return QueryType.GeneralQuestion;
      }

      // Default to legal question
      return QueryType.LegalQuestion;
    }

    private string HandleGreeting(string language)
    {
      if (language?.ToLower() == "en")
      {
        return "Hello! I'm your legal assistant for North Macedonian legal and accounting matters. How can I help you today? You can ask me about VAT, corporate law, or other legal topics in North Macedonia.";
      }
      else
      {
        return "Здраво! Јас сум вашиот правен асистент за правни и сметководствени прашања во Северна Македонија. Како можам да ви помогнам денес? Можете да ме прашате за ДДВ, корпоративно право или други правни теми во Северна Македонија.";
      }
    }

    private async Task<string> HandleGeneralQuestion(string userQuery, string language)
    {
      var promptBuilder = new StringBuilder();

      if (language?.ToLower() == "en")
      {
        promptBuilder.AppendLine("The user is asking a general question about the chatbot or its capabilities. Please respond in a friendly, helpful way.");
        promptBuilder.AppendLine("Remember you are a legal and accounting assistant for North Macedonia.");
        promptBuilder.AppendLine($"User question: {userQuery}");
      }
      else
      {
        promptBuilder.AppendLine("Корисникот поставува општо прашање за чет-ботот или неговите способности. Одговорете на пријателски, корисен начин.");
        promptBuilder.AppendLine("Запомнете дека сте правен и сметководствен асистент за Северна Македонија.");
        promptBuilder.AppendLine($"Прашање на корисникот: {userQuery}");
      }

      var chatCompletionRequest = new ChatCompletionCreateRequest
      {
        Messages = new List<ChatMessage>
                {
                    ChatMessage.FromSystem(GetSystemPrompt(language)),
                    ChatMessage.FromUser(promptBuilder.ToString())
                },
        Model = _modelName,
        Temperature = 0.7f,
        MaxTokens = 500
      };

      var response = await _openAIService.ChatCompletion.CreateCompletion(chatCompletionRequest);

      if (response.Successful)
      {
        return response.Choices.First().Message.Content;
      }
      else
      {
        return language?.ToLower() == "en"
            ? "I'm a legal assistant that can help you with North Macedonian legal and accounting questions. How can I assist you today?"
            : "Јас сум правен асистент кој може да ви помогне со правни и сметководствени прашања во Северна Македонија. Како можам да ви помогнам денес?";
      }
    }

    private async Task<string> HandleLegalQuestion(string userQuery, List<LegalDocument> documents, string language)
    {
      // If no documents found, engage in a more helpful conversation
      if (documents == null || !documents.Any())
      {
        var noDocsPrompt = language?.ToLower() == "en"
            ? $"The user asked: '{userQuery}', but I couldn't find any specific legal documents about this topic. Please respond helpfully, explaining that you don't have specific information on this topic, but offer suggestions for related legal areas they might want to ask about instead."
            : $"Корисникот праша: '{userQuery}', но не можев да најдам конкретни правни документи на оваа тема. Одговорете корисно, објаснувајќи дека немате конкретни информации на оваа тема, но понудете предлози за сродни правни области за кои тие би можеле да прашаат наместо тоа.";

        var chatRequest = new ChatCompletionCreateRequest
        {
          Messages = new List<ChatMessage>
                    {
                        ChatMessage.FromSystem(GetSystemPrompt(language)),
                        ChatMessage.FromUser(noDocsPrompt)
                    },
          Model = _modelName,
          Temperature = 0.7f,
          MaxTokens = 500
        };

        var response = await _openAIService.ChatCompletion.CreateCompletion(chatRequest);

        if (response.Successful)
        {
          return response.Choices.First().Message.Content;
        }
        else
        {
          return language?.ToLower() == "en"
              ? "I don't have specific information about this topic in my legal database. Could you ask about another legal or accounting matter in North Macedonia?"
              : "Немам конкретни информации за оваа тема во мојата правна база на податоци. Дали можете да прашате за друго правно или сметководствено прашање во Северна Македонија?";
        }
      }

      // Build a prompt for the LLM with context from the retrieved documents
      string prompt = BuildPromptWithContext(userQuery, documents, language);

      // Update conversation history with user query
      _conversationHistory.Add(ChatMessage.FromUser(userQuery));

      // Ensure we don't exceed the maximum history length
      while (_conversationHistory.Count > _maxHistoryLength)
      {
        _conversationHistory.RemoveAt(0);
      }

      // Prepare messages for the chat completion request
      var messages = new List<ChatMessage>
            {
                ChatMessage.FromSystem(GetSystemPrompt(language)),
            };

      // Add conversation history if available (for better context)
      if (_conversationHistory.Count > 0)
      {
        // Only add recent history, limited to avoid token limits
        foreach (var message in _conversationHistory.Skip(Math.Max(0, _conversationHistory.Count - 4)))
        {
          messages.Add(message);
        }
      }

      // Add the current prompt with document context
      messages.Add(ChatMessage.FromUser(prompt));

      try
      {
        // Log the model name and other configurations being used
        Console.WriteLine($"LLM Request: Using model '{_modelName}' with {documents.Count} documents");

        // Set up chat completion request
        var chatCompletionRequest = new ChatCompletionCreateRequest
        {
          Messages = messages,
          Model = _modelName,
          Temperature = float.Parse(_configuration["ChatbotSettings:Temperature"] ?? "0.7"),
          MaxTokens = int.Parse(_configuration["ChatbotSettings:MaxTokens"] ?? "1000")
        };

        // Log detailed request information
        Console.WriteLine($"Sending request to LLM API. UseLocalModel: {_configuration["OpenAISettings:UseLocalModel"]}, ModelEndpoint: {_configuration["OpenAISettings:LocalModelUrl"]}");

        var response = await _openAIService.ChatCompletion.CreateCompletion(chatCompletionRequest);

        if (response.Successful)
        {
          Console.WriteLine("LLM response received successfully!");
          var responseContent = response.Choices.First().Message.Content;

          // Update conversation history with assistant's response
          _conversationHistory.Add(ChatMessage.FromAssistant(responseContent));

          return responseContent;
        }
        else
        {
          Console.WriteLine($"OpenAI API Error: {response.Error?.Message}");
          Console.WriteLine($"Error Code: {response.Error?.Code}, Type: {response.Error?.Type}");

          // Fallback to a simple response if there's an API error
          return language?.ToLower() == "en"
              ? "I encountered an issue while processing your legal question. Could you please rephrase or try a different question?"
              : "Наидов на проблем при обработката на вашето правно прашање. Дали би можеле да го преформулирате или да пробате со различно прашање?";
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error calling OpenAI API: {ex.Message}");

        // Fallback to a simple response if there's an API error
        return language?.ToLower() == "en"
            ? "I'm having trouble accessing my legal database right now. Please try again in a moment."
            : "Имам проблеми со пристапот до мојата правна база на податоци во моментов. Ве молам обидете се повторно за момент.";
      }
    }

    private string BuildPromptWithContext(string userQuery, List<LegalDocument> documents, string language)
    {
      StringBuilder contextBuilder = new StringBuilder();

      // Add an introduction to the context based on language
      if (language?.ToLower() == "en")
      {
        contextBuilder.AppendLine("Relevant legal documents and information:");
        contextBuilder.AppendLine();
      }
      else
      {
        contextBuilder.AppendLine("Релевантни правни документи и информации:");
        contextBuilder.AppendLine();
      }

      // Add the content from each document to provide context
      foreach (var doc in documents.Take(3)) // Limit to top 3 documents
      {
        contextBuilder.AppendLine($"DOCUMENT: {doc.Title} ({doc.Year})");
        contextBuilder.AppendLine($"CONTENT: {doc.Content}");
        contextBuilder.AppendLine();
      }

      // Add the user query
      if (language?.ToLower() == "en")
      {
        contextBuilder.AppendLine($"User question: {userQuery}");
        contextBuilder.AppendLine("\nBased on the information above, please answer the user's question clearly and concisely in English. Format your response with appropriate paragraphs, bullet points, or numbered lists where it makes the information easier to understand. Always cite the source documents in your response.");
      }
      else
      {
        contextBuilder.AppendLine($"Прашање на корисникот: {userQuery}");
        contextBuilder.AppendLine("\nВрз основа на горенаведените информации, одговорете на прашањето на корисникот јасно и концизно на македонски јазик. Форматирајте го вашиот одговор со соодветни параграфи, точки или нумерирани листи каде што тоа ја прави информацијата полесна за разбирање. Секогаш цитирајте ги изворните документи во вашиот одговор.");
      }

      return contextBuilder.ToString();
    }

    private string GetSystemPrompt(string language)
    {
      if (language?.ToLower() == "en")
      {
        return @"You are an intelligent and friendly legal and accounting assistant providing accurate information about legal and accounting matters in North Macedonia. 

Answer using information provided in the context when available. If you cannot find an answer based on the given information, be honest about the limitations and suggest what kind of information might help.

Always cite the source documents in your response. 

Make your responses conversational and engaging, not just factual. Use friendly language that non-experts can understand while still being precise about legal matters.

Answer in English.";
      }
      else
      {
        return @"Вие сте интелигентен и пријателски правен и сметководствен асистент кој обезбедува точни информации за правни и сметководствени прашања во Северна Македонија. 

Одговарајте со користење на информациите дадени во контекстот кога се достапни. Ако не можете да најдете одговор врз основа на дадените информации, бидете искрени за ограничувањата и предложете каков вид на информации би можеле да помогнат.

Секогаш цитирајте ги изворните документи во вашиот одговор. 

Направете ги вашите одговори разговорни и привлечни, не само фактички. Користете пријателски јазик што и не-експертите можат да го разберат, а сепак да бидете прецизни за правните прашања.

Одговорете на македонски јазик.";
      }
    }
  }

  public enum QueryType
  {
    Greeting,
    GeneralQuestion,
    LegalQuestion
  }
}