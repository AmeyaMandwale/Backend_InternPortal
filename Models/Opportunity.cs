using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InternConnect_Backend.Models
{
    public class Opportunity
    {
        [Key]
        public int OpportunityId { get; set; }   // Primary Key

        // Foreign Key to User
        
        public int UserId { get; set; }

        [ForeignKey("UserId")]

        [JsonIgnore]
        public User? User { get; set; }   // Navigation property

        [Required]
        public string Position { get; set; } = string.Empty;   // e.g., Software Intern

        [Required]
        public string Company { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public string Stipend { get; set; } = string.Empty;   // string to handle ranges like "10k-15k"

        public DateTime PostedDate { get; set; } = DateTime.UtcNow;

        public string Description { get; set; } = string.Empty;

        public bool IsSaved { get; set; } = false;

        public bool IsApplied { get; set; } = false;

        public string Status { get; set; } = "None";   // e.g., "Open", "Closed", "Selected"

        public string ApplyLink { get; set; } = string.Empty;  // URL for application

        public string Type { get; set; } = string.Empty;  // e.g., Internship / Full-time

        public string CompanyWebsite { get; set; } = string.Empty;
    }
}