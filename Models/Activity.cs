using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InternConnect_Backend.Models
{
    public class Activity
    {
        [Key]
        public int ActivityId { get; set; }   // Primary Key

        // Foreign Key to User
       
        public int UserId { get; set; }

        [ForeignKey("UserId")]

        [JsonIgnore]
        public User? User { get; set; }   // Navigation property

        [Required]
        public int StepNumber { get; set; }   // Sequence order of the activity

        [Required]
        public string Heading { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string ResourceLink { get; set; } = string.Empty;  // URL to docs, tutorials, etc.

        public string EstimatedTime { get; set; } = string.Empty; // e.g., "2 hours", "3 days"

        public string Status { get; set; } = "Pending"; // Default: Pending, can be Completed/In-progress

        public string ActivityGoal { get; set; } = string.Empty;
    }
}