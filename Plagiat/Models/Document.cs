using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Plagiat.Models
{
    public class Document
    {
        public int Id { get; set; }
        
        [Required]
        public string Title { get; set; }
        
        public string Content { get; set; }
        
        public string OriginalFileName { get; set; }
        
        public string FileFormat { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime? LastModified { get; set; }
        
        public double UniquenessPercentage { get; set; }
        
        public DocumentStatus Status { get; set; }
        
        public string Language { get; set; }
        
        // Связь с проектом
        public int? ProjectId { get; set; }
        public virtual Project Project { get; set; }
        
        public virtual ICollection<PlagiarismResult> PlagiarismResults { get; set; }
        
        public virtual ICollection<Citation> Citations { get; set; }
        
        public Document()
        {
            CreatedAt = DateTime.Now;
            Status = DocumentStatus.New;
            Content = ""; // Инициализируем пустой строкой
            PlagiarismResults = new List<PlagiarismResult>();
            Citations = new List<Citation>();
        }
    }

    public enum DocumentStatus
    {
        New,
        Analyzing,
        Analyzed,
        Processing,
        Completed,
        Error
    }
}
