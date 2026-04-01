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
// Dispatcher seed: API key ilk açılışta yoksa oluştur
try
{
    var client = app.Services.GetRequiredService<IMongoClient>();
    var db = client.GetDatabase("DispatcherDb");
    var apiKeys = db.GetCollection<BsonDocument>("ApiKeys");

    var keyFilter = Builders<BsonDocument>.Filter.Eq("key", "KingoSifre123");
    var existing = apiKeys.Find(keyFilter).FirstOrDefault();

    if (existing == null)
    {
        apiKeys.InsertOne(new BsonDocument
        {
            { "key", "KingoSifre123" },
            { "isActive", true }
        });

        Console.WriteLine("--> DispatcherDb: Varsayılan API key oluşturuldu (KingoSifre123)");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"--> Dispatcher seed hatası: {ex.Message}");
}
// ── TRAFİK LOGLAMA MIDDLEWARE ─────────────────────────────────────────────────
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var sw = Stopwatch.StartNew();
    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "?";
    logger.LogInformation("[TRAFİK] → {Method} {Path}{Query} | IP: {IP}",
        context.Request.Method, context.Request.Path, context.Request.QueryString, ip);
    await next();
    sw.Stop();
    logger.LogInformation("[TRAFİK] ← {Method} {Path} | Status: {Status} | {Ms}ms",
        context.Request.Method, context.Request.Path, context.Response.StatusCode, sw.ElapsedMilliseconds);
});

// ── PROMETHEUS METRİKLERİ ─────────────────────────────────────────────────────
app.UseHttpMetrics();
app.MapMetrics();

