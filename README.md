# Products API + Datadog (OpenTelemetry)

CRUD de productos en **.NET 8** (minimal API + EF Core + PostgreSQL) instrumentado con **OpenTelemetry**.
Trazas, métricas y logs se envían por **OTLP** al **Datadog Agent**, que los reenvía a Datadog.

```
products-api (dotnet run) ──OTLP gRPC :4317──► datadog-agent ──► Datadog (datadoghq.com)
        │                                           ▲
        └──── Npgsql :5432 ────► postgres ──────────┘ (check de Postgres vía autodiscovery)
```

## Requisitos

- .NET SDK 8 (fijado en `global.json`)
- Docker Desktop
- Una API key de Datadog (*Organization Settings → API Keys*)

## Puesta en marcha

1. Crea el archivo `.env` a partir del ejemplo y pon tu `DD_API_KEY`:

   ```bash
   cp .env.example .env
   ```

2. Levanta el Datadog Agent y PostgreSQL:

   ```bash
   docker compose up -d
   ```

3. Comprueba que el Agent recibe OTLP (sección **OTLP** del status):

   ```bash
   docker exec dd-agent agent status
   ```

4. Ejecuta la API (aplica las migraciones automáticamente en Development):

   ```bash
   dotnet run --project src/Products.Api
   ```

   Swagger: http://localhost:5161/swagger — o usa `src/Products.Api/Products.Api.http`.

## Endpoints

| Método | Ruta                  | Descripción          |
|--------|-----------------------|----------------------|
| GET    | `/api/products`       | Lista productos      |
| GET    | `/api/products/{id}`  | Obtiene un producto  |
| POST   | `/api/products`       | Crea un producto     |
| PUT    | `/api/products/{id}`  | Actualiza un producto|
| DELETE | `/api/products/{id}`  | Elimina un producto  |
| GET    | `/health`             | Health check (no se traza) |

## Qué se ve en Datadog

- **APM → Services → `products-api`** (`env:development`, `version:1.0.0`): trazas HTTP con spans hijos de PostgreSQL (Npgsql) y el span propio `product.create`.
- **Metrics Explorer**: métricas de ASP.NET Core, runtime de .NET, Npgsql y las custom `products.created`, `products.updated`, `products.deleted`, `products.price`.
- **Logs**: logs de `ILogger` con `service:products-api`, enlazados a su traza.
- **Integración Postgres**: métricas del servidor (`postgresql.*`) recogidas por el Agent con el usuario `datadog`.

## Configuración

| Variable / clave                  | Por defecto              | Uso |
|-----------------------------------|--------------------------|-----|
| `OTEL_EXPORTER_OTLP_ENDPOINT`     | `http://localhost:4317`  | Dónde está el receptor OTLP del Agent |
| `OpenTelemetry:ServiceName`       | `products-api`           | Tag `service` |
| `OpenTelemetry:ServiceVersion`    | `1.0.0`                  | Tag `version` |
| `ASPNETCORE_ENVIRONMENT`          | `Development`            | Tag `env` (en minúsculas) |
| `ConnectionStrings__Default`      | ver `appsettings.Development.json` | Conexión a PostgreSQL |

> Si el puerto 5432 ya está ocupado, define `POSTGRES_PORT` en `.env` (p. ej. `5434`) y ejecuta la API con
> `ConnectionStrings__Default="Host=localhost;Port=5434;Database=products;Username=products;Password=products"`.
>
> El script `docker/postgres/init-datadog.sh` solo corre la primera vez que se crea el volumen.
> Para reiniciar la base de datos desde cero: `docker compose down -v`.

## CI/CD

Workflow: [`.github/workflows/ci-cd.yml`](.github/workflows/ci-cd.yml) (GitHub Actions).

```
push / PR ──► build-test (restore, build, test) ──► publish (solo push a main)
                                                      └─► ghcr.io/<owner>/<repo>/products-api:latest y :<sha>
```

- **CI**: en cada push y pull request a `main` compila y ejecuta los tests (`tests/Products.Api.Tests`).
- **CD**: si estás en `main` y el CI pasó, construye la imagen (`src/Products.Api/Dockerfile`) y la publica en GitHub Container Registry con `GITHUB_TOKEN` (no requiere secretos).
- Probar local: `docker build -f src/Products.Api/Dockerfile -t products-api .`
