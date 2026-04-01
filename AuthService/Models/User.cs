using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AuthService.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "user";
    }

    public record LoginRequest(string Username, string Password);

    public record RegisterRequest(string Username, string Password);
}