// ── ANA SAYFA ─────────────────────────────────────────────────────────────────
app.MapGet("/", () =>
{
    var html = """
<!DOCTYPE html>
<html lang="tr">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width,initial-scale=1.0">
<title>BiletSistemi — Etkinlik Biletleri</title>
<style>
*,*::before,*::after{box-sizing:border-box;margin:0;padding:0}
:root{
  --pink:#ff0080;--orange:#ff6b35;--gold:#ffd700;
  --grad:linear-gradient(135deg,#ff0080,#ff6b35);
  --grad-r:linear-gradient(135deg,#ff6b35,#ff0080);
  --bg:#000;--s1:#0d0d0d;--s2:#111;--s3:#1a1a1a;--s4:#222;
  --text:#fff;--muted:rgba(255,255,255,.5);--dim:rgba(255,255,255,.08);
  --border:rgba(255,255,255,.1);--r:12px;
  --tr:.28s cubic-bezier(.4,0,.2,1);
}
html{scroll-behavior:smooth}
body{background:var(--bg);color:var(--text);font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;min-height:100vh;overflow-x:hidden}

/* ── NAVBAR ── */
.navbar{
  position:fixed;top:0;left:0;right:0;z-index:100;
  background:rgba(0,0,0,.92);backdrop-filter:blur(24px);-webkit-backdrop-filter:blur(24px);
  border-bottom:1px solid rgba(255,0,128,.2);
  height:64px;display:flex;align-items:center;
}
.nav-inner{
  width:100%;max-width:1280px;margin:0 auto;padding:0 24px;
  display:flex;align-items:center;gap:32px;
}
.nav-logo{
  font-size:1.15rem;font-weight:900;letter-spacing:-.02em;
  background:var(--grad);-webkit-background-clip:text;-webkit-text-fill-color:transparent;background-clip:text;
  display:flex;align-items:center;gap:8px;white-space:nowrap;
  text-decoration:none;
}
.nav-logo .tick{font-size:1.25rem;-webkit-text-fill-color:initial}
.nav-links{display:flex;align-items:center;gap:4px;flex:1}
.nav-link{
  padding:8px 16px;background:none;border:none;color:var(--muted);
  font-size:.9rem;font-weight:500;cursor:pointer;border-radius:8px;
  transition:var(--tr);white-space:nowrap;
}
.nav-link:hover,.nav-link.active{color:var(--text);background:var(--dim)}
.nav-link.active{color:var(--pink)}
.nav-cta{
  margin-left:auto;padding:10px 22px;
  background:var(--grad);border:none;border-radius:8px;
  color:#fff;font-size:.88rem;font-weight:700;cursor:pointer;
  transition:var(--tr);white-space:nowrap;
}
.nav-cta:hover{box-shadow:0 0 24px rgba(255,0,128,.55),0 0 48px rgba(255,107,53,.2);transform:translateY(-1px)}

/* ── HERO ── */
.hero{
  padding:140px 24px 80px;
  background:radial-gradient(ellipse 80% 60% at 50% -10%,rgba(255,0,128,.15) 0%,transparent 70%),
             radial-gradient(ellipse 60% 50% at 80% 50%,rgba(255,107,53,.08) 0%,transparent 60%),
             var(--bg);
  position:relative;overflow:hidden;
}
.hero::before{
  content:'';position:absolute;inset:0;
  background-image:
    linear-gradient(rgba(255,255,255,.03) 1px,transparent 1px),
    linear-gradient(90deg,rgba(255,255,255,.03) 1px,transparent 1px);
  background-size:60px 60px;pointer-events:none;
}
.hero-inner{max-width:1280px;margin:0 auto;display:flex;align-items:center;gap:60px}
.hero-text{flex:1;max-width:680px}
.hero-badge{
  display:inline-flex;align-items:center;gap:8px;
  background:rgba(255,0,128,.1);border:1px solid rgba(255,0,128,.3);
  color:var(--pink);font-size:.75rem;font-weight:700;
  letter-spacing:.1em;text-transform:uppercase;
  padding:6px 14px;border-radius:999px;margin-bottom:28px;
}
.hero-badge-dot{width:7px;height:7px;background:var(--pink);border-radius:50%;animation:pulse-dot 1.4s ease infinite}
.hero h1{
  font-size:clamp(2.8rem,6vw,5.2rem);font-weight:900;line-height:1.06;
  letter-spacing:-.03em;margin-bottom:20px;
}
.hero h1 span{
  background:var(--grad);-webkit-background-clip:text;-webkit-text-fill-color:transparent;background-clip:text;
}
.hero-sub{color:var(--muted);font-size:1.1rem;line-height:1.6;max-width:500px;margin-bottom:36px}
.hero-actions{display:flex;gap:14px;flex-wrap:wrap}
.btn-primary{
  padding:14px 32px;background:var(--grad);border:none;border-radius:10px;
  color:#fff;font-size:1rem;font-weight:700;cursor:pointer;transition:var(--tr);
}
.btn-primary:hover{box-shadow:0 0 30px rgba(255,0,128,.6),0 0 60px rgba(255,107,53,.2);transform:translateY(-2px)}
.btn-ghost{
  padding:14px 32px;background:none;border:1px solid var(--border);border-radius:10px;
  color:var(--text);font-size:1rem;font-weight:600;cursor:pointer;transition:var(--tr);
}
.btn-ghost:hover{border-color:var(--pink);box-shadow:0 0 16px rgba(255,0,128,.2)}
.hero-stats{display:flex;gap:40px;margin-top:52px;padding-top:40px;border-top:1px solid var(--border)}
.hero-stat-num{font-size:1.8rem;font-weight:800;background:var(--grad);-webkit-background-clip:text;-webkit-text-fill-color:transparent;background-clip:text}
.hero-stat-lbl{font-size:.78rem;color:var(--muted);margin-top:2px}

/* ── MAIN ── */
.main-content{max-width:1280px;margin:0 auto;padding:60px 24px 100px}

/* ── SECTION HEADER ── */
.sec-head{display:flex;align-items:center;justify-content:space-between;margin-bottom:28px}
.sec-title{font-size:1.5rem;font-weight:800;letter-spacing:-.02em}
.sec-title span{background:var(--grad);-webkit-background-clip:text;-webkit-text-fill-color:transparent;background-clip:text}
.sec-badge{
  font-size:.75rem;font-weight:600;padding:4px 12px;border-radius:999px;
  background:rgba(255,0,128,.1);border:1px solid rgba(255,0,128,.3);color:var(--pink);
}

/* ── EVENT CARDS ── */
.events-grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(340px,1fr));gap:24px}
.ev-card{
  border-radius:16px;overflow:hidden;background:var(--s1);
  border:1px solid var(--border);transition:var(--tr);
  animation:fadeUp .45s ease both;cursor:pointer;
}
.ev-card:hover{transform:translateY(-6px);border-color:rgba(255,0,128,.4);box-shadow:0 20px 60px rgba(0,0,0,.6),0 0 40px rgba(255,0,128,.12)}
.ev-visual{
  height:200px;position:relative;overflow:hidden;
  display:flex;align-items:center;justify-content:center;
}
.ev-visual::after{
  content:'';position:absolute;bottom:0;left:0;right:0;height:60%;
  background:linear-gradient(to top,var(--s1),transparent);
}
.ev-emoji{font-size:5rem;filter:drop-shadow(0 0 30px currentColor);animation:float 3s ease-in-out infinite}
.ev-tags{
  position:absolute;top:14px;left:14px;
  display:flex;gap:6px;z-index:1;
}
.ev-tag{
  font-size:.65rem;font-weight:700;letter-spacing:.08em;text-transform:uppercase;
  padding:3px 9px;border-radius:4px;
  background:rgba(0,0,0,.6);backdrop-filter:blur(8px);border:1px solid rgba(255,255,255,.15);
}
.ev-hot{
  position:absolute;top:14px;right:14px;z-index:1;
  font-size:.68rem;font-weight:700;padding:3px 10px;border-radius:4px;
  background:rgba(255,0,0,.85);letter-spacing:.05em;text-transform:uppercase;
  animation:blink 1.2s ease infinite;
}
.ev-body{padding:20px}
.ev-name{font-size:1.15rem;font-weight:800;letter-spacing:-.02em;margin-bottom:10px;line-height:1.3}
.ev-meta{display:flex;gap:16px;margin-bottom:16px;flex-wrap:wrap}
.ev-meta-item{display:flex;align-items:center;gap:5px;font-size:.8rem;color:var(--muted)}
.ev-footer{display:flex;align-items:center;justify-content:space-between;margin-top:4px}
.ev-price{font-size:1.3rem;font-weight:900}
.ev-price-currency{font-size:.85rem;font-weight:600;color:var(--muted);margin-right:2px}
.ev-counter{display:flex;align-items:center;gap:6px;font-size:.78rem;font-weight:600}
.ev-counter-dot{width:7px;height:7px;border-radius:50%;background:#ff3333;flex-shrink:0;animation:pulse-dot 1.2s ease infinite}
.ev-counter-num{color:#ff6666;font-size:.92rem;font-weight:800;min-width:24px;display:inline-block;text-align:right}
.ev-counter-lbl{color:var(--muted)}
.buy-btn{
  padding:10px 22px;background:var(--grad);border:none;border-radius:8px;
  color:#fff;font-size:.85rem;font-weight:700;cursor:pointer;transition:var(--tr);
  white-space:nowrap;flex-shrink:0;
}
.buy-btn:hover{box-shadow:0 0 20px rgba(255,0,128,.6);transform:scale(1.04)}

/* ── TICKET CARDS ── */
.tickets-grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(320px,1fr));gap:20px}
.tk-card{
  background:var(--s1);border:1px solid var(--border);border-radius:16px;
  padding:0;overflow:hidden;transition:var(--tr);animation:fadeUp .4s ease both;
  position:relative;
}
.tk-card:hover{transform:translateY(-4px);border-color:rgba(255,0,128,.3);box-shadow:0 16px 48px rgba(0,0,0,.5)}
.tk-card-top{
  padding:18px 20px 14px;
  background:linear-gradient(135deg,var(--s2),var(--s3));
  border-bottom:1px solid var(--border);
  display:flex;align-items:center;justify-content:space-between;
}
.tk-status{
  font-size:.68rem;font-weight:700;letter-spacing:.08em;text-transform:uppercase;
  padding:3px 10px;border-radius:4px;
}
.tk-status.active{background:rgba(0,255,136,.12);border:1px solid rgba(0,255,136,.3);color:#00ff88}
.tk-status.other {background:rgba(255,50,50,.12);border:1px solid rgba(255,50,50,.3);color:#ff6666}
.tk-id{font-size:.7rem;color:var(--muted);font-family:monospace}
.tk-body{padding:20px}
.tk-event{font-size:1.05rem;font-weight:800;letter-spacing:-.01em;margin-bottom:16px}
.tk-grid{display:grid;grid-template-columns:1fr 1fr;gap:12px;margin-bottom:16px}
.tk-field label{font-size:.65rem;text-transform:uppercase;letter-spacing:.08em;color:var(--muted);display:block;margin-bottom:3px}
.tk-field span{font-size:.9rem;font-weight:600}
.tk-price-big{font-size:1.25rem;font-weight:900;background:var(--grad);-webkit-background-clip:text;-webkit-text-fill-color:transparent;background-clip:text}
.tk-barcode{
  margin-top:16px;padding-top:16px;border-top:1px dashed var(--border);
  display:flex;align-items:center;gap:3px;
}
.tk-bar{background:var(--text);flex-shrink:0}

/* ── LOADING ── */
.loading-screen{display:flex;flex-direction:column;align-items:center;justify-content:center;padding:100px 20px;gap:20px}
.loader{width:48px;height:48px;border:3px solid var(--s3);border-top:3px solid var(--pink);border-radius:50%;animation:spin .75s linear infinite}
.loading-text{color:var(--muted);font-size:.95rem}

/* ── EMPTY ── */
.empty-screen{display:flex;flex-direction:column;align-items:center;padding:80px 20px;gap:12px}
.empty-ico{font-size:4rem;opacity:.35}
.empty-text{color:var(--muted);font-size:.95rem}

/* ── PAGINATION ── */
.pagination{display:none;align-items:center;justify-content:center;gap:12px;margin-top:48px}
.pagination.on{display:flex}
.pg-btn{
  padding:10px 24px;background:var(--s2);border:1px solid var(--border);
  border-radius:8px;color:var(--text);font-size:.88rem;font-weight:600;cursor:pointer;transition:var(--tr);
}
.pg-btn:hover:not(:disabled){border-color:var(--pink);box-shadow:0 0 16px rgba(255,0,128,.25)}
.pg-btn:disabled{opacity:.3;cursor:not-allowed}
.pg-info{font-size:.82rem;color:var(--muted);background:var(--s2);border:1px solid var(--border);padding:8px 18px;border-radius:8px}

/* ── MODAL ── */
.modal-overlay{
  display:none;position:fixed;inset:0;
  background:rgba(0,0,0,.85);backdrop-filter:blur(12px);-webkit-backdrop-filter:blur(12px);
  z-index:200;align-items:center;justify-content:center;padding:20px;
}
.modal-overlay.open{display:flex;animation:fadeIn .2s ease}
.modal{
  background:var(--s1);border:1px solid rgba(255,0,128,.25);
  border-radius:20px;width:100%;max-width:500px;overflow:hidden;
  animation:slideUp .3s cubic-bezier(.4,0,.2,1);
  box-shadow:0 40px 100px rgba(0,0,0,.7),0 0 80px rgba(255,0,128,.08);
}
.modal-header{
  background:linear-gradient(135deg,rgba(255,0,128,.15),rgba(255,107,53,.1));
  border-bottom:1px solid rgba(255,0,128,.2);
  padding:22px 28px;
  display:flex;align-items:center;justify-content:space-between;
}
.modal-title{font-size:1.1rem;font-weight:800;letter-spacing:-.01em}
.modal-close{
  width:34px;height:34px;background:var(--dim);border:1px solid var(--border);
  border-radius:50%;color:var(--muted);font-size:1rem;cursor:pointer;
  display:flex;align-items:center;justify-content:center;transition:var(--tr);
}
.modal-close:hover{background:rgba(255,50,50,.2);border-color:#f44;color:#f66}
.modal-body{padding:28px}
.f-group{margin-bottom:18px}
.f-label{display:block;font-size:.72rem;text-transform:uppercase;letter-spacing:.08em;color:var(--muted);font-weight:600;margin-bottom:7px}
.f-input{
  width:100%;background:var(--s3);border:1px solid var(--border);border-radius:10px;
  color:var(--text);padding:12px 15px;font-size:.95rem;outline:none;transition:var(--tr);
}
.f-input:focus{border-color:var(--pink);background:rgba(255,0,128,.05);box-shadow:0 0 0 3px rgba(255,0,128,.1)}

select#fEvent,
select.f-input#fEvent{
  background:#000 !important;
  color:#fff;
  border-color:rgba(255,255,255,.25);
}
select#fEvent:focus{
  background:#0a0a0a !important;
  color:#fff;
  border-color:var(--pink);
  box-shadow:0 0 0 3px rgba(255,0,128,.15);
}
select#fEvent option{
  background:#111;
  color:#fff;
}
.f-row{display:grid;grid-template-columns:1fr 1fr;gap:14px}
.auth-badge{
  display:flex;align-items:center;gap:10px;
  background:rgba(0,255,136,.06);border:1px solid rgba(0,255,136,.2);
  border-radius:10px;padding:11px 15px;font-size:.82rem;color:#00cc6a;
}
.auth-badge-icon{font-size:1rem}
.modal-submit{
  width:100%;padding:14px;margin-top:8px;
  background:var(--grad);border:none;border-radius:10px;
  color:#fff;font-size:1rem;font-weight:800;cursor:pointer;
  transition:var(--tr);letter-spacing:.02em;
}
.modal-submit:hover{box-shadow:0 0 28px rgba(255,0,128,.55);transform:translateY(-1px)}
.modal-submit:disabled{opacity:.4;cursor:not-allowed;transform:none;box-shadow:none}

.modal.modal-admin{max-width:min(980px,96vw);max-height:min(92vh,900px);display:flex;flex-direction:column}
.ap-panels{flex:1;overflow-y:auto;padding:22px 24px 28px}
.api-toolbar{display:flex;flex-wrap:wrap;gap:10px;margin-bottom:14px}
.api-action-btn{
  display:inline-flex;align-items:center;gap:8px;padding:10px 14px;border-radius:10px;
  border:1px solid var(--border);background:var(--s3);color:var(--text);
  font-size:.82rem;font-weight:700;cursor:pointer;transition:var(--tr);
}
.api-action-btn:hover{border-color:var(--pink);box-shadow:0 0 18px rgba(255,0,128,.2)}
.api-action-btn .meth{
  font-family:ui-monospace,Consolas,monospace;font-size:.68rem;font-weight:800;
  padding:3px 7px;border-radius:5px;background:rgba(255,0,128,.2);color:#ff88cc;
}
.api-action-btn.m-key .meth{background:rgba(255,200,0,.15);color:#ffd080}
.api-json-out{
  margin:0;padding:16px;border-radius:12px;background:#070708;border:1px solid var(--border);
  font-family:ui-monospace,Consolas,monospace;font-size:.72rem;line-height:1.5;color:#c8e0c8;
  white-space:pre-wrap;word-break:break-word;max-height:min(42vh,380px);overflow:auto;
}
.api-json-meta{font-size:.75rem;color:var(--muted);margin-bottom:8px}
.nav-admin{
  display:none;
  background:linear-gradient(135deg,rgba(255,200,80,.15),rgba(255,0,128,.12)) !important;
  border:1px solid rgba(255,200,100,.35) !important;
  box-shadow:0 0 20px rgba(255,180,60,.12) !important;
}

/* ── TOAST ── */
.toast{
  position:fixed;bottom:28px;right:28px;z-index:300;
  max-width:360px;padding:14px 20px;border-radius:12px;
  font-size:.9rem;font-weight:600;backdrop-filter:blur(16px);
  opacity:0;transform:translateX(16px);pointer-events:none;
  transition:opacity .3s,transform .3s;
}
.toast.on{opacity:1;transform:translateX(0);pointer-events:auto}
.toast.ok {background:rgba(0,255,136,.1);border:1px solid rgba(0,255,136,.3);color:#00ff88}
.toast.err{background:rgba(255,50,50,.1);border:1px solid rgba(255,50,50,.3);color:#ff6666}

/* ── ANIMATIONS ── */
@keyframes fadeUp   {from{opacity:0;transform:translateY(24px)}to{opacity:1;transform:translateY(0)}}
@keyframes fadeIn   {from{opacity:0}to{opacity:1}}
@keyframes slideUp  {from{opacity:0;transform:translateY(28px)}to{opacity:1;transform:translateY(0)}}
@keyframes spin     {to{transform:rotate(360deg)}}
@keyframes float    {0%,100%{transform:translateY(0) scale(1)} 50%{transform:translateY(-10px) scale(1.05)}}
@keyframes pulse-dot{0%,100%{transform:scale(1);opacity:1} 50%{transform:scale(1.6);opacity:.6}}
@keyframes blink    {0%,100%{opacity:1} 50%{opacity:.5}}
@keyframes countUp  {from{transform:translateY(8px);opacity:0} to{transform:translateY(0);opacity:1}}

@media(max-width:768px){
  .hero-stats{display:none}
  .hero h1{font-size:2.4rem}
  .events-grid,.tickets-grid{grid-template-columns:1fr}
  .f-row{grid-template-columns:1fr}
  .modal{max-width:100%}
}
</style>
</head>
<body>

<!-- NAVBAR -->
<nav class="navbar">
  <div class="nav-inner">
    <a class="nav-logo" href="#"><span class="tick">&#127903;</span>BiletSistemi</a>
    <div class="nav-links">
      <button class="nav-link" id="navEvents">&#127908; Etkinlikler</button>
      <button class="nav-link" id="navTickets">&#127915; Biletlerim</button>
    </div>
    <button class="nav-cta" id="navLogin">Giriş Yap</button>
    <button class="nav-cta" id="navRegister" style="background:transparent;border:1px solid rgba(255,0,128,.45);box-shadow:none;">Kayıt Ol</button>
    <button class="nav-cta" id="navLogout" style="display:none;">Çıkış Yap</button>
    <button class="nav-cta nav-admin" id="navAdminPanel" type="button" style="display:none;">&#9881; Yönetim</button>
    <button class="nav-cta" id="navBuy">+ Bilet Satın Al</button>
  </div>
</nav>

<!-- HERO -->
<section class="hero" id="heroSection">
  <div class="hero-inner">
    <div class="hero-text">
      <div class="hero-badge">
        <span class="hero-badge-dot"></span>
        Canlı Etkinlikler &#8212; Türkiye Geneli
      </div>
      <h1>Unutulmaz<br>Anların<br><span>Tek Adresi.</span></h1>
      <p class="hero-sub">Konserler, festivaller, tiyatrolar ve özel etkinlikler. Hepsini tek platformdan keşfet, anında satın al.</p>
      <div class="hero-actions">
        <button class="btn-primary" id="heroBrowse">Etkinlikleri Keşfet &#8594;</button>
        <button class="btn-ghost" id="heroTickets">Biletlerim</button>
      </div>
      <div class="hero-stats">
        <div>
          <div class="hero-stat-num">1,200+</div>
          <div class="hero-stat-lbl">Aktif Etkinlik</div>
        </div>
        <div>
          <div class="hero-stat-num">50K+</div>
          <div class="hero-stat-lbl">Satılan Bilet</div>
        </div>
        <div>
          <div class="hero-stat-num">%98</div>
          <div class="hero-stat-lbl">Memnuniyet</div>
        </div>
      </div>
    </div>
  </div>
</section>

<!-- MAIN CONTENT -->
<main class="main-content">
  <div id="content-area"></div>
  <div class="pagination" id="pagination">
    <button class="pg-btn" id="btnPrev">&#8592; Önceki</button>
    <span class="pg-info" id="pgInfo"></span>
    <button class="pg-btn" id="btnNext">Sonraki &#8594;</button>
  </div>
</main>

<!-- MODAL -->
<div class="modal-overlay" id="mOverlay">
  <div class="modal">
    <div class="modal-header">
      <span class="modal-title">&#127915; Bilet Satın Al</span>
      <button class="modal-close" id="mClose">&#10005;</button>
    </div>
    <div class="modal-body">
      <form id="ticketForm">
        <div class="f-group">
          <label class="f-label">Etkinlik Adı</label>
        <select class="f-input" id="fEvent" required>
  <option value="">Etkinlik seçin</option>
</select>
        </div>
        <div class="f-row">
          <div class="f-group">
            <label class="f-label">Müşteri Adı</label>
            <input class="f-input" type="text" id="fCustomer" placeholder="Ad Soyad" required>
          </div>
          <div class="f-group">
            <label class="f-label">Koltuk</label>
            <input class="f-input" type="text" id="fSeat" placeholder="A-12" required>
          </div>
        </div>
        <div class="f-group">
          <label class="f-label">Fiyat (TL)</label>
          <input class="f-input" type="number" id="fPrice" placeholder="500" min="0" required>
        </div>
        <div class="f-group">
          <label class="f-label">Hesap</label>
          <div class="auth-badge" id="ticketAuthHint"><span class="auth-badge-icon">&#9888;</span> Bilet görmek ve satın almak için giriş yapın</div>
        </div>
        <button type="submit" class="modal-submit" id="submitBtn">Satın Al &#8594;</button>
      </form>
    </div>
  </div>
</div>
<div class="modal-overlay" id="loginOverlay">
  <div class="modal">
    <div class="modal-header">
      <span class="modal-title">&#128274; Giriş Yap</span>
      <button class="modal-close" id="loginClose">&#10005;</button>
    </div>

    <div class="modal-body">
      <form id="loginForm">
        <div class="f-group">
          <label class="f-label">Kullanıcı Adı</label>
          <input class="f-input" type="text" id="loginUsername" value="admin" required>
        </div>

        <div class="f-group">
          <label class="f-label">Şifre</label>
          <input class="f-input" type="password" id="loginPassword" value="Bilet2026" required>
        </div>

        <button type="submit" class="modal-submit" id="loginBtn">Token Al</button>
      </form>
    </div>
  </div>
</div>
<div class="modal-overlay" id="registerOverlay">
  <div class="modal">
    <div class="modal-header">
      <span class="modal-title">&#128100; Kayıt Ol</span>
      <button class="modal-close" id="registerClose">&#10005;</button>
    </div>
    <div class="modal-body">
      <form id="registerForm">
        <div class="f-group">
          <label class="f-label">Kullanıcı Adı</label>
          <input class="f-input" type="text" id="regUsername" placeholder="En az 3 karakter" required minlength="3" autocomplete="username">
        </div>
        <div class="f-group">
          <label class="f-label">Şifre</label>
          <input class="f-input" type="password" id="regPassword" placeholder="En az 6 karakter" required minlength="6" autocomplete="new-password">
        </div>
        <div class="f-group">
          <label class="f-label">Şifre (tekrar)</label>
          <input class="f-input" type="password" id="regPassword2" placeholder="Tekrar girin" required minlength="6" autocomplete="new-password">
        </div>
        <button type="submit" class="modal-submit" id="registerBtn">Hesap Oluştur</button>
      </form>
    </div>
  </div>
</div>
<div class="modal-overlay" id="adminOverlay">
  <div class="modal modal-admin">
    <div class="modal-header">
      <span class="modal-title">&#9881; API konsolu</span>
      <button class="modal-close" id="adminClose" type="button">&#10005;</button>
    </div>
    <div class="ap-panels">
      <p class="api-json-meta">İstekler bu tarayıcıdaki oturum (JWT) veya anahtar ile gider; yanıt aşağıda görünür.</p>
      <div class="api-toolbar">
        <button type="button" class="api-action-btn" id="admGetEvents"><span class="meth">GET</span> /api/events</button>
        <button type="button" class="api-action-btn" id="admGetTickets"><span class="meth">GET</span> /api/tickets</button>
        <button type="button" class="api-action-btn m-key" id="admGetTicketsKey"><span class="meth">GET</span> Biletler (API key)</button>
        <button type="button" class="api-action-btn" id="admPostValidate"><span class="meth">POST</span> /api/auth/validate</button>
        <button type="button" class="api-action-btn" id="admPostTicket"><span class="meth">POST</span> /api/tickets</button>
      </div>
      <div class="api-json-meta" id="adminApiMeta"></div>
      <pre class="api-json-out" id="adminApiOut">Henüz istek yok. Yukarıdan bir işlem seçin.</pre>
    </div>
  </div>
</div>
<div class="toast" id="toast"></div>

<script>

const API_KEY = 'KingoSifre123';
const TOKEN_KEY = 'bilet_token';
const ROLE_KEY = 'bilet_role';

function parseJwtRole(token){
  try{
    const p=JSON.parse(atob(token.split('.')[1].replace(/-/g,'+').replace(/_/g,'/')));
    return p.role||p['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']||'';
  }catch{ return ''; }
}
function getStoredRole(){
  let r=localStorage.getItem(ROLE_KEY)||'';
  const t=localStorage.getItem(TOKEN_KEY);
  if(!r&&t) r=parseJwtRole(t);
  return r;
}
function isAdminUser(){ return getStoredRole()==='admin'; }

function setAuthUi(){
  const token = localStorage.getItem(TOKEN_KEY);
  const loginBtn = document.getElementById('navLogin');
  const registerBtn = document.getElementById('navRegister');
  const logoutBtn = document.getElementById('navLogout');
  const adminBtn = document.getElementById('navAdminPanel');
  const hint = document.getElementById('ticketAuthHint');

  if(token){
    loginBtn.style.display = 'none';
    if(registerBtn) registerBtn.style.display = 'none';
    logoutBtn.style.display = 'inline-block';
    if(adminBtn) adminBtn.style.display = isAdminUser() ? 'inline-block' : 'none';
    if(hint) hint.innerHTML = '<span class="auth-badge-icon">&#10003;</span> Bilet hesabınıza kaydedilecek';
  } else {
    loginBtn.style.display = 'inline-block';
    if(registerBtn) registerBtn.style.display = 'inline-block';
    logoutBtn.style.display = 'none';
    if(adminBtn) adminBtn.style.display = 'none';
    if(hint) hint.innerHTML = '<span class="auth-badge-icon">&#9888;</span> Bilet görmek ve satın almak için giriş yapın';
  }
}

/** Etkinlikler vb. — tarayıcıda oturum yoksa API anahtarı (herkese açık liste) */
function getAuthHeaders(contentType = false){
  const token = localStorage.getItem(TOKEN_KEY);
  const h = {};
  if (token) h['Authorization'] = `Bearer ${token}`;
  else h['X-Api-Key'] = API_KEY;
  if (contentType) h['Content-Type'] = 'application/json';
  return h;
}

/** Bilet listesi / satın alma — sadece giriş yapmış kullanıcı (JWT); API anahtarı yok */
function getBearerHeaders(contentType = false){
  const token = localStorage.getItem(TOKEN_KEY);
  const h = {};
  if (token) h['Authorization'] = `Bearer ${token}`;
  if (contentType) h['Content-Type'] = 'application/json';
  return h;
}
const THEMES = [
  {bg:'linear-gradient(160deg,#1a0533,#3d0070,#6600cc)',dot:'#cc44ff',icon:'🎤',tags:['KONSER','LIVE','POP']},
  {bg:'linear-gradient(160deg,#330000,#660019,#cc0033)',dot:'#ff3366',icon:'🎸',tags:['ROCK','LIVE','18+']},
  {bg:'linear-gradient(160deg,#001a33,#003366,#0055bb)',dot:'#00aaff',icon:'🎹',tags:['KLASİK','GALİ']},
  {bg:'linear-gradient(160deg,#0a1a00,#1a4400,#2d7700)',dot:'#44ff88',icon:'🎷',tags:['CAZ','BLUES']},
  {bg:'linear-gradient(160deg,#1a1000,#443300,#886600)',dot:'#ffcc00',icon:'🎺',tags:['FESTIVAL','OPEN AIR']},
  {bg:'linear-gradient(160deg,#1a0011,#440033,#880066)',dot:'#ff44cc',icon:'🥁',tags:['EDM','DANS','18+']},
  {bg:'linear-gradient(160deg,#001a1a,#004444,#007777)',dot:'#00ffee',icon:'🎻',tags:['OPERA','KLASİK']},
  {bg:'linear-gradient(160deg,#1a0d00,#4a2200,#993300)',dot:'#ff8844',icon:'🎵',tags:['POP','TÜRKÇE']},
];

let tPage=1, tTotal=0;
let cachedEvents = [];
/** Bilet modalında her zaman gösterilen sabit konserler + API'den gelenler (isim tekrarında API öncelikli) */
function mergeTicketEventOptions(){
  const defaults = [
    { name: 'Tarkan Konseri', price: 500 },
    { name: 'Sertab Erener Konseri', price: 450 },
    { name: 'Sezen Aksu Konseri', price: 550 }
  ];
  const byName = new Map();
  defaults.forEach(e => byName.set(e.name, { ...e }));
  cachedEvents.forEach(e => {
    if (!e || !e.name) return;
    const prev = byName.get(e.name);
    byName.set(e.name, prev
      ? { ...prev, ...e, price: e.price != null ? e.price : prev.price }
      : { ...e });
  });
  return Array.from(byName.values());
}
const counterIntervals=[];

// ── TOAST ─────────────────────────────────────────────────────────────────────
function toast(msg,type='ok',ms=3600){
  const el=document.getElementById('toast');
  el.textContent=msg; el.className=`toast ${type} on`;
  clearTimeout(el._t); el._t=setTimeout(()=>el.classList.remove('on'),ms);
}

// ── MODAL ─────────────────────────────────────────────────────────────────────
function openModal(ev='',price=''){
  populateEventOptions();
  document.getElementById('fEvent').value = ev || '';
  document.getElementById('fPrice').value = price || '';
  document.getElementById('mOverlay').classList.add('open');
  setTimeout(()=>document.getElementById(ev?'fCustomer':'fEvent').focus(),60);
}
function closeModal(){
  document.getElementById('mOverlay').classList.remove('open');
  document.getElementById('ticketForm').reset();
}
document.getElementById('mClose').onclick=closeModal;
document.getElementById('navBuy').onclick=()=>openModal();
document.getElementById('mOverlay').addEventListener('click',e=>{
  if(e.target===document.getElementById('mOverlay'))closeModal();});
function openLogin(){ document.getElementById('loginOverlay').classList.add('open'); document.getElementById('registerOverlay')?.classList.remove('open'); }
function closeLogin(){ document.getElementById('loginOverlay').classList.remove('open'); }

function openRegister(){ document.getElementById('registerOverlay').classList.add('open'); document.getElementById('loginOverlay')?.classList.remove('open'); }
function closeRegister(){ document.getElementById('registerOverlay').classList.remove('open'); document.getElementById('registerForm')?.reset(); }

document.getElementById('navLogin').onclick = openLogin;
document.getElementById('navRegister').onclick = openRegister;
document.getElementById('navLogout').onclick = () => {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(ROLE_KEY);
  document.getElementById('adminOverlay')?.classList.remove('open');
  setAuthUi();
  toast('Çıkış yapıldı', 'ok');
};
document.getElementById('loginClose').onclick = closeLogin;
document.getElementById('loginOverlay').addEventListener('click', e => {
  if (e.target.id === 'loginOverlay') closeLogin();
});

document.getElementById('registerClose').onclick = closeRegister;
document.getElementById('registerOverlay').addEventListener('click', e => {
  if (e.target.id === 'registerOverlay') closeRegister();
});

document.getElementById('registerForm').addEventListener('submit', async (e) => {
  e.preventDefault();
  const username = document.getElementById('regUsername').value.trim();
  const password = document.getElementById('regPassword').value;
  const password2 = document.getElementById('regPassword2').value;
  const btn = document.getElementById('registerBtn');
  if (password !== password2) return toast('Şifreler eşleşmiyor', 'err');
  btn.disabled = true; btn.textContent = 'Kaydediliyor...';
  try {
    const res = await fetch('/api/auth/register', {
      method: 'POST',
      headers: {'Content-Type':'application/json'},
      body: JSON.stringify({ Username: username, Password: password })
    });
    const data = await res.json().catch(() => ({}));
    if (!res.ok) return toast(data.error || ('Kayıt başarısız: HTTP ' + res.status), 'err');
    closeRegister();
    document.getElementById('loginUsername').value = username;
    document.getElementById('loginPassword').value = '';
    openLogin();
    toast('Kayıt tamam. Şimdi giriş yapabilirsin.', 'ok');
  } catch (err) {
    toast('Bağlantı hatası: ' + err.message, 'err');
  } finally {
    btn.disabled = false; btn.textContent = 'Hesap Oluştur';
  }
});

document.getElementById('loginForm').addEventListener('submit', async (e) => {
  e.preventDefault();
  const username = document.getElementById('loginUsername').value.trim();
  const password = document.getElementById('loginPassword').value.trim();

  const res = await fetch('/api/auth/login', {
    method: 'POST',
    headers: {'Content-Type':'application/json'},
    body: JSON.stringify({ Username: username, Password: password })
  });

  const data = await res.json().catch(() => ({}));
  if (!res.ok || !data.token) return toast(data.error || 'Login başarısız', 'err');

  localStorage.setItem(TOKEN_KEY, data.token);
  localStorage.setItem(ROLE_KEY, data.role || parseJwtRole(data.token) || '');
  setAuthUi();
  closeLogin();
  toast('Giriş başarılı, JWT kaydedildi', 'ok');
});

function adminFetch(label,url,init){
  const meta=document.getElementById('adminApiMeta');
  const out=document.getElementById('adminApiOut');
  meta.textContent='İstek gönderiliyor…';
  out.textContent='';
  fetch(url,init).then(async r=>{
    const raw=await r.text();
    let pretty=raw;
    try{pretty=JSON.stringify(JSON.parse(raw),null,2);}catch{}
    meta.textContent=label+' — HTTP '+r.status+' '+r.statusText;
    out.textContent=pretty;
  }).catch(e=>{
    meta.textContent=label+' — Ağ hatası';
    out.textContent=e.message||String(e);
  });
}
document.getElementById('navAdminPanel').onclick=()=>{
  if(!isAdminUser()) return toast('Bu alan yalnızca yöneticiler içindir','err');
  document.getElementById('adminOverlay').classList.add('open');
};
document.getElementById('adminClose').onclick=()=>document.getElementById('adminOverlay').classList.remove('open');
document.getElementById('adminOverlay').addEventListener('click',e=>{
  if(e.target.id==='adminOverlay')document.getElementById('adminOverlay').classList.remove('open');
});
document.getElementById('admGetEvents').onclick=()=>
  adminFetch('GET /api/events','/api/events',{headers:getAuthHeaders()});
document.getElementById('admGetTickets').onclick=()=>
  adminFetch('GET /api/tickets','/api/tickets?page=1&pageSize=10',{headers:getBearerHeaders()});
document.getElementById('admGetTicketsKey').onclick=()=>
  adminFetch('GET /api/tickets (yalnızca X-Api-Key)','/api/tickets?page=1&pageSize=10',{headers:{'X-Api-Key':API_KEY}});
document.getElementById('admPostValidate').onclick=()=>{
  const t=localStorage.getItem(TOKEN_KEY);
  if(!t)return toast('Önce giriş yapın','err');
  adminFetch('POST /api/auth/validate','/api/auth/validate',{
    method:'POST',
    headers:{'Content-Type':'application/json'},
    body:JSON.stringify({token:t})
  });
};
document.getElementById('admPostTicket').onclick=()=>{
  if(!localStorage.getItem(TOKEN_KEY))return toast('Önce giriş yapın','err');
  adminFetch('POST /api/tickets','/api/tickets',{
    method:'POST',
    headers:getBearerHeaders(true),
    body:JSON.stringify({
      eventName:'Yönetim paneli testi',
      customerName:'Admin',
      seat:'ADM-1',
      status:'Active',
      price:1,
      purchaseDate:new Date().toISOString()
    })
  });
};

// ── COUNTER ANIMATION ─────────────────────────────────────────────────────────
function seedRng(str){
  let h=5381; for(let c of str) h=((h<<5)+h)+c.charCodeAt(0); return Math.abs(h);
}
function getRemaining(id){ return (seedRng(id||'x')%55)+18; } // 18-72

function animateCounter(el,target){
  let v=target+Math.floor(seedRng(target+'s')%18)+4;
  el.textContent=v;
  const tid=setInterval(()=>{
    if(v<=target){clearInterval(tid);el.style.animation='countUp .25s ease';return;}
    v--; el.textContent=v;
    if(v<=20){el.style.color='#ff3333';}
    else if(v<=35){el.style.color='#ff8c00';}
  },60);
  // slow live ticker after initial animation
  const liveTid=setInterval(()=>{
    const cur=parseInt(el.textContent)||0;
    if(cur>5&&Math.random()<.3){el.textContent=cur-1;}
  },Math.random()*12000+8000);
  counterIntervals.push(tid,liveTid);
}

function clearCounters(){ counterIntervals.forEach(clearInterval); counterIntervals.length=0; }

// ── HELPERS ───────────────────────────────────────────────────────────────────
function populateEventOptions(){
  const sel = document.getElementById('fEvent');
  if(!sel) return;

  const current = sel.value;
  sel.innerHTML = '<option value="">Etkinlik seçin</option>';

  mergeTicketEventOptions().forEach(e => {
    const opt = document.createElement('option');
    opt.value = e.name || '';
    opt.textContent = `${e.name || 'İsimsiz Etkinlik'}${e.price != null ? ` — ${Number(e.price).toLocaleString('tr-TR')} ₺` : ''}`;
    if (opt.value === current) opt.selected = true;
    sel.appendChild(opt);
  });
}

const fEventEl = document.getElementById('fEvent');
if (fEventEl) {
  fEventEl.addEventListener('change', (e) => {
    const selected = mergeTicketEventOptions().find(x => (x.name || '') === e.target.value);
    if (selected && selected.price != null) {
      document.getElementById('fPrice').value = selected.price;
    }
  });
}

function esc(s){return String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;');}
function fmtD(s,short){
  if(!s)return '—';
  try{const d=new Date(s);return d.toLocaleDateString('tr-TR',short?{day:'numeric',month:'short'}:{day:'numeric',month:'long',year:'numeric'});}
  catch{return s;}
}
function fmtP(n){return n!=null?Number(n).toLocaleString('tr-TR')+' &#8378;':'—';}

function setActiveNav(id){
  ['navEvents','navTickets'].forEach(b=>document.getElementById(b).classList.remove('active'));
  document.getElementById(id).classList.add('active');
}
function showLoad(msg='Yükleniyor...'){
  clearCounters();
  document.getElementById('content-area').innerHTML=
    `<div class="loading-screen"><div class="loader"></div><div class="loading-text">${msg}</div></div>`;
  document.getElementById('pagination').classList.remove('on');
}
function showEmpty(ico,msg){
  clearCounters();
  document.getElementById('content-area').innerHTML=
    `<div class="empty-screen"><div class="empty-ico">${ico}</div><div class="empty-text">${msg}</div></div>`;
  document.getElementById('pagination').classList.remove('on');
}

// ── BARCODE SVG ───────────────────────────────────────────────────────────────
function barcode(id){
  const seed=seedRng(id||'x');
  let bars='';
  for(let i=0;i<38;i++){
    const w=((seed>>(i%16))&3)+1;
    const x=i*6;
    bars+=`<rect x="${x}" y="0" width="${w}" height="28" fill="rgba(255,255,255,0.7)"/>`;
  }
  return `<svg width="232" height="28" viewBox="0 0 232 28">${bars}</svg>`;
}

// ── RENDER EVENTS ─────────────────────────────────────────────────────────────
function renderEvents(payload){
  clearCounters();
  const rows=payload.data;
  document.getElementById('pagination').classList.remove('on');
  if(!rows||!rows.length){showEmpty('🎭','Henüz etkinlik eklenmemiş.');return;}
  const area=document.getElementById('content-area');
  area.innerHTML=`
    <div class="sec-head">
      <h2 class="sec-title">Öne Çıkan <span>Etkinlikler</span></h2>
      <span class="sec-badge">${rows.length} etkinlik</span>
    </div>
    <div class="events-grid" id="evGrid"></div>`;
  const grid=document.getElementById('evGrid');
  rows.forEach((item,i)=>{
    const e=item.data;
    const th=THEMES[i%THEMES.length];
    const rem=getRemaining(e.id||String(i));
    const isHot=rem<25;
    const div=document.createElement('div');
    div.className='ev-card';
    div.style.animationDelay=`${i*70}ms`;
    div.innerHTML=`
      <div class="ev-visual" style="background:${th.bg}">
        <div class="ev-tags">${th.tags.map(t=>`<span class="ev-tag">${t}</span>`).join('')}</div>
        ${isHot?'<div class="ev-hot">&#128293; Son Biletler</div>':''}
        <span class="ev-emoji">${th.icon}</span>
      </div>
      <div class="ev-body">
        <div class="ev-name">${esc(e.name||'—')}</div>
        <div class="ev-meta">
          <span class="ev-meta-item">&#128205; ${esc(e.location||'—')}</span>
          <span class="ev-meta-item">&#128197; ${fmtD(e.date,false)}</span>
        </div>
        <div class="ev-footer">
          <div>
            <div class="ev-counter">
              <span class="ev-counter-dot"></span>
              <span class="ev-counter-num" id="cnt-${i}">—</span>
              <span class="ev-counter-lbl">bilet kaldı</span>
            </div>
            <div style="font-size:1.3rem;font-weight:900;margin-top:6px">
              <span style="font-size:.82rem;color:var(--muted);font-weight:600">&#8378; </span>${e.price!=null?Number(e.price).toLocaleString('tr-TR'):'—'}
            </div>
          </div>
          <button class="buy-btn" id="buy-${i}">Bilet Al &#8594;</button>
        </div>
      </div>`;
    grid.appendChild(div);
    animateCounter(document.getElementById(`cnt-${i}`),rem);
    document.getElementById(`buy-${i}`).addEventListener('click',()=>openModal(e.name||'',e.price||''));
  });
}

// ── RENDER TICKETS ────────────────────────────────────────────────────────────
function renderTickets(payload){
  clearCounters();
  const pg=payload.pagination;
  const rows=payload.data;
  if(!rows||!rows.length){showEmpty('🎫','Henüz bilet yok.');return;}
  const area=document.getElementById('content-area');
  area.innerHTML=`
    <div class="sec-head">
      <h2 class="sec-title">Bilet <span>Geçmişim</span></h2>
      <span class="sec-badge">${pg.totalCount} bilet</span>
    </div>
    <div class="tickets-grid" id="tkGrid"></div>`;
  const grid=document.getElementById('tkGrid');
  rows.forEach((item,i)=>{
    const t=item.data;
    const act=(t.status||'').toLowerCase()==='active';
    const div=document.createElement('div');
    div.className='tk-card';
    div.style.animationDelay=`${i*65}ms`;
    div.innerHTML=`
      <div class="tk-card-top">
        <span class="tk-status ${act?'active':'other'}">${act?'&#9679; Aktif':'&#9679; '+esc(t.status||'?')}</span>
        <span class="tk-id">#${(t.id||'').slice(-8).toUpperCase()}</span>
      </div>
      <div class="tk-body">
        <div class="tk-event">${esc(t.eventName||'—')}</div>
        <div class="tk-grid">
          <div class="tk-field"><label>Müşteri</label><span>${esc(t.customerName||'—')}</span></div>
          <div class="tk-field"><label>Koltuk</label><span>${esc(t.seat||'—')}</span></div>
          <div class="tk-field"><label>Fiyat</label><span class="tk-price-big">${t.price!=null?Number(t.price).toLocaleString('tr-TR')+' ₺':'—'}</span></div>
          <div class="tk-field"><label>Tarih</label><span>${fmtD(t.purchaseDate,true)}</span></div>
        </div>
        <div class="tk-barcode">${barcode(t.id||String(i))}</div>
      </div>`;
    grid.appendChild(div);
  });
  tPage=pg.page; tTotal=pg.totalPages;
  document.getElementById('pgInfo').textContent=`Sayfa ${pg.page} / ${pg.totalPages}`;
  document.getElementById('btnPrev').disabled=pg.page<=1;
  document.getElementById('btnNext').disabled=pg.page>=pg.totalPages;
  document.getElementById('pagination').classList.toggle('on',pg.totalPages>1);
}
// ── FETCH ─────────────────────────────────────────────────────────────────────
async function loadEvents(){
  setActiveNav('navEvents');
  document.getElementById('heroSection').style.display='none';
  showLoad('Etkinlikler yükleniyor...');
  try{
    const r=await fetch('/api/events',{headers:getAuthHeaders()});
    const d=await r.json();
    if (r.ok && Array.isArray(d?.data)) {
      cachedEvents = d.data.map(x => x.data).filter(Boolean);
      populateEventOptions();
    }
    r.ok?renderEvents(d):showEmpty('&#9888;',d.error||`HTTP ${r.status}`);
  }catch(e){showEmpty('&#9888;','Bağlantı hatası: '+e.message);}
}

async function loadTickets(page=1){
  setActiveNav('navTickets');
  document.getElementById('heroSection').style.display='none';
  if(!localStorage.getItem(TOKEN_KEY)){
    showEmpty('&#128274;','Biletlerinizi görmek için giriş yapın.');
    return;
  }
  showLoad('Biletler yükleniyor...');
  try{
    const r=await fetch(`/api/tickets?page=${page}&pageSize=10`,{headers:getBearerHeaders()});
    const d=await r.json();
    r.ok?renderTickets(d):showEmpty('&#9888;',d.error||`HTTP ${r.status}`);
  }catch(e){showEmpty('&#9888;','Bağlantı hatası: '+e.message);}
}

// ── SUBMIT ────────────────────────────────────────────────────────────────────
document.getElementById('ticketForm').addEventListener('submit',async e=>{
  e.preventDefault();
  if(!localStorage.getItem(TOKEN_KEY)){
    toast('Bilet satın almak için önce giriş yapın','err');
    openLogin();
    return;
  }
  const btn=document.getElementById('submitBtn');
  btn.disabled=true; btn.textContent='İşleniyor...';
  const payload={
    EventName:   document.getElementById('fEvent').value.trim(),
    CustomerName:document.getElementById('fCustomer').value.trim(),
    Seat:        document.getElementById('fSeat').value.trim(),
    Price:       parseFloat(document.getElementById('fPrice').value)||0,
    Status:      'Active',
    PurchaseDate:new Date().toISOString(),
  };
  try{
   const r=await fetch('/api/tickets',{
  method:'POST',
  headers:getBearerHeaders(true),
  body:JSON.stringify(payload),
});
    if(r.ok){
      closeModal();
      toast('&#127881; Bilet başarıyla oluşturuldu! İyi eğlenceler.','ok');
    } else {
      const d=await r.json().catch(()=>({}));
      toast(d.error||`Hata: HTTP ${r.status}`,'err');
    }
  }catch(err){toast('Bağlantı hatası: '+err.message,'err');}
  finally{btn.disabled=false;btn.textContent='Satın Al →';}
});

// ── PAGINATION ────────────────────────────────────────────────────────────────
document.getElementById('btnPrev').onclick=()=>{if(tPage>1)loadTickets(tPage-1);};
document.getElementById('btnNext').onclick=()=>{if(tPage<tTotal)loadTickets(tPage+1);};

// ── NAV BINDINGS ──────────────────────────────────────────────────────────────
document.getElementById('navEvents').onclick=loadEvents;
document.getElementById('navTickets').onclick=()=>loadTickets(1);
document.getElementById('heroBrowse').onclick=loadEvents;
document.getElementById('heroTickets').onclick=()=>loadTickets(1);
setAuthUi();
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

    if (lowerPath.Contains("auth"))
    {
        var authUrl = isDocker
            ? $"http://authservice:5002/{path}"
            : $"http://localhost:5002/{path}";
        return await ForwardRequest(context, authUrl, clientFactory);
    }

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
            var db = mongoClient.GetDatabase("DispatcherDb");
            var authCollection = db.GetCollection<BsonDocument>("ApiKeys");
            var filter = Builders<BsonDocument>.Filter.Eq("key", apiKey.ToString());
            var authRecord = await authCollection.Find(filter).FirstOrDefaultAsync();
            isAuthenticated = authRecord != null && authRecord["isActive"] != false;
        }
        catch
        {
            isAuthenticated = true;
        }
    }

    if (!isAuthenticated)
        return Results.Json(new { error = "Yetkisiz erişim! X-Api-Key veya Authorization: Bearer <token> gönder." }, statusCode: 401);

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

        var fullUrl = context.Request.QueryString.HasValue
            ? targetUrl + context.Request.QueryString
            : targetUrl;

        var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), fullUrl);

        if (context.Request.Headers.TryGetValue("Authorization", out var authHdr))
            request.Headers.TryAddWithoutValidation("Authorization", authHdr.ToString());
        if (context.Request.Headers.TryGetValue("X-Api-Key", out var apiHdr))
            request.Headers.TryAddWithoutValidation("X-Api-Key", apiHdr.ToString());

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
