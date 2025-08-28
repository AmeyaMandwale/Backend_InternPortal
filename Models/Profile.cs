using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InternConnect_Backend.Models
{
    public class Profile
    {
        [Key]
        public int ProfileId { get; set; }   // Primary Key for Profile

        // Foreign Key
       
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        [JsonIgnore]
        public User? User { get; set; }  // Navigation Property

        [Required]
        public string Name { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        public string Phone { get; set; } = string.Empty;

        public DateTime? DOB { get; set; }

        public string Gender { get; set; } = string.Empty;

        public string Photo { get; set; } = string.Empty; // can store URL/path

        public string Location { get; set; } = string.Empty;

        public bool WillingToRelocate { get; set; }

        public string PreferredLocation { get; set; } = string.Empty;

        public string CareerGoal { get; set; } = string.Empty;

        public string Bio { get; set; } = string.Empty;

        public string LinkedIn { get; set; } = string.Empty;

        public string Github { get; set; } = string.Empty;
        
        // Collections
        public ICollection<Education> Educations { get; set; } = new List<Education>();
        public ICollection<Skill> Skills { get; set; } = new List<Skill>();
        public ICollection<Project> Projects { get; set; } = new List<Project>();
        public ICollection<Achievement> Achievements { get; set; } = new List<Achievement>();
    }
}