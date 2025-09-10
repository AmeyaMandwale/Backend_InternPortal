using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InternConnect_Backend.Models
{
    public class Education
    {
        [Key]
        public int EducationId { get; set; }   // Primary Key

        // Foreign Key to Profile
       
        public int ProfileId { get; set; }

        [ForeignKey("ProfileId")]
        [JsonIgnore]
        public Profile? Profile { get; set; }   // Navigation property

        [Required]
        public string Degree { get; set; } = string.Empty;

       
        public string Branch { get; set; } = string.Empty;

        [Required]
        public string Institute { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }   // Nullable in case ongoing

        public string Grade { get; set; } = string.Empty;
    }
}