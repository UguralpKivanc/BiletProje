using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Bağlantı dizesini hem Docker hem Local uyumlu yapıyoruz
var mongoConn = Environment.GetEnvironmentVariable("ConnectionStrings__MongoDb")
                ?? "mongodb://mongodb:27017";

builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoConn));
builder.Services.AddControllers();

var app = builder.Build();

// Hata detaylarını görmek için önemli
app.UseDeveloperExceptionPage();
app.MapControllers();

// Veritabanı başlangıç verisi (Hata almaması için Try-Catch içine aldık)
try
{
    var client = app.Services.GetRequiredService<IMongoClient>();
    var db = client.GetDatabase("BiletSistemiDb");
    var collection = db.GetCollection<dynamic>("Events");

    // Uygulama açılırken MongoDB hazır değilse beklemesi için küçük bir kontrol
    if (collection.CountDocuments(_ => true) == 0)
    {
        collection.InsertOne(new { name = "Tarkan Konseri", location = "Istanbul", date = DateTime.Now.AddDays(10), price = 500 });
        Console.WriteLine("--> Event verisi eklendi.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"--> MongoDB henüz hazır değil: {ex.Message}");
}

app.Run();