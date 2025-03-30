using System.Collections.Generic;
using System.Threading.Tasks;
using LegalChatbot.API.Models;

namespace LegalChatbot.API.Repositories
{
  public interface ILegalDocumentRepository
  {
    Task<List<LegalDocument>> GetAllDocumentsAsync();
    Task<LegalDocument?> GetDocumentByIdAsync(int id);
    Task<List<LegalDocument>> SearchDocumentsAsync(string query, string? language = null, string? category = null);
    Task<List<string>> GetAvailableCategoriesAsync(string? language = null);
    Task<List<LegalDocument>> GetSimilarDocumentsAsync(int documentId, int count = 3);
  }
}