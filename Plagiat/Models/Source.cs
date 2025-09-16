using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Plagiat.Models
{
    public class Source
    {
        public int Id { get; set; }
        
        [Required]
        public string Title { get; set; }
        
        public string Author { get; set; }
        
        public string Publisher { get; set; }
        
        public int? Year { get; set; }
        
        public string Url { get; set; }
        
        public string ISBN { get; set; }
        
        public string DOI { get; set; }
        
        public SourceType Type { get; set; }
        
        public string Volume { get; set; }
        
        public string Issue { get; set; }
        
        public string Pages { get; set; }
        
        public string City { get; set; }
        
        public DateTime? AccessDate { get; set; }
        
        public bool IsComplete { get; set; }
        
        public string ConferenceName { get; set; }
        
        public string Institution { get; set; }
        
        public string Journal { get; set; }
        
        public virtual ICollection<Citation> Citations { get; set; }
        
        public Source()
        {
            Citations = new List<Citation>();
            Type = SourceType.Unknown;
        }
        
        public Source Clone()
        {
            return new Source
            {
                Id = this.Id,
                Title = this.Title,
                Author = this.Author,
                Publisher = this.Publisher,
                Year = this.Year,
                Url = this.Url,
                ISBN = this.ISBN,
                DOI = this.DOI,
                Type = this.Type,
                Volume = this.Volume,
                Issue = this.Issue,
                Pages = this.Pages,
                City = this.City,
                AccessDate = this.AccessDate,
                IsComplete = this.IsComplete,
                ConferenceName = this.ConferenceName,
                Institution = this.Institution,
                Journal = this.Journal,
                Citations = new List<Citation>(this.Citations)
            };
        }
        
        public string GetFormattedReference(CitationStyle style)
        {
            switch (style)
            {
                case CitationStyle.GOST:
                    return FormatGOST();
                case CitationStyle.APA:
                    return FormatAPA();
                case CitationStyle.MLA:
                    return FormatMLA();
                case CitationStyle.Chicago:
                    return FormatChicago();
                case CitationStyle.Harvard:
                    return FormatHarvard();
                case CitationStyle.Vancouver:
                    return FormatVancouver();
                default:
                    return FormatGOST();
            }
        }
        
        private string FormatGOST()
        {
            var result = "";
            if (!string.IsNullOrEmpty(Author)) result += Author + ". ";
            result += Title;
            if (!string.IsNullOrEmpty(Publisher)) result += " / " + Publisher;
            if (Year.HasValue) result += ", " + Year.Value;
            if (!string.IsNullOrEmpty(Pages)) result += ". – " + Pages + " с.";
            return result;
        }
        
        private string FormatAPA()
        {
            var result = "";
            if (!string.IsNullOrEmpty(Author)) result += Author + " ";
            if (Year.HasValue) result += "(" + Year.Value + "). ";
            result += Title + ". ";
            if (!string.IsNullOrEmpty(Publisher)) result += Publisher + ".";
            return result;
        }
        
        private string FormatMLA()
        {
            var result = "";
            if (!string.IsNullOrEmpty(Author)) result += Author + ". ";
            result += "\"" + Title + ".\" ";
            if (!string.IsNullOrEmpty(Publisher)) result += Publisher + ", ";
            if (Year.HasValue) result += Year.Value + ".";
            return result;
        }
        
        private string FormatChicago()
        {
            return FormatAPA(); // Упрощенная версия
        }
        
        private string FormatHarvard()
        {
            return FormatAPA(); // Упрощенная версия
        }
        
        private string FormatVancouver()
        {
            var result = "";
            if (!string.IsNullOrEmpty(Author)) result += Author + ". ";
            result += Title + ". ";
            if (!string.IsNullOrEmpty(Publisher)) result += Publisher + "; ";
            if (Year.HasValue) result += Year.Value + ".";
            return result;
        }
    }

    public enum SourceType
    {
        Unknown,
        Book,
        Article,
        Website,
        Journal,
        Thesis,
        Conference,
        Report
    }
}

