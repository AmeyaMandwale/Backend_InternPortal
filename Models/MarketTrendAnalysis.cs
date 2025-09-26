using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InternConnect_Backend.Models
{
    public class MarketTrendAnalysis
    {
        [Key]
        public int TrendId { get; set; }  // Primary Key

        // Foreign Key to User
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        [JsonIgnore]
        public User? User { get; set; }   // Navigation property

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;  // e.g., "Cybersecurity Priority"

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;  // Detailed description

        [MaxLength(100)]
        public string Industry { get; set; } = string.Empty;  // e.g., "Technology"

        [MaxLength(200)]
        public string Growth { get; set; } = string.Empty;  // e.g., "+38%"

        [MaxLength(100)]
        public string SalaryTrend { get; set; } = string.Empty;  // e.g., "High Growth"

        [NotMapped]  // Stored as JSON in UI, can handle serialization if stored in DB
        public List<string> Locations { get; set; } = new List<string>();  // e.g., ["Washington DC", "Remote"]

        [NotMapped]  // Tags list, similar to Locations
        public List<string> Tags { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
