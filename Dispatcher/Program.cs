using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
using Prometheus;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
builder.Logging.AddConsole();

bool isDocker = Environment.GetEnvironmentVariable("DOCKER_ENV") == "true";
var mongoConn = isDocker ? "mongodb://mongodb:27017" : "mongodb://localhost:27017";
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
                ?? "BiletSistemi-JWT-Gizli-Anahtar-2026-SuperSecret!";

builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoConn));

var app = builder.Build();

// ── GÖREV 3: TRAFİK LOGLAMA MIDDLEWARE ──────────────────────────────────────
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var sw = Stopwatch.StartNew();
    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "?";

    logger.LogInformation("[TRAFİK] → {Method} {Path}{Query} | IP: {IP}",
        context.Request.Method,
        context.Request.Path,
        context.Request.QueryString,
        ip);

    await next();

    sw.Stop();
    logger.LogInformation("[TRAFİK] ← {Method} {Path} | Status: {Status} | {Ms}ms",
        context.Request.Method,
        context.Request.Path,
        context.Response.StatusCode,
        sw.ElapsedMilliseconds);
});

// ── GÖREV 4: PROMETHEUS METRİKLERİ ──────────────────────────────────────────
app.UseHttpMetrics();
app.MapMetrics(); // /metrics endpoint'i (catch-all'dan önce geldiği için öncelikli)

// ── ANA SAYFA ────────────────────────────────────────────────────────────────
app.MapGet("/", () =>
{
    var html = """
    <!DOCTYPE html>
    <html lang="tr">
    <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <title>Bilet Sistemi</title>
        <style>
            *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }
            body {
                background: #0d1117;
                color: #c9d1d9;
                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
                min-height: 100vh;
                padding: 40px 16px;
            }
            .container { max-width: 860px; margin: 0 auto; }
            h1 { color: #58a6ff; font-size: 1.8rem; margin-bottom: 4px; }
            .subtitle { color: #8b949e; margin-bottom: 32px; font-size: 0.95rem; }

            /* NAV BUTONLARI */
            .nav { display: flex; gap: 12px; flex-wrap: wrap; margin-bottom: 36px; }
            .btn {
                padding: 10px 22px; border-radius: 6px; border: none;
                font-size: 0.9rem; font-weight: 600; cursor: pointer;
                text-decoration: none; display: inline-flex; align-items: center;
                gap: 6px; transition: opacity .15s;
            }
            .btn:hover { opacity: .85; }
            .btn:disabled { opacity: .5; cursor: not-allowed; }
            .btn-green  { background: #238636; color: #fff; }
            .btn-blue   { background: #1f6feb; color: #fff; }
            .btn-purple { background: #6e40c9; color: #fff; }

            /* FORM KARTI */
            .card {
                background: #161b22;
                border: 1px solid #30363d;
                border-radius: 12px;
                padding: 28px 32px;
                margin-bottom: 24px;
            }
            .card h2 { color: #58a6ff; font-size: 1.15rem; margin-bottom: 20px; }
            .form-grid {
                display: grid;
                grid-template-columns: 1fr 1fr;
                gap: 16px;
            }
            .form-group { display: flex; flex-direction: column; gap: 6px; }
            .form-group.full { grid-column: 1 / -1; }
            label { font-size: 0.82rem; color: #8b949e; font-weight: 500; }
            input {
                background: #0d1117;
                border: 1px solid #30363d;
                border-radius: 6px;
                color: #c9d1d9;
                padding: 9px 12px;
                font-size: 0.95rem;
                outline: none;
                transition: border-color .15s;
            }
            input:focus { border-color: #58a6ff; }
            .apikey-row {
                display: flex; align-items: center; gap: 10px;
                background: #0d1117;
                border: 1px solid #238636;
                border-radius: 6px;
                padding: 8px 12px;
                font-size: 0.82rem; color: #3fb950;
            }
            .apikey-row span { opacity: .7; }
            .submit-btn {
                grid-column: 1 / -1;
                padding: 12px;
                background: #238636;
                color: #fff;
                border: none; border-radius: 6px;
                font-size: 1rem; font-weight: 600;
                cursor: pointer; transition: background .15s;
            }
            .submit-btn:hover { background: #2ea043; }
            .submit-btn:disabled { background: #21262d; color: #484f58; cursor: not-allowed; }

            /* SONUÇ ALANI */
            #result {
                display: none;
                border-radius: 10px;
                padding: 20px 24px;
                border: 1px solid #30363d;
                font-size: 0.88rem;
            }
            #result.success { border-color: #238636; background: #0f2a17; }
            #result.error   { border-color: #da3633; background: #2d1117; }
            #result h3 { margin-bottom: 10px; font-size: 1rem; }
            #result.success h3 { color: #3fb950; }
            #result.error   h3 { color: #f85149; }
            pre {
                background: #0d1117;
                border-radius: 6px;
                padding: 12px;
                overflow-x: auto;
                color: #c9d1d9;
                font-size: 0.82rem;
                line-height: 1.5;
                white-space: pre-wrap;
            }

            /* PAGİNASYON */
            .pagination {
                display: none;
                align-items: center;
                justify-content: space-between;
                margin-top: 14px;
                gap: 10px;
            }
            .pagination.visible { display: flex; }
            .page-info { font-size: 0.82rem; color: #8b949e; }
            .page-btns { display: flex; gap: 8px; }
            .page-btn {
                padding: 6px 14px; border-radius: 6px; border: 1px solid #30363d;
                background: #161b22; color: #c9d1d9; font-size: 0.82rem;
                cursor: pointer; transition: border-color .15s;
            }
            .page-btn:hover:not(:disabled) { border-color: #58a6ff; color: #58a6ff; }
            .page-btn:disabled { opacity: .35; cursor: not-allowed; }

            .spinner {
                display: inline-block;
                width: 16px; height: 16px;
                border: 2px solid #30363d;
                border-top-color: #58a6ff;
                border-radius: 50%;
                animation: spin .7s linear infinite;
                vertical-align: middle; margin-right: 6px;
            }
            @keyframes spin { to { transform: rotate(360deg); } }
            @media (max-width: 560px) { .form-grid { grid-template-columns: 1fr; } .form-group.full { grid-column: 1; } }
        </style>
    </head>
    <body>
        <div class="container">
            <h1>Bilet Sistemi</h1>
            <p class="subtitle">Mikroservis API Gateway &mdash; v3.0</p>

            <!-- NAV -->
            <div class="nav">
                <button class="btn btn-green"  id="btnEvents">Etkinlikleri Listele</button>
                <button class="btn btn-blue"   id="btnTickets">Biletleri Listele</button>
                <a class="btn btn-purple" href="/metrics" target="_blank">Prometheus Metrikleri</a>
            </div>

            <!-- BİLET SATIN ALMA FORMU -->
            <div class="card">
                <h2>Bilet Satın Al</h2>
                <form id="ticketForm">
                    <div class="form-grid">

                        <div class="form-group full">
                            <label>Etkinlik Adı</label>
                            <input type="text" id="eventName" placeholder="Örn: Tarkan Konseri" required>
                        </div>

                        <div class="form-group">
                            <label>Müşteri Adı</label>
                            <input type="text" id="customerName" placeholder="Ad Soyad" required>
                        </div>

                        <div class="form-group">
                            <label>Koltuk</label>
                            <input type="text" id="seat" placeholder="Örn: A-12" required>
                        </div>

                        <div class="form-group">
                            <label>Fiyat (TL)</label>
                            <input type="number" id="price" placeholder="500" min="0" required>
                        </div>

                        <div class="form-group">
                            <label>API Kimlik Doğrulama</label>
                            <div class="apikey-row">
                                <span>X-Api-Key:</span>
                                <strong>KingoSifre123</strong>
                                <span style="margin-left:auto">&#10003; Otomatik Eklendi</span>
                            </div>
                        </div>

                        <button type="submit" class="submit-btn" id="submitBtn">
                            Bileti Satın Al
                        </button>
                    </div>
                </form>
            </div>

            <!-- SONUÇ -->
            <div id="result">
                <h3 id="resultTitle"></h3>
                <pre id="resultBody"></pre>
                <div class="pagination" id="pagination">
                    <span class="page-info" id="pageInfo"></span>
                    <div class="page-btns">
                        <button class="page-btn" id="btnPrev">&#8592; Önceki</button>
                        <button class="page-btn" id="btnNext">Sonraki &#8594;</button>
                    </div>
                </div>
            </div>
        </div>

        <script>
            const API_KEY = 'KingoSifre123';

            // ── LİSTELEME BUTONLARI ──────────────────────────────────────────────
            const PAGE_SIZE = 10;
            let ticketPage  = 1;
            let ticketTotal = 0;

            async function fetchAndShow(url, btn, label) {
                btn.disabled = true;
                btn.innerHTML = '<span class="spinner"></span>' + label;
                const result     = document.getElementById('result');
                const pagination = document.getElementById('pagination');
                result.style.display = 'none';
                pagination.classList.remove('visible');

                try {
                    const res  = await fetch(url, { headers: { 'X-Api-Key': API_KEY } });
                    const text = await res.text();
                    let pretty;
                    try   { pretty = JSON.stringify(JSON.parse(text), null, 2); }
                    catch { pretty = text; }

                    result.className = res.ok ? 'success' : 'error';
                    document.getElementById('resultTitle').textContent =
                        res.ok ? `${label} (HTTP ${res.status})` : `Hata (HTTP ${res.status})`;
                    document.getElementById('resultBody').textContent = pretty;
                } catch (err) {
                    result.className = 'error';
                    document.getElementById('resultTitle').textContent = 'Bağlantı Hatası';
                    document.getElementById('resultBody').textContent  = err.message;
                } finally {
                    result.style.display = 'block';
                    btn.disabled = false;
                    btn.textContent = label;
                    result.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
                }
            }

            async function fetchTickets(page) {
                const btn        = document.getElementById('btnTickets');
                const result     = document.getElementById('result');
                const pagination = document.getElementById('pagination');

                btn.disabled = true;
                btn.innerHTML = '<span class="spinner"></span>Biletleri Listele';
                result.style.display = 'none';
                pagination.classList.remove('visible');

                try {
                    const res  = await fetch(`/api/tickets?page=${page}&pageSize=${PAGE_SIZE}`,
                                            { headers: { 'X-Api-Key': API_KEY } });
                    const text = await res.text();
                    let data, pretty;
                    try {
                        data   = JSON.parse(text);
                        pretty = JSON.stringify(data, null, 2);
                    } catch {
                        pretty = text;
                        data   = null;
                    }

                    result.className = res.ok ? 'success' : 'error';

                    if (res.ok && data?.pagination) {
                        const p = data.pagination;
                        ticketPage  = p.page;
                        ticketTotal = p.totalPages;

                        document.getElementById('resultTitle').textContent =
                            `Biletleri Listele — Sayfa ${p.page} / ${p.totalPages}  (Toplam: ${p.totalCount})`;
                        document.getElementById('pageInfo').textContent =
                            `${(p.page - 1) * p.pageSize + 1}–${Math.min(p.page * p.pageSize, p.totalCount)} / ${p.totalCount} kayıt`;
                        document.getElementById('btnPrev').disabled = p.page <= 1;
                        document.getElementById('btnNext').disabled = p.page >= p.totalPages;
                        pagination.classList.add('visible');
                    } else {
                        document.getElementById('resultTitle').textContent =
                            res.ok ? `Biletleri Listele (HTTP ${res.status})` : `Hata (HTTP ${res.status})`;
                    }

                    document.getElementById('resultBody').textContent = pretty;
                } catch (err) {
                    result.className = 'error';
                    document.getElementById('resultTitle').textContent = 'Bağlantı Hatası';
                    document.getElementById('resultBody').textContent  = err.message;
                } finally {
                    result.style.display = 'block';
                    btn.disabled = false;
                    btn.textContent = 'Biletleri Listele';
                    result.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
                }
            }

            document.getElementById('btnEvents').addEventListener('click', function () {
                fetchAndShow('/api/events', this, 'Etkinlikleri Listele');
            });

            document.getElementById('btnTickets').addEventListener('click', () => {
                ticketPage = 1;
                fetchTickets(1);
            });

            document.getElementById('btnPrev').addEventListener('click', () => {
                if (ticketPage > 1) fetchTickets(ticketPage - 1);
            });

            document.getElementById('btnNext').addEventListener('click', () => {
                if (ticketPage < ticketTotal) fetchTickets(ticketPage + 1);
            });

            // ── BİLET SATIN ALMA ─────────────────────────────────────────────────
            document.getElementById('ticketForm').addEventListener('submit', async (e) => {
                e.preventDefault();
                const btn    = document.getElementById('submitBtn');
                const result = document.getElementById('result');

                // Formu oku
                const payload = {
                    EventName:    document.getElementById('eventName').value.trim(),
                    CustomerName: document.getElementById('customerName').value.trim(),
                    Seat:         document.getElementById('seat').value.trim(),
                    Price:        parseFloat(document.getElementById('price').value),
                    Status:       'Active',
                    PurchaseDate: new Date().toISOString()
                };

                // Yükleniyor durumu
                btn.disabled = true;
                btn.innerHTML = '<span class="spinner"></span>İşleniyor...';
                result.style.display = 'none';

                try {
                    const response = await fetch('/api/tickets', {
                        method:  'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'X-Api-Key':    API_KEY
                        },
                        body: JSON.stringify(payload)
                    });

                    const text = await response.text();
                    let pretty;
                    try   { pretty = JSON.stringify(JSON.parse(text), null, 2); }
                    catch { pretty = text; }

                    result.className = response.ok ? 'success' : 'error';
                    document.getElementById('resultTitle').textContent =
                        response.ok
                            ? `Bilet Oluşturuldu! (HTTP ${response.status})`
                            : `Hata Oluştu (HTTP ${response.status})`;
                    document.getElementById('resultBody').textContent = pretty;
                } catch (err) {
                    result.className = 'error';
                    document.getElementById('resultTitle').textContent = 'Bağlantı Hatası';
                    document.getElementById('resultBody').textContent  = err.message;
                } finally {
                    result.style.display = 'block';
                    btn.disabled = false;
                    btn.textContent = 'Bileti Satın Al';
                    result.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
                }
            });
        </script>
    </body>
    </html>
    """;

    return Results.Content(html, "text/html; charset=utf-8", Encoding.UTF8);
});

// ── GENEL YÖNLENDİRME (Auth + Routing) ──────────────────────────────────────
app.Map("{*path}", async (HttpContext context, string path, IHttpClientFactory clientFactory, IMongoClient mongoClient) =>
{
    var lowerPath = path?.ToLower() ?? "";

    // Auth endpoint'i doğrudan ilet (login için kimlik doğrulama gerekmez)
    if (lowerPath.Contains("auth"))
    {
        var authUrl = isDocker
            ? $"http://authservice:5002/{path}"
            : $"http://localhost:5002/{path}";
        return await ForwardRequest(context, authUrl, clientFactory);
    }

    // ── KİMLİK DOĞRULAMA: X-Api-Key veya JWT Bearer ──────────────────────────
    bool isAuthenticated = false;

    if (context.Request.Headers.TryGetValue("Authorization", out var authHeader)
        && authHeader.ToString().StartsWith("Bearer "))
    {
        var token = authHeader.ToString()[7..];
        isAuthenticated = ValidateJwt(token, jwtSecret);
    }
    else if (context.Request.Headers.TryGetValue("X-Api-Key", out var apiKey))
    {
        try
        {
            var db = mongoClient.GetDatabase("AuthServiceDb");
            var authCollection = db.GetCollection<BsonDocument>("ApiKeys");
            var filter = Builders<BsonDocument>.Filter.Eq("key", apiKey.ToString());
            var authRecord = await authCollection.Find(filter).FirstOrDefaultAsync();
            isAuthenticated = authRecord != null && authRecord["isActive"] != false;
        }
        catch
        {
            // MongoDB erişilemiyorsa geliştirme ortamı için geç
            isAuthenticated = true;
        }
    }

    if (!isAuthenticated)
        return Results.Json(new { error = "Yetkisiz erişim! X-Api-Key veya Authorization: Bearer <token> gönder." }, statusCode: 401);

    // ── HEDEF SERVİS BELİRLE ─────────────────────────────────────────────────
    string targetUrl;

    if (lowerPath.Contains("events"))
        targetUrl = isDocker ? $"http://eventservice:5001/{path}" : $"http://localhost:5001/{path}";
    else if (lowerPath.Contains("tickets"))
        targetUrl = isDocker ? $"http://ticketservice:5168/{path}" : $"http://localhost:5168/{path}";
    else
        return Results.Json(new { error = "Geçersiz servis yolu!", gelen_yol = path }, statusCode: 400);

    return await ForwardRequest(context, targetUrl, clientFactory);
});

app.Run();

// ── YARDIMCI METODLAR ─────────────────────────────────────────────────────────
static async Task<IResult> ForwardRequest(HttpContext context, string targetUrl, IHttpClientFactory clientFactory)
{
    try
    {
        var client = clientFactory.CreateClient();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        // Query string varsa URL'e ekle
        var fullUrl = context.Request.QueryString.HasValue
            ? targetUrl + context.Request.QueryString
            : targetUrl;

        var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), fullUrl);

        // Gövdeyi tamamen byte olarak oku (ContentLength bağımsız, StreamContent sorunlarını önler)
        if (context.Request.Method != "GET" && context.Request.Method != "DELETE")
        {
            using var ms = new MemoryStream();
            await context.Request.Body.CopyToAsync(ms);
            var bodyBytes = ms.ToArray();

            if (bodyBytes.Length > 0)
            {
                request.Content = new ByteArrayContent(bodyBytes);
                var contentType = context.Request.ContentType ?? "application/json";
                request.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);
            }
        }

        var response = await client.SendAsync(request, cts.Token);
        var content = await response.Content.ReadAsStringAsync();

        return Results.Content(content, "application/json", Encoding.UTF8, (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        return Results.Json(new
        {
            error = "Hedefe ulaşılamadı!",
            hedef = targetUrl,
            detay = ex.Message
        }, statusCode: 502);
    }
}

static bool ValidateJwt(string token, string secret)
{
    try
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(secret);
        tokenHandler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = "BiletSistemi",
            ValidateAudience = true,
            ValidAudience = "BiletSistemi",
            ClockSkew = TimeSpan.Zero
        }, out _);
        return true;
    }
    catch
    {
        return false;
    }
}
