# RentaKey — Etapa 3 (Frontend MVC)

## Contenido
- `RentalAPI.Web/` → proyecto ASP.NET Core MVC + Razor Pages (.NET 10), consume el backend existente vía HttpClient.
- `docker-compose.yml` → stack completo (postgres, rabbitmq, api, worker, **web**).

## Ejecutar con Docker
1. Copia la carpeta `RentalAPI.Web/` y este `docker-compose.yml` dentro de la raíz donde está tu backend (`RentalAPI/`), de modo que `./RentalAPI` y `./RentalAPI.Web` queden como hermanos del compose.
2. `docker compose up --build`
3. Web: http://localhost:8081 · API/Swagger: http://localhost:8080/swagger

## Ejecutar local (sin Docker)
```
cd RentalAPI.Web
dotnet run
```
Ajusta `ApiSettings:BaseUrl` en `appsettings.json` si tu API corre en otro puerto (por defecto `http://localhost:8080`).

## Notas técnicas
- Autenticación: cookie de sesión en el MVC que almacena el JWT emitido por `/api/auth/login`; un `DelegatingHandler` (`AuthHeaderHandler`) lo inyecta como `Bearer` en cada llamada a la API.
- Roles: `[Authorize(Roles="...")]` espejando las reglas del backend (Owner/Admin para propiedades y dashboard, Admin para reportes).
- El endpoint `GET /api/properties` no expone `OwnerId`; `PropertyService.GetMyPropertiesAsync` resuelve "mis propiedades" cruzando el listado con el detalle de cada propiedad.
- El Dashboard de propietario muestra Ingresos/Ocupación/Reservas en 0 porque la API actual no expone un endpoint agregado por propietario (solo `GET /api/reservations` del usuario autenticado como huésped). Estructura lista para conectar cuando exista.
- KYC: la API no expone un score de confianza numérico; se muestra un nivel cualitativo derivado del `Status`.
