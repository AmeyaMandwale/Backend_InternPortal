using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InternConnect_Backend.Models
{
    public class SkillGapAnalysis
    {
        [Key]
        public int SkillGapAnalysisId { get; set; }   // Primary Key

        // Foreign Key to User
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        [JsonIgnore]
        public User? User { get; set; }   // Navigation property

        
        public string Title { get; set; } = string.Empty; // Title of the analysis

       
        public string Description { get; set; } = string.Empty; // Detailed description

        public string ResourceLink { get; set; } = string.Empty;  // URL to docs, tutorials, etc.

        public string SkillGapAnalysisGoal { get; set; } = string.Empty;

    }
}