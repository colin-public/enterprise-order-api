# enterprise-order-api

A compact order management REST API built with C# and ASP.NET Core. It demonstrates customer and product management, order creation with inventory checks, SQL Server persistence, and automated tests.

## Technology

- C# and .NET 10
- ASP.NET Core Web API with controllers and dependency injection
- Entity Framework Core 10 with SQL Server
- SQL Server LocalDB for local development and integration testing
- Swagger / OpenAPI
- xUnit and ASP.NET Core integration testing with `WebApplicationFactory`

## Project Structure

```text
enterprise-order-api/
├── Contracts/                   # API request DTOs and validation
├── Controllers/                 # Customer, Product, and Order endpoints
├── Data/                        # ApplicationDbContext and EF Core mappings
├── Migrations/                  # SQL Server schema migrations
├── Models/                      # Customer, Product, Order, and OrderItem
├── Properties/                  # Local launch settings
├── Program.cs                   # Application startup and dependency injection
├── appsettings.json             # Local development connection string
├── enterprise-order-api.csproj
├── enterprise-order-api.slnx
└── README.md
```

The test project is located at `../EnterpriseOrderApi.Tests/` and is included in `enterprise-order-api.slnx`.

## API Endpoints

| Resource | Method | Endpoint | Purpose |
| --- | --- | --- | --- |
| Customer | GET | `/api/customers` | List customers |
| Customer | GET | `/api/customers/{id}` | Get a customer |
| Customer | POST | `/api/customers` | Create a customer |
| Customer | PUT | `/api/customers/{id}` | Update a customer |
| Customer | DELETE | `/api/customers/{id}` | Delete a customer |
| Product | GET | `/api/products` | List products |
| Product | GET | `/api/products/{id}` | Get a product |
| Product | POST | `/api/products` | Create a product |
| Product | PUT | `/api/products/{id}` | Update a product |
| Product | DELETE | `/api/products/{id}` | Delete a product |
| Order | GET | `/api/orders` | List orders and order items |
| Order | GET | `/api/orders/{id}` | Get an order and its order items |
| Order | POST | `/api/orders` | Create an order and deduct stock |

Requests use DataAnnotations validation and ASP.NET Core automatic API validation. The API returns appropriate `400 Bad Request`, `404 Not Found`, and `409 Conflict` responses, including ProblemDetails for resource and business conflicts. Customer email and Product SKU are unique; known SQL Server unique constraint violations are returned as conflicts.

Order creation runs in a database transaction. It aggregates requested quantities for repeated ProductIds, validates available stock, deducts stock with conditional database updates, and reads each order item's price from the database. Insufficient stock rolls back the transaction. Unit prices are not accepted from the client.

## How to Run

### 1. Prerequisites

- .NET 10 SDK
- SQL Server LocalDB (`MSSQLLocalDB`)
- Entity Framework Core CLI 10.0.12 (`dotnet-ef`)

Run the commands from the `enterprise-order-api` directory.

### 2. Restore packages

```powershell
dotnet restore
```

### 3. Configure the development database

The default `ConnectionStrings:DefaultConnection` in `appsettings.json` uses Windows Authentication with `(localdb)\MSSQLLocalDB` and the development database **`EnterpriseOrderDb`**. Change this setting only if your local SQL Server instance requires a different connection.

The integration test uses a separate database named **`EnterpriseOrderApiTestDb`**. It is configured inside the test project and must not be changed to or mixed with the development database.

### 4. Apply EF Core migrations

With LocalDB available, apply the existing migrations to `EnterpriseOrderDb`:

```powershell
dotnet ef database update
```

This applies the Customer, Product, Order, and OrderItem schema migrations. The integration test applies migrations to its dedicated test database when it runs.

### 5. Run the API

```powershell
dotnet run --launch-profile https
```

### 6. Open Swagger

With the included HTTPS launch profile, open:

```text
https://localhost:7065/swagger/index.html
```

### 7. Run tests

From the `enterprise-order-api` directory:

```powershell
dotnet test ..\EnterpriseOrderApi.Tests\EnterpriseOrderApi.Tests.csproj
```

## Example API Usage

Create a customer:

```http
POST /api/customers
Content-Type: application/json
```

```json
{
  "name": "Jordan Lee",
  "email": "jordan.lee@example.com",
  "phone": "0812345678"
}
```

Create an order for an existing customer and existing products. `productId` values must refer to products with sufficient stock. The server reads product prices from the database.

```http
POST /api/orders
Content-Type: application/json
```

```json
{
  "customerId": 1,
  "items": [
    {
      "productId": 1,
      "quantity": 2
    },
    {
      "productId": 2,
      "quantity": 1
    }
  ]
}
```

## Testing

The test project contains two xUnit unit tests for `CustomerRequest` validation and one ASP.NET Core integration test for `POST /api/customers`. The integration test uses `WebApplicationFactory` and SQL Server LocalDB with the dedicated `EnterpriseOrderApiTestDb` database.

Latest reported test result:

```text
Total: 3
Passed: 3
Failed: 0
Skipped: 0
```

## Future Improvements

- Add pagination to list endpoints.
- Strengthen concurrency handling for inventory updates.
- Add automated tests for Product and Order validation and business rules.
- Improve API response metadata and endpoint documentation.
