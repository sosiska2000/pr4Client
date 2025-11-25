using System;
using System.Linq;
using Server.Data;
using Server.Models;

namespace Server.Services
{
    public interface IUserService
    {
        User Authenticate(string login, string password);
        void LogCommand(int userId, string commandType, string commandText, string parameters, bool success);
    }

    public class UserService : IUserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public User Authenticate(string login, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Login == login);

            if (user == null)
                return null;

            // Простая проверка пароля (в реальном приложении используйте хеширование!)
            if (user.PasswordHash == password) // Замените на хеширование!
            {
                user.LastLogin = DateTime.UtcNow;
                _context.SaveChanges();
                return user;
            }

            return null;
        }

        public void LogCommand(int userId, string commandType, string commandText, string parameters, bool success)
        {
            var commandLog = new CommandHistory
            {
                UserId = userId,
                CommandType = commandType,
                CommandText = commandText,
                Parameters = parameters,
                ExecutedAt = DateTime.UtcNow,
                Success = success
            };

            _context.CommandHistory.Add(commandLog);
            _context.SaveChanges();
        }
    }
}