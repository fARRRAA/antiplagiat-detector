using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Plagiat.Models
{
    public class Project
    {
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; }
        
        public string Description { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime? LastModified { get; set; }
        
        public ProjectStatus Status { get; set; }
        
        public CitationStyle DefaultCitationStyle { get; set; }
        
        public string Settings { get; set; } // JSON настройки
        
        public virtual ICollection<Document> Documents { get; set; }
        
        public virtual ICollection<Source> FavoritesSources { get; set; }
        
        public Project()
        {
            CreatedAt = DateTime.Now;
            Status = ProjectStatus.Active;
            DefaultCitationStyle = CitationStyle.GOST;
            Documents = new List<Document>();
            FavoritesSources = new List<Source>();
        }
    }

    public enum ProjectStatus
    {
        Active,
        Completed,
        Archived,
        Deleted
    }
}

