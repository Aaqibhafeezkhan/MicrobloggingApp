namespace MicrobloggingApp.Core
{
    public class UserService : IUserService
    {
        private readonly Dictionary<string, (int Id, string Password)> _users = new()
        {
            { "user1", (1, "password1") },
            { "user2", (2, "password2") }
        };

        public bool ValidateUser(string username, string password)
        {
            return _users.TryGetValue(username, out var user) && user.Password == password;
        }

        public int? GetUserId(string username)
        {
            return _users.TryGetValue(username, out var user) ? user.Id : null;
        }
    }
}
