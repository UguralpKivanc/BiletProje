# Bilet Sistemi - Mikroservis Mimarisi

.NET 10 tabanlı, Docker Compose ile orkestre edilen, MongoDB destekli bilet satış sistemi.

---

## Mimari Genel Görünüm

```mermaid
graph TB
    subgraph Internet["Dış Dünya"]
        Client["İstemci (Browser / k6 / Postman)"]
    end

    subgraph frontend["frontend_network (Herkese Açık)"]
        Dispatcher["Dispatcher\nAPI Gateway\n:5000"]
        Grafana["Grafana\nDashboard\n:3000"]
    end

    subgraph backend["backend_network (İzole - Internal)"]
        AuthService["AuthService\nJWT Login\n:5002"]
        EventService["EventService\nEtkinlikler\n:5001"]
        TicketService["TicketService\nBiletler\n:5168"]
        Prometheus["Prometheus\nMetrik Toplama\n:9090"]
        MongoDB[("MongoDB\nBiletSistemiDb\n:27017")]
    end

    Client -->|"HTTP :5000"| Dispatcher
    Client -->|"HTTP :3000"| Grafana

    Dispatcher --> AuthService
    Dispatcher --> EventService
    Dispatcher --> TicketService

    AuthService --> MongoDB
    EventService --> MongoDB
    TicketService --> MongoDB

    Prometheus -->|scrape /metrics| Dispatcher
    Prometheus -->|scrape /metrics| EventService
    Prometheus -->|scrape /metrics| TicketService
    Prometheus -->|scrape /metrics| AuthService

    Grafana -->|PromQL| Prometheus
```

---

## İstek Akışı (Sequence Diagram)

```mermaid
sequenceDiagram
    actor Client as İstemci
    participant D as Dispatcher
    participant A as AuthService
    participant E as EventService
    participant T as TicketService
    participant DB as MongoDB

    Note over Client,DB: 1. Login Akışı
    Client->>D: POST /api/auth/login
    D->>A: İlet → POST /api/auth/login
    A->>DB: Users koleksiyonunda sorgula
    DB-->>A: Kullanıcı döndür
    A-->>D: JWT Token
    D-->>Client: { token: "eyJ..." }

    Note over Client,DB: 2. Korumalı Endpoint Akışı
    Client->>D: GET /api/events\n[Authorization: Bearer <token>]
    D->>D: JWT doğrula
    D->>E: İlet → GET /api/events
    E->>DB: Events koleksiyonu
    DB-->>E: Etkinlik listesi
    E-->>D: 200 OK + JSON
    D-->>Client: 200 OK + JSON

    Note over Client,DB: 3. Yetkisiz Erişim
    Client->>D: GET /api/tickets\n[Header yok]
    D-->>Client: 401 Unauthorized
```

---

## Veritabanı Şeması

```mermaid
erDiagram
    EVENTS {
        ObjectId _id PK
        string Name
        string Location
        datetime Date
        decimal Price
    }

    TICKETS {
        ObjectId _id PK
        string EventName
        string CustomerName
        string Seat
        string Status
        decimal Price
        datetime PurchaseDate
    }

    USERS {
        ObjectId _id PK
        string Username
        string PasswordHash
        string Role
    }

    APIKEYS {
        ObjectId _id PK
        string key
        bool isActive
    }
```

---

## SOLID / OOP Katman Diyagramı

```mermaid
classDiagram
    class IEventRepository {
        <<interface>>
        +GetAllAsync() Task~List~Event~~
        +GetByIdAsync(id) Task~Event~
        +CreateAsync(evt) Task~Event~
        +UpdateAsync(id, evt) Task
        +DeleteAsync(id) Task
    }

    class EventRepository {
        -IMongoCollection~Event~ _collection
        +GetAllAsync() Task~List~Event~~
        +GetByIdAsync(id) Task~Event~
        +CreateAsync(evt) Task~Event~
        +UpdateAsync(id, evt) Task
        +DeleteAsync(id) Task
    }

    class EventsController {
        -IEventRepository _repository
        +Get() IActionResult
        +GetById(id) IActionResult
        +Post(evt) IActionResult
        +Put(id, evt) IActionResult
        +Delete(id) IActionResult
    }

    class ITicketRepository {
        <<interface>>
        +GetAllAsync() Task~List~Ticket~~
        +GetByIdAsync(id) Task~Ticket~
        +CreateAsync(ticket) Task~Ticket~
        +UpdateAsync(id, ticket) Task
        +DeleteAsync(id) Task
    }

    class TicketRepository {
        -IMongoCollection~Ticket~ _collection
        +GetAllAsync() Task~List~Ticket~~
        +GetByIdAsync(id) Task~Ticket~
        +CreateAsync(ticket) Task~Ticket~
        +UpdateAsync(id, ticket) Task
        +DeleteAsync(id) Task
    }

    class TicketsController {
        -ITicketRepository _repository
        +Get() IActionResult
        +GetById(id) IActionResult
        +Post(ticket) IActionResult
        +Put(id, ticket) IActionResult
        +Delete(id) IActionResult
    }

    IEventRepository <|.. EventRepository : implements
    EventsController --> IEventRepository : depends on
    ITicketRepository <|.. TicketRepository : implements
    TicketsController --> ITicketRepository : depends on
```

---

## API Referansı

### Dispatcher (Port 5000) - Tüm İstekler Buradan

| Method | Endpoint | Auth | Açıklama |
|--------|----------|------|----------|
| POST | `/api/auth/login` | Yok | JWT token al |
| POST | `/api/auth/validate` | Yok | Token doğrula |
| GET | `/api/events` | Gerekli | Tüm etkinlikler |
| GET | `/api/events/{id}` | Gerekli | Etkinlik detayı |
| POST | `/api/events` | Gerekli | Etkinlik oluştur |
| PUT | `/api/events/{id}` | Gerekli | Etkinlik güncelle |
| DELETE | `/api/events/{id}` | Gerekli | Etkinlik sil |
| GET | `/api/tickets` | Gerekli | Tüm biletler |
| GET | `/api/tickets/{id}` | Gerekli | Bilet detayı |
| POST | `/api/tickets` | Gerekli | Bilet oluştur |
| PUT | `/api/tickets/{id}` | Gerekli | Bilet güncelle |
| DELETE | `/api/tickets/{id}` | Gerekli | Bilet sil |

**Auth header seçenekleri:**
- `X-Api-Key: KingoSifre123`
- `Authorization: Bearer <jwt_token>`

---

## Kurulum ve Çalıştırma

```bash
# 1. Tüm servisleri başlat
docker-compose up --build

# 2. Servislere eriş
# Ana sayfa:   http://localhost:5000
# Grafana:     http://localhost:3000  (admin / bilet2026)

# 3. Token al
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin123!"}'

# 4. Etkinlikleri listele (X-Api-Key ile)
curl http://localhost:5000/api/events \
  -H "X-Api-Key: KingoSifre123"

# 5. k6 yük testi çalıştır
k6 run k6/load-test.js
```

---

## Servis Portları (Docker İçi)

| Servis | Port | Dışa Açık | Açıklama |
|--------|------|-----------|----------|
| Dispatcher | 5000 | Evet | API Gateway |
| AuthService | 5002 | Hayır | JWT Login |
| EventService | 5001 | Hayır | Etkinlik CRUD |
| TicketService | 5168 | Hayır | Bilet CRUD |
| MongoDB | 27017 | Hayır | Veritabanı |
| Prometheus | 9090 | Hayır | Metrik toplama |
| Grafana | 3000 | Evet | Dashboard |
