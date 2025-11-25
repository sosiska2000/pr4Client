using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Server.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Login { get; set; }

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; }

        [Required]
        public string BaseDirectory { get; set; }

        // Добавьте это поле обратно для совместимости
        public string temp_src { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLogin { get; set; }

        // Навигационное свойство для истории команд
        public virtual ICollection<CommandHistory> CommandHistory { get; set; }

        // Конструктор для удобства
        public User() { }

        public User(string login, string passwordHash, string baseDirectory)
        {
            Login = login;
            PasswordHash = passwordHash;
            BaseDirectory = baseDirectory;
            temp_src = baseDirectory; // Инициализируем temp_src
        }
    }
}