# ECommerce Microservices

A hands-on .NET 10 gRPC microservices project — 3 services talking to each other, each with its own SQL Server database, running in Docker.

## What This Project Does

Imagine an online store backend split into 3 small services:

| Service | Port (HTTPS) | What It Does |
|---------|-------------|--------------|
| **ProductService** | `5001` | Manages products (name, price, category) |
| **InventoryService** | `5003` | Tracks stock quantities (reserve/release) |
| **OrderService** | `5002` | Creates orders, calls Product + Inventory to validate and reserve |

**Flow when a customer places an order:**

```
  Client
    │ CreateOrder(req)
    ▼
  OrderService ──► ProductService (check product exists & get price)
      │
      └──► InventoryService (reserve stock for each item)
                │
                ▼ success? ──► Save order to DB
                ▼ fail?     ──► Release already-reserved stock (rollback)
```

## Prerequisites

Install these before starting:

| Tool | Why |
|------|-----|
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | Runs SQL Server locally |
| [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | Builds and runs the C# code |
| [dotnet-grpc-cli](https://github.com/grpc/grpc/blob/master/doc/command_line_tool.md) (optional) | Test gRPC endpoints from terminal |

Check everything is installed:

```bash
docker --version
dotnet --version
```

---

## Step-by-Step Setup

### 1. Start SQL Server (Docker)

One command starts SQL Server 2022 on port `1433`:

```bash
docker compose up -d
```

Wait about 20 seconds, then verify it's ready:

```bash
docker compose exec sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "YourStrong!Passw0rd" -C \
  -Q "SELECT 1 AS ok"
```

If you see `ok` printed, you're good.

> **What's happening?** The `docker-compose.yml` file downloads the official SQL Server 2022 image and runs a container with the SA password `YourStrong!Passw0rd`. The databases (`ProductDb`, `InventoryDb`, `OrderDb`) are created automatically by each service when they first start via `EnsureCreated()`.

### 2. Build the Solution

```bash
dotnet build
```

This compiles all 4 projects. Zero warnings = ready to run.

### 3. Run Each Service

Open **3 separate terminals** and run each service:

```bash
# Terminal 1 — ProductService
dotnet run --project ProductService

# Terminal 2 — InventoryService
dotnet run --project InventoryService

# Terminal 3 — OrderService
dotnet run --project OrderService
```

Each terminal will show startup logs. You should see messages like:

```
Now listening on: https://localhost:5001
Application started.
```

> **Important:** The services must be started in this order:
> 1. ProductService (port 5001)
> 2. InventoryService (port 5003)
> 3. OrderService (port 5002) — depends on the other two via gRPC calls

---

## Project Structure Explained

```
ECommerceMicroservices/
├── docker-compose.yml                     # SQL Server config
├── ECommerceMicroservices.slnx            # Solution file (links projects)
├── ECommerce.Contracts/                   # Shared code library
│   ├── Protos/                            # gRPC contract definitions
│   │   ├── product.proto
│   │   ├── inventory.proto
│   │   └── order.proto
│   └── Interceptors/                      # Shared gRPC middleware
│       ├── DeadlinePropagationInterceptor.cs
│       └── RetryInterceptor.cs
├── ProductService/                        # Microservice #1
│   ├── Entities/Product.cs                # Product table model
│   ├── Context/ProductDbContext.cs         # Database connection + indexes
│   ├── Repositories/
│   │   ├── IProductRepository.cs          # Interface (contract for data access)
│   │   └── ProductRepository.cs           # Implementation (EF Core queries)
│   ├── Services/ProductServiceImpl.cs     # gRPC endpoint handlers
│   └── Program.cs                         # App startup (DI, gRPC, EF)
├── InventoryService/                      # Microservice #2
│   ├── Entities/InventoryItem.cs
│   ├── Context/InventoryDbContext.cs
│   ├── Repositories/
│   │   ├── IInventoryRepository.cs
│   │   └── InventoryRepository.cs
│   ├── Services/InventoryServiceImpl.cs
│   └── Program.cs
└── OrderService/                          # Microservice #3 (orchestrator)
    ├── Entities/Order.cs
    ├── Entities/OrderItem.cs
    ├── Context/OrderDbContext.cs
    ├── Repositories/
    │   ├── IOrderRepository.cs
    │   └── OrderRepository.cs
    ├── Services/OrderServiceImpl.cs
    └── Program.cs
```

### Why This Folder Layout?

| Folder | What Goes Here | Why |
|--------|---------------|-----|
| `Entities/` | Database table classes (POCOs) | Each file = one database table |
| `Context/` | DbContext with EF Core setup | Configures connections, indexes, relationships |
| `Repositories/` | Data access layer | `I*Repository.cs` = interface (contract), `*Repository.cs` = implementation. Keeps database code separate from business logic |
| `Services/` | gRPC endpoint handlers | Contains the actual request/response logic |

---

## Architecture Concepts (For Beginners)

### What is gRPC?

gRPC is a way for services to talk to each other. Instead of REST/JSON (text-based), gRPC uses Protocol Buffers (binary, faster). You define the API in a `.proto` file, and code is auto-generated.

**Example `.proto` snippet** (`ECommerce.Contracts/Protos/product.proto`):

```protobuf
service ProductService {
  rpc GetProduct (ProductRequest) returns (ProductResponse);
}

message ProductRequest { int32 id = 1; }
message ProductResponse {
  int32 id = 1;
  string name = 2;
  double price = 3;
}
```

When you build, the compiler generates:
- `ProductServiceBase` — abstract class you inherit from to implement the server
- `ProductServiceClient` — class your code uses to call the server

### Communication Flow

```
                   ┌──────────────┐
                   │ OrderService │
                   │   (port 5002)│
                   └──┬───┬───┬──┘
              gRPC call │   │   │
          ┌─────────────┘   │   └────────────┐
          ▼                 ▼                 ▼
  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐
  │ProductService│  │InventorySvc │  │  SQL Server  │
  │  (port 5001) │  │  (port 5003)│  │  (port 1433) │
  └──────────────┘  └──────────────┘  └──────────────┘
```

### What is a Repository?

A **Repository** is a pattern that wraps database access. Instead of writing SQL/EF queries directly in your service class, you call a method on an interface:

```csharp
// ❌ Without repository — service knows about EF Core
public class ProductServiceImpl(ProductDbContext db) {
    public async Task<ProductResponse> GetProduct(...) {
        var p = await db.Products.FindAsync(...);  // EF code in service
    }
}

// ✅ With repository — service only knows about the interface
public class ProductServiceImpl(IProductRepository repo) {
    public async Task<ProductResponse> GetProduct(...) {
        var p = await repo.GetByIdAsync(...);  // No EF here!
    }
}
```

**Benefits:**
- Easier to unit test (mock the interface)
- If you change the database library, you only change the repository
- Clear separation of concerns

### Database Indexes

Indexes make queries faster. Like a book's table of contents — instead of reading every page, you jump straight to the right one.

| Entity | Indexed Columns | Why |
|--------|----------------|-----|
| `Product` | `Name`, `Category` | Search/filter products |
| `InventoryItem` | `ProductId` (unique) | Fast stock lookup, one row per product |
| `Order` | `Status`, `CreatedAt` | Filter by status, sort by date |
| `OrderItem` | `OrderId`, `ProductId` | Look up items by order or product |

---

## Testing the APIs

### Option 1: Using `grpcurl` (terminal)

Install: `brew install grpcurl`

```bash
# List all products
grpcurl -plaintext localhost:5001 Product.ProductService/ListProducts

# Get a specific product
grpcurl -plaintext -d '{"id": 1}' localhost:5001 Product.ProductService/GetProduct

# Check stock
grpcurl -plaintext -d '{"product_id": 1}' localhost:5003 Inventory.InventoryService/CheckStock

# Create an order
grpcurl -plaintext -d '{"items": [{"product_id": 1, "quantity": 2}]}' \
  localhost:5002 Order.OrderService/CreateOrder
```

### Option 2: Using `dotnet-grpc-cli`

```bash
dotnet tool install -g Grpc.Tools

# List services
grpcui localhost:5001  # Opens a browser UI
```

---

## Common Issues & Fixes

| Symptom | Likely Cause | Fix |
|---------|-------------|-----|
| `Cannot connect to SQL Server` | Docker not started | Run `docker compose up -d` |
| `Connection refused` when calling another service | That service isn't running | Start all 3 services |
| `StatusCode.NotFound` for a product | Product ID doesn't exist | First add a product via `ListProducts` to see available IDs |
| Build error: `'Product' is a namespace but is used like a type` | Name collision with proto types | Use `Entities.Product` instead of just `Product` |
| `No process is on the other end of the pipe` | Rosetta emulation issue on Apple Silicon | Wait 30s for SQL to fully start |

---

## About Proto Files

All `.proto` files live in **one place**: `ECommerce.Contracts/Protos/`

Each service `.csproj` references them with `GrpcServices`:

| Service | References | Mode | Why |
|---------|-----------|------|-----|
| ProductService | `product.proto` | `Server` | Only hosts the service |
| InventoryService | `inventory.proto` | `Server` | Only hosts the service |
| OrderService | `order.proto` | `Server` | Hosts the order service |
| OrderService | `product.proto` | `Client` | Calls ProductService |
| OrderService | `inventory.proto` | `Client` | Calls InventoryService |

**No duplicate proto files** — the `.proto` files are the single source of truth.

---

## Database Schema

Each service has its own database on the same SQL Server instance:

| Database | Tables |
|----------|--------|
| `ProductDb` | `Products` (Id, Name, Description, Price, Category) |
| `InventoryDb` | `InventoryItems` (Id, ProductId, AvailableQuantity, ReservedQuantity) |
| `OrderDb` | `Orders` (Id, Status, CreatedAt, Total), `OrderItems` (Id, OrderId, ProductId, Quantity, UnitPrice) |

Databases are auto-created on first run via `EnsureCreated()`.

---

## Tech Stack

| Technology | Version | Purpose |
|-----------|---------|---------|
| .NET | 10.0 | Application framework |
| gRPC | 2.64.0 | Inter-service communication |
| Entity Framework Core | 10.0.8 | Database ORM |
| SQL Server | 2022 | Database |
| Docker | — | Container runtime |
