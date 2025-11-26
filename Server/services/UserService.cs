using System;
using System.Linq;
using Server.Data;
using Server.Models;

namespace Server.Services
{
    public class UserService
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

            if (user.PasswordHash == password)
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

        public void InitializeDatabase(string ftpUserPath)
        {
       
            _context.Database.EnsureCreated();


            if (!_context.Users.Any())
            {
                var user = new User("skomor1n", "Asdfgl23", ftpUserPath);
                _context.Users.Add(user);
                _context.SaveChanges();
            }
        }
    }
}