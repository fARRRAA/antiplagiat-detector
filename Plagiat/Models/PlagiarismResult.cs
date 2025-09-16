using System;
using System.ComponentModel.DataAnnotations;

namespace Plagiat.Models
{
    public class PlagiarismResult
    {
        public int Id { get; set; }
        
        public int DocumentId { get; set; }
        
        [Required]
        public string MatchedText { get; set; }
        
        public int StartPosition { get; set; }
        
        public int EndPosition { get; set; }
        
        public double SimilarityPercentage { get; set; }
        
        public string SourceUrl { get; set; }
        
        public string SourceTitle { get; set; }
        
        public string SourceAuthor { get; set; }
        
        public DateTime? SourceDate { get; set; }
        
        public PlagiarismLevel Level { get; set; }
        
        public bool IsExcluded { get; set; }
        
        public string ExclusionReason { get; set; }
        
        public bool IsProcessed { get; set; }
        
        public virtual Document Document { get; set; }
        
        public PlagiarismResult()
        {
            Level = GetLevelBySimilarity(SimilarityPercentage);
        }
        
        private PlagiarismLevel GetLevelBySimilarity(double percentage)
        {
            if (percentage >= 70) return PlagiarismLevel.Critical;
            if (percentage >= 30) return PlagiarismLevel.Warning;
            return PlagiarismLevel.Acceptable;
        }
        
        public void UpdateLevel()
        {
            Level = GetLevelBySimilarity(SimilarityPercentage);
        }
        
        public override string ToString()
        {
            var levelText = Level switch
            {
                PlagiarismLevel.Critical => "КРИТИЧНО",
                PlagiarismLevel.Warning => "ВНИМАНИЕ", 
                PlagiarismLevel.Acceptable => "ДОПУСТИМО",
                _ => "НЕИЗВЕСТНО"
            };
            
            var shortText = MatchedText?.Length > 50 
                ? MatchedText.Substring(0, 50) + "..." 
                : MatchedText ?? "Не указано";
                
            return $"{levelText} ({SimilarityPercentage:F1}%): {shortText}";
        }
    }

    public enum PlagiarismLevel
    {
        Acceptable,  // < 30% - Зеленый
        Warning,     // 30-70% - Желтый  
        Critical     // > 70% - Красный
    }
}

