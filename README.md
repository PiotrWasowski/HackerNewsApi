# HackerNews Best Stories API

## 📌 Description

This project is a sample **ASP.NET Core (.NET 8.0)** API that exposes an endpoint returning a list of the best stories from **Hacker News**.

The application is built in a **production-ready** style and demonstrates good backend engineering practices:

- async / await
- in-memory caching
- protection against *cache stampede*
- rate limiting
- retry + circuit breaker (Polly)
- unit and integration tests

The project can be treated as a **recruitment coding task (mid/senior backend .NET)**.

## ▶️ Running the project

```bash
dotnet run
```

Swagger UI:

```
https://localhost:7213/swagger
```

---

---

## 🏗️ Architecture

```
HackerNewsApi/
│
├── Controllers/          # API endpoints
├── Services/             # Business logic
├── Clients/              # Hacker News API client
├── Models/               # External models
├── Dtos/                 # API response DTOs
├── Program.cs            # Application configuration
└── appsettings.json
```

Layers are separated according to **SOLID / GRASP** principles.

---

## 🚀 Endpoint

```
GET /api/beststories?n=10
```

### Parameters

| Name | Type | Description                              |
| ---- | ---- | ---------------------------------------- |
| n    | int  | Number of stories to return (max 100)   |

### Sample response

```json
[
  {
    "title": "Example story",
    "uri": "https://example.com",
    "postedBy": "author",
    "time": "2024-01-01T12:00:00Z",
    "score": 350,
    "commentCount": 42
  }
]
```

---

## ⚙️ Key technical decisions

### Cache

- `IMemoryCache`
- TTL: 2 minutes
- cache stores the **full list of best stories**, not a single request result
- **lock (SemaphoreSlim)** used to protect against *cache stampede*

### Rate limiting

- built-in **ASP.NET Core Rate Limiter**
- per-IP policy
- Sliding Window algorithm
- HTTP 429 returned when the limit is exceeded

### Resiliency (Polly)

- Retry with exponential backoff
- Circuit Breaker
- policies attached to `HttpClient`

---

## 🧪 Tests

### Unit tests (NUnit)

- `BestStoriesService`
- cache logic
- sorting
- handling of the `n` parameter
- protection against *cache stampede*

### Integration tests

- `WebApplicationFactory`
- full ASP.NET Core pipeline
- cache behavior
- rate limiting (429)
- mocked external Hacker News API

Run tests:

```bash
dotnet test
```

---

## 🧠 Assumptions and simplifications

- no authentication (public API)
- in-memory cache (production → Redis)
- no persistence (stateless API)

---

## 🔮 Possible production improvements

- Redis + distributed lock
- background cache refresh
- metrics (Prometheus / OpenTelemetry)
- health checks
- feature flags

---

## 👤 Author

Project prepared as an example solution for a backend (.NET) recruitment task.

---

## 📄 License

MIT (educational purposes)
