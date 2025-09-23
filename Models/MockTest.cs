using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InternConnect_Backend.Models
{
    public class MockTest
    {
        [Key]
        public int QuestionId { get; set; }   // Primary Key

        // Foreign Key to User
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        [JsonIgnore]
        public User? User { get; set; }   // Navigation property

        [Required]
        public int QuestionNumber { get; set; }   // Order of the question in test

        [Required]
        public string Question { get; set; } = string.Empty;

        // Four options as separate fields
        [Required]
        public string Option1 { get; set; } = string.Empty;

        [Required]
        public string Option2 { get; set; } = string.Empty;

        [Required]
        public string Option3 { get; set; } = string.Empty;

        [Required]
        public string Option4 { get; set; } = string.Empty;

        [Required]
        public string CorrectAnswer { get; set; } = string.Empty; // Must match one of the options

        public string Explanation { get; set; } = string.Empty;  // Why it's correct
    }
}