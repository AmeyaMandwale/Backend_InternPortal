using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InternConnect_Backend.Models
{
    public class Skill
    {
        [Key]
        public int SkillId { get; set; }   // Primary Key

        // Foreign Key to Profile
       
        public int ProfileId { get; set; }

        [ForeignKey("ProfileId")]
        [JsonIgnore]
        public Profile? Profile { get; set; }   // Navigation property

        
        public string Technical { get; set; } = string.Empty;  // e.g., C#, Java, React

        public string Tools { get; set; } = string.Empty;      // e.g., Git, Docker, VS Code
    }
}