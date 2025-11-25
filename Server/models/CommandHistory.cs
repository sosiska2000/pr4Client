using System;
using System.ComponentModel.DataAnnotations;

namespace Server.Models
{
    public class CommandHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(20)]
        public string CommandType { get; set; } // "connect", "cd", "get", "set"

        public string CommandText { get; set; }

        public string Parameters { get; set; }

        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

        public bool Success { get; set; }

        // Навигационное свойство
        public virtual User User { get; set; }
    }
}