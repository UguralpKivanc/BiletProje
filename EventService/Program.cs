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
// Seed Data
try
{
    var client = app.Services.GetRequiredService<IMongoClient>();
    var db = client.GetDatabase("EventServiceDb");
    var collection = db.GetCollection<BsonDocument>("Events");

    var seedEvents = new[]
    {
        new { Name = "Tarkan Konseri", Location = "Istanbul", Date = new DateTime(2026, 5, 30), Price = 500 },
        new { Name = "Sertab Erener Konseri", Location = "Istanbul", Date = new DateTime(2026, 6, 15), Price = 450 },
        new { Name = "Sezen Aksu Konseri", Location = "Istanbul", Date = new DateTime(2026, 7, 20), Price = 550 }
    };

    foreach (var e in seedEvents)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("Name", e.Name);
        if (collection.CountDocuments(filter) > 0)
            continue;

        collection.InsertOne(new BsonDocument
        {
            { "Name", e.Name },
            { "Location", e.Location },
            { "Date", e.Date },
            { "Price", e.Price }
        });
        Console.WriteLine($"--> {e.Name} veritabanına eklendi.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"--> MongoDB Hatası: {ex.Message}");
}

app.Run();
