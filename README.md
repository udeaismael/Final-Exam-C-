# RentalAPI — Etapas 1 & 2

## Quickstart

```bash
# 1. Levantar infraestructura
cd docker
docker compose up -d

# 2. Generar migración inicial (primera vez)
cd ../src/RentalAPI.Api
dotnet ef migrations add InitialCreate --project ../RentalAPI.Infrastructure -- --environment Development
dotnet ef database update -- --environment Development

# 3. Correr en local
dotnet run --project src/RentalAPI.Api

# 4. Correr Worker
dotnet run --project src/RentalAPI.Worker
```

Swagger disponible en: http://localhost:8080/swagger

## Migración EF Core (comando exacto)

```bash
dotnet ef migrations add <NombreMigration> \
  --project src/RentalAPI.Infrastructure \
  --startup-project src/RentalAPI.Api \
  --output-dir Data/Migrations
```

## Variables de entorno necesarias

| Variable | Ejemplo |
|---|---|
| `ConnectionStrings__Default` | `Host=localhost;...` |
| `Jwt__Key` | mínimo 32 chars |
| `Jwt__Issuer` | `RentalAPI` |
| `Jwt__Audience` | `RentalClient` |
| `RabbitMQ__Host` | `localhost` |
| `Azure__DocumentIntelligence__Endpoint` | (opcional) |
| `Azure__DocumentIntelligence__ApiKey` | (opcional) |

## Flujo KYC

```
POST /api/kyc/upload (multipart/form-data, field: file)
  → guarda imagen temporal
  → publica en cola kyc-processing
  → KycConsumer procesa OCR (Azure o Tesseract)
  → actualiza KycValidation.Status = Approved | Rejected
  → elimina imagen de forma segura (overwrite + delete)

GET /api/kyc/status → consulta estado actual
```

## Validación solapamiento de reservas (EF Core → SQL)

```csharp
// En ReservationsController.Create:
var overlap = await db.Reservations.AnyAsync(r =>
    r.PropertyId == req.PropertyId &&
    r.Status != "Cancelled"        &&
    r.CheckIn  < checkOut          &&   // existente empieza antes de que el nuevo termine
    r.CheckOut > checkIn);              // existente termina después de que el nuevo empieza
```

SQL generado equivalente:
```sql
SELECT EXISTS (
  SELECT 1 FROM "Reservations"
  WHERE "PropertyId" = @propertyId
    AND "Status" <> 'Cancelled'
    AND "CheckIn"  < @checkOut
    AND "CheckOut" > @checkIn
);
```

## Estructura de carpetas

```
RentalAPI/
├── docker/
│   └── docker-compose.yml
├── src/
│   ├── RentalAPI.Api/
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs
│   │   │   ├── PropertiesController.cs
│   │   │   ├── ReservationsController.cs
│   │   │   ├── WishlistController.cs
│   │   │   ├── ReportsController.cs
│   │   │   └── KycController.cs
│   │   ├── Program.cs
│   │   ├── appsettings.Development.json
│   │   └── Dockerfile
│   ├── RentalAPI.Application/       ← espacio para features/handlers si crece
│   ├── RentalAPI.Domain/
│   │   └── Entities/
│   │       ├── User.cs
│   │       ├── Property.cs
│   │       ├── Reservation.cs
│   │       ├── WishlistItem.cs
│   │       ├── KycValidation.cs
│   │       └── Notification.cs
│   ├── RentalAPI.Infrastructure/
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs
│   │   │   └── Migrations/         ← generado por EF
│   │   ├── Messaging/
│   │   │   └── RabbitMqPublisher.cs
│   │   └── Services/
│   │       ├── JwtService.cs
│   │       └── KycOcrService.cs
│   └── RentalAPI.Worker/
│       ├── Consumers/
│       │   ├── NotificationConsumer.cs
│       │   ├── KycConsumer.cs
│       │   └── ReportConsumer.cs
│       ├── Program.cs
│       └── Dockerfile
└── RentalAPI.sln
```
