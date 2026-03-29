using eventservice.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://*:5001");

var mongoConn = Environment.GetEnvironmentVariable("ConnectionStrings__MongoDb")
                ?? "mongodb://mongodb:27017";

builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoConn));
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddControllers();

var app = builder.Build();

app.UseDeveloperExceptionPage();
app.UseHttpMetrics();   // Prometheus HTTP metrikleri
app.MapMetrics();       // /metrics endpoint'i

app.MapControllers();

// Seed Data
try
{
    var client = app.Services.GetRequiredService<IMongoClient>();
    var db = client.GetDatabase("BiletSistemiDb");
    var collection = db.GetCollection<BsonDocument>("Events");

    if (collection.CountDocuments(new BsonDocument()) == 0)
    {
        var tarkan = new BsonDocument
        {
            { "Name", "Tarkan Konseri" },
            { "Location", "Istanbul" },
            { "Date", new DateTime(2026, 5, 30) },
            { "Price", 500 }
        };
        collection.InsertOne(tarkan);
        Console.WriteLine("--> AĞAM: Tarkan Konseri Veritabanına İşlendi!");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"--> MongoDB Hatası: {ex.Message}");
}

app.Run();
