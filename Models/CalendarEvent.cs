using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InternConnect_Backend.Models
{
    public class CalendarEvent
    {
        [Key]
        public int EventId { get; set; }   // Primary Key

        // Foreign Key to User
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        [JsonIgnore]
        public User? User { get; set; }   // Navigation property

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;   // e.g., "Interview - Tech Corp"

        [MaxLength(250)]
        public string Description { get; set; } = string.Empty;   // Optional details

        [Required]
        public DateTime StartTime { get; set; }   // Event start datetime

        [Required]
        public DateTime EndTime { get; set; }     // Event end datetime
    }
}
