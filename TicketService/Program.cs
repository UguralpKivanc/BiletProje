using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
using Prometheus;
using TicketService.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://*:5168");

var mongoConn = Environment.GetEnvironmentVariable("ConnectionStrings__MongoDb")
                ?? "mongodb://mongodb:27017";

var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
                ?? "BiletSistemi-JWT-Gizli-Anahtar-2026-SuperSecret!";

builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoConn));
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddControllers();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = "BiletSistemi",
            ValidateAudience = true,
            ValidAudience = "BiletSistemi",
            ClockSkew = TimeSpan.Zero,
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseDeveloperExceptionPage();
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpMetrics();   // Prometheus HTTP metrikleri
app.MapMetrics();       // /metrics endpoint'i

app.MapControllers();

// Seed Data
try
{
    var client = app.Services.GetRequiredService<IMongoClient>();
    var db = client.GetDatabase("TicketServiceDb");
    var collection = db.GetCollection<BsonDocument>("Tickets");

    if (collection.CountDocuments(new BsonDocument()) == 0)
    {
        var sampleTicket = new BsonDocument
        {
            { "EventName", "Tarkan Konseri" },
            { "CustomerName", "Ahmet Yılmaz" },
            { "Seat", "A-12" },
            { "Status", "Active" },
            { "Price", 500 },
            { "PurchaseDate", DateTime.UtcNow }
        };
        collection.InsertOne(sampleTicket);
        Console.WriteLine("--> AĞAM: Örnek Bilet Verisi Veritabanına İşlendi!");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"--> Ticket DB Hatası: {ex.Message}");
}

app.Run();
