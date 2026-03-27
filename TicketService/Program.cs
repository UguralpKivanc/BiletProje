using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

var mongoConn = Environment.GetEnvironmentVariable("ConnectionStrings__MongoDb")
                ?? "mongodb://mongodb:27017";

builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoConn));
builder.Services.AddControllers();

var app = builder.Build();

app.UseDeveloperExceptionPage();
app.MapControllers();

try
{
    var client = app.Services.GetRequiredService<IMongoClient>();
    var db = client.GetDatabase("BiletSistemiDb");
    var collection = db.GetCollection<dynamic>("Tickets");

    if (collection.CountDocuments(_ => true) == 0)
    {
        collection.InsertOne(new { eventName = "Tarkan Konseri", seat = "A-12", status = "Sold" });
        Console.WriteLine("--> Ticket verisi eklendi.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"--> MongoDB henüz hazır değil: {ex.Message}"); 
}

app.Run();