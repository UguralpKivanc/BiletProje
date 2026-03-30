using AuthService.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://*:5002");

var mongoConn = Environment.GetEnvironmentVariable("ConnectionStrings__MongoDb")
                ?? "mongodb://mongodb:27017";

builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoConn));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpMetrics();
app.MapMetrics();
app.MapControllers();

// Seed Data: Varsayılan admin kullanıcısı + API key
try
{
    var client = app.Services.GetRequiredService<IMongoClient>();
    var db = client.GetDatabase("AuthServiceDb");
    var usersCollection = db.GetCollection<BsonDocument>("Users");
    var apiKeysCollection = db.GetCollection<BsonDocument>("ApiKeys");

    if (usersCollection.CountDocuments(new BsonDocument()) == 0)
    {
        var adminHash = BCrypt.Net.BCrypt.HashPassword("Bilet2026");
        var admin = new BsonDocument
        {
            { "Username", "admin" },
            { "PasswordHash", adminHash },
            { "Role", "admin" }
        };
        usersCollection.InsertOne(admin);
        Console.WriteLine("--> AĞAM: Varsayılan admin kullanıcısı oluşturuldu! (admin / Bilet2026)");
    }

    var keyFilter = Builders<BsonDocument>.Filter.Eq("key", "KingoSifre123");
    var existingKey = apiKeysCollection.Find(keyFilter).FirstOrDefault();
    if (existingKey == null)
    {
        apiKeysCollection.InsertOne(new BsonDocument
        {
            { "key", "KingoSifre123" },
            { "isActive", true }
        });
        Console.WriteLine("--> AĞAM: Varsayılan API key oluşturuldu! (KingoSifre123)");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"--> Auth DB Hatası: {ex.Message}");
}

app.Run();
