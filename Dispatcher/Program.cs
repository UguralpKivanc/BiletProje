using System.Text;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
var app = builder.Build();

bool isDocker = Environment.GetEnvironmentVariable("DOCKER_ENV") == "true";

// 1. ADIM: ANA SAYFA - Karakter Kodlaması (Encoding) Düzeltildi
app.MapGet("/", () => {
    var html = @"
    <!DOCTYPE html>
    <html lang='tr'>
    <head>
        <meta charset='UTF-8'>
        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
        <title>Bilet Sistemi Kontrol Paneli</title>
    </head>
    <body style='background:#0d1117; color:#58a6ff; font-family:sans-serif; text-align:center; padding-top:80px;'>
        <div style='border:2px solid #238636; display:inline-block; padding:40px; border-radius:15px; background:#161b22; box-shadow: 0 10px 30px rgba(0,0,0,0.5);'>
            <h1 style='color:#238636; margin-bottom:10px;'>🦁 AĞAM HOŞ GELDİN!</h1>
            <p style='color:#8b949e; font-size:1.2em;'>Bilet Sistemi Mikroservis Kontrol Paneli</p>
            <hr style='border:0; border-top:1px solid #30363d; margin:25px 0;'>
            <div style='display:flex; gap:20px; justify-content:center;'>
                <a href='/api/events' style='background:#238636; color:white; padding:15px 30px; text-decoration:none; border-radius:6px; font-weight:bold;'>📅 Etkinlikleri Listele</a>
                <a href='/api/tickets' style='background:#1f6feb; color:white; padding:15px 30px; text-decoration:none; border-radius:6px; font-weight:bold;'>🎫 Biletleri Listele</a>
            </div>
            <p style='margin-top:40px; font-size:0.85em; color:#484f58;'>Dispatcher (API Gateway) v1.1 - Durum: Aktif ve Kararlı</p>
        </div>
    </body>
    </html>";

    // ContentType yanına charset ekleyerek tarayıcıyı zorluyoruz
    return Results.Content(html, "text/html; charset=utf-8", Encoding.UTF8);
});

// 2. ADIM: GENEL YÖNLENDİRME MANTIĞI
app.Map("{*path}", async (HttpContext context, string path, IHttpClientFactory clientFactory) =>
{
    var client = clientFactory.CreateClient();
    string targetUrl = "";

    if (path.Contains("events"))
        targetUrl = isDocker ? $"http://event-service:5001/{path}" : $"http://localhost:5001/{path}";
    else if (path.Contains("tickets"))
        targetUrl = isDocker ? $"http://ticket-service:5168/{path}" : $"http://localhost:5168/{path}";
    else
        return Results.Content("Ağam bu yol çıkmaz sokak, rota bulunamadı!", "text/plain", Encoding.UTF8);

    try
    {
        var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), targetUrl);

        if (context.Request.ContentLength > 0)
        {
            request.Content = new StreamContent(context.Request.Body);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(context.Request.ContentType ?? "application/json");
        }

        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        return Results.Content(content, "application/json", Encoding.UTF8);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Dispatcher ağda takıldı ağam: {ex.Message}");
    }
});

app.Run();