using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InternConnect_Backend.Models
{
    public class Achievement
    {
        [Key]
        public int AchievementId { get; set; }   // Primary Key

        // Foreign Key to Profile
       
        public int ProfileId { get; set; }

        [ForeignKey("ProfileId")]
        [JsonIgnore]
        public Profile? Profile { get; set; }   // Navigation property

        [Required]
        public string Title { get; set; } = string.Empty;   // e.g., "Best Intern Award"

        public string Issuer { get; set; } = string.Empty;  // e.g., "Microsoft, College, etc."

        
        public int Year { get; set; }    // e.g., 2023

        [Required]
        public string Description { get; set; } = string.Empty;
    }
}