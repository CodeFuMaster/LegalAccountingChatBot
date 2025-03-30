using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LegalChatbot.API.Models;

namespace LegalChatbot.API.Repositories
{
    public class InMemoryLegalDocumentRepository : ILegalDocumentRepository
    {
        private readonly List<LegalDocument> _documents;

        public InMemoryLegalDocumentRepository()
        {
            // Initialize with some mock legal documents
            _documents = new List<LegalDocument>
            {
                new LegalDocument
                {
                    Id = 1,
                    Title = "Закон за Данок На Додадена Вредност",
                    Content = "Член 1: Со овој закон се уредува оданочувањето на потрошувачката со данок на додадена вредност (во натамошниот текст: ДДВ), како општ данок на потрошувачката кој се пресметува и плаќа во сите фази на производството и трговијата, како и во целокупниот услужен сектор. Член 2: ДДВ се пресметува со примена на пропорционални даночни стапки и тоа: општа даночна стапка од 18% и повластена даночна стапка од 5%.",
                    Year = 2023,
                    Language = "mk",
                    Category = "Taxation"
                },
                new LegalDocument
                {
                    Id = 2,
                    Title = "Value Added Tax Law",
                    Content = "Article 1: This law regulates the taxation of consumption with value added tax (hereinafter: VAT), as a general consumption tax that is calculated and paid in all stages of production and trade, as well as in the entire service sector. Article 2: VAT is calculated by applying proportional tax rates, specifically: general tax rate of 18% and preferential tax rate of 5%.",
                    Year = 2023,
                    Language = "en",
                    Category = "Taxation"
                },
                new LegalDocument
                {
                    Id = 3,
                    Title = "Закон за Корпорации",
                    Content = "Член 1: Овој закон ги регулира основањето, организирањето и функционирањето на трговските друштва. Член 2: Трговско друштво може да биде основано како: јавно трговско друштво, командитно друштво, друштво со ограничена одговорност, акционерско друштво и командитно друштво со акции. Член 3: Трговските друштва се правни лица кои самостојно настапуваат во правниот промет.",
                    Year = 2022,
                    Language = "mk",
                    Category = "Corporate Law"
                },
                new LegalDocument
                {
                    Id = 4,
                    Title = "Corporate Law",
                    Content = "Article 1: This law regulates the establishment, organization, and functioning of business companies. Article 2: A business company can be established as: public trading company, limited partnership, limited liability company, joint-stock company, and limited partnership with shares. Article 3: Business companies are legal entities that act independently in legal transactions.",
                    Year = 2022,
                    Language = "en",
                    Category = "Corporate Law"
                },
                new LegalDocument
                {
                    Id = 5,
                    Title = "Закон за Данок На Додадена Вредност (Стар)",
                    Content = "Член 1: Со овој закон се уредува оданочувањето на потрошувачката со данок на додадена вредност (во натамошниот текст: ДДВ), како општ данок на потрошувачката. Член 2: ДДВ се пресметува со примена на пропорционални даночни стапки и тоа: општа даночна стапка од 15% и повластена даночна стапка од 5%.",
                    Year = 2010,
                    Language = "mk",
                    Category = "Taxation"
                },
                new LegalDocument
                {
                    Id = 6,
                    Title = "Закон за Работни Односи",
                    Content = "Член 1: Со овој закон се уредуваат работните односи меѓу работниците и работодавачите кои се воспоставуваат со склучување на договор за вработување. Член 5: Работникот има право на плата, безбедност при работа, и дневен, неделен и годишен одмор.",
                    Year = 2021,
                    Language = "mk",
                    Category = "Labor Law"
                },
                new LegalDocument
                {
                    Id = 7,
                    Title = "Labor Relations Law",
                    Content = "Article 1: This law regulates labor relations between workers and employers that are established by concluding an employment contract. Article 5: The worker has the right to salary, safety at work, and daily, weekly and annual leave.",
                    Year = 2021,
                    Language = "en",
                    Category = "Labor Law"
                }
            };
        }

        public Task<List<LegalDocument>> GetAllDocumentsAsync()
        {
            return Task.FromResult(_documents);
        }

        public Task<LegalDocument?> GetDocumentByIdAsync(int id)
        {
            return Task.FromResult(_documents.FirstOrDefault(d => d.Id == id));
        }

        public Task<List<string>> GetAvailableCategoriesAsync(string? language = null)
        {
            var categories = _documents
                .Where(d => string.IsNullOrEmpty(language) || d.Language.Equals(language, StringComparison.OrdinalIgnoreCase))
                .Select(d => d.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
                
            return Task.FromResult(categories);
        }

    public Task<List<LegalDocument>> SearchDocumentsAsync(string query, string? language = null, string? category = null)
    {
      var results = _documents.AsQueryable();

      // Filter by language if specified
      if (!string.IsNullOrEmpty(language))
      {
        results = results.Where(d => d.Language.Equals(language, StringComparison.OrdinalIgnoreCase));
      }

      // Filter by category if specified
      if (!string.IsNullOrEmpty(category))
      {
        results = results.Where(d => d.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
      }

      // Simple search implementation
      if (!string.IsNullOrEmpty(query))
      {
        // Convert query to lowercase for case-insensitive search
        string lowerQuery = query.ToLowerInvariant();

        // Extract key terms from the query (words with 3+ characters)
        var keyTerms = Regex.Matches(lowerQuery, @"\b\w{3,}\b")
            .Cast<Match>()
            .Select(m => m.Value)
            .ToList();

        if (keyTerms.Any())
        {
          // Search for documents that contain any of the key terms
          results = results.Where(d =>
              keyTerms.Any(term =>
                  d.Title.ToLowerInvariant().Contains(term) ||
                  d.Content.ToLowerInvariant().Contains(term)));

          // Score documents based on term frequency and recency
          var scoredResults = results.Select(d => new
          {
            Document = d,
            // Score based on term frequency in title (weighted higher) and content
            TermScore = keyTerms.Sum(term =>
                (d.Title.ToLowerInvariant().Contains(term) ? 3 : 0) +
                CountOccurrences(d.Content.ToLowerInvariant(), term)),
            // Add recency bonus
            RecencyScore = Math.Max(0, (d.Year - 2000) / 5.0) // More recent documents score higher
          })
          .OrderByDescending(r => r.TermScore + r.RecencyScore)
          .Select(r => r.Document)
          .ToList();

          return Task.FromResult(scoredResults);
        }
      }

      // If no search terms or no matches, return all documents filtered by language/category
      return Task.FromResult(results.OrderByDescending(d => d.Year).ToList());
    }

    private int CountOccurrences(string text, string term)
        {
            int count = 0;
            int index = 0;
            
            while ((index = text.IndexOf(term, index, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                count++;
                index += term.Length;
            }
            
            return count;
        }

        public Task<List<LegalDocument>> GetSimilarDocumentsAsync(int documentId, int count = 3)
        {
            var sourceDoc = _documents.FirstOrDefault(d => d.Id == documentId);
            
            if (sourceDoc == null)
            {
                return Task.FromResult(new List<LegalDocument>());
            }
            
            // Get documents in the same category and language
            var similarDocs = _documents
                .Where(d => d.Id != documentId && 
                           d.Category == sourceDoc.Category && 
                           d.Language == sourceDoc.Language)
                .OrderByDescending(d => d.Year)
                .Take(count)
                .ToList();
                
            return Task.FromResult(similarDocs);
        }
    }
}