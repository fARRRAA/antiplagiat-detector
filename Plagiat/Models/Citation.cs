using System;
using System.ComponentModel.DataAnnotations;

namespace Plagiat.Models
{
    public class Citation
    {
        public int Id { get; set; }
        
        public int DocumentId { get; set; }
        
        [Required]
        public string QuotedText { get; set; }
        
        public int StartPosition { get; set; }
        
        public int EndPosition { get; set; }
        
        public CitationType Type { get; set; }
        
        public int SourceId { get; set; }
        
        public string PageNumber { get; set; }
        
        public bool IsFormatted { get; set; }
        
        public CitationStyle Style { get; set; }
        
        public virtual Document Document { get; set; }
        
        public virtual Source Source { get; set; }
    }

    public enum CitationType
    {
        Direct,      // Прямая цитата
        Indirect,    // Косвенная цитата/пересказ
        Block,       // Блочная цитата
        Epigraph  ,
        Reference// Эпиграф
    }

    public enum CitationStyle
    {
        GOST,
        APA,
        MLA,
        Chicago,
        Harvard,
        Vancouver,
        IEEE,
        Nature
    }
}

