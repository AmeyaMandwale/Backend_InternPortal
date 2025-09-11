using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InternConnect_Backend.Models
{
    public class CareerTip
    {
        [Key]
        public int CareerTipId { get; set; }   // Primary Key

        // Foreign Key to User
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        [JsonIgnore]
        public User? User { get; set; }   // Navigation property

        
        public string Description { get; set; } = string.Empty; // The career tip content
    }
}