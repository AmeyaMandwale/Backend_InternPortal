using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InternConnect_Backend.Models
{
    public class ApplicationRating
    {
        public int Id { get; set; }
        public string UserId { get; set; } // Foreign key from Users table
        public int Rating { get; set; } //1-5
        public DateTime RatedOn { get; set; }
    }
}