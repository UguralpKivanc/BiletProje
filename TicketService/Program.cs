using MongoDB.Bson;
using MongoDB.Driver;
using Prometheus;
using TicketService.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://*:5168");

var mongoConn = Environment.GetEnvironmentVariable("ConnectionStrings__MongoDb")
                ?? "mongodb://mongodb:27017";

builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoConn));
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
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
