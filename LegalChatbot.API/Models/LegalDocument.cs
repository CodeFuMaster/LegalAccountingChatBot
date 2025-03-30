using System;

namespace LegalChatbot.API.Models
{
  public class LegalDocument
  {
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Language { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime? LastUpdated { get; set; }
    public string? Source { get; set; }
    public bool IsActive { get; set; } = true;
  }
}