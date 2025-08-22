using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InternConnect_Backend.Models
{
    public class Project
    {
        [Key]
        public int ProjectId { get; set; }   // Primary Key

        // Foreign Key to Profile
      

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public string Technologies { get; set; } = string.Empty;  // e.g., React, .NET, MySQL

        public string Link { get; set; } = string.Empty;  // e.g., GitHub/Live Demo URL


        public int ProfileId { get; set; }

        [ForeignKey("ProfileId")]
        [JsonIgnore]
        public Profile? Profile { get; set; }   // Navigation property
    }
}