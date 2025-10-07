using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternConnect_Backend.Models
{
    public class Mentor
    {
        [Key]
        public int MentorId { get; set; }  // Primary Key

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Organisation { get; set; } = string.Empty;

        [Required]
        public int Experience { get; set; }  // in years

        [Required]
        public decimal Cost { get; set; }    // Mentorship cost or fee

        public string Photo { get; set; } = string.Empty; // URL or file path

        public string Linkedin { get; set; } = string.Empty; // URL or file path


        public string Mobile { get; set; } = string.Empty;

        public string Bio { get; set; } = string.Empty;  // Short description or background

        public string Link { get; set; } = string.Empty; // External profile or booking link

        public bool Approved { get; set; } = false;  // Default: not approved
    }
}