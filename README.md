# Sistema de facturación y recordatorios

Aplicación full-stack para consultar facturas en MongoDB, procesar recordatorios de pago y ver sus estados en un dashboard React. El backend está construido con .NET 8 y Minimal APIs; el correo está **simulado** y no envía mensajes reales.

## Revisión técnica

- Arquitectura, decisiones y pasos de despliegue: [Guía de revisión técnica](./docs/REVISION-TECNICA.md).

## Arquitectura

```text
React + TypeScript + CSS
        │ Fetch / HTTP / JSON
        ▼
WebApi (.NET 8: endpoints, CORS, Swagger, manejo de errores)
        │
        ▼
Application (caso de uso, interfaces y DTOs)
        ├──────────────────┐
        ▼                  ▼
Infrastructure           Domain
(MongoDB y correo mock)   (Factura)
        │
        ▼
MongoDB: FacturacionDemo.Facturas
```

### Estructura principal

```text
backend/
├── Domain/           Entidad Factura
├── Application/      Servicios, interfaces y DTOs
├── Infrastructure/
│   ├── External/     Implementación simulada del correo
│   └── Persistence/  Repositorio y seed de MongoDB
├── WebApi/           API, endpoints y configuración
└── Tests/            Pruebas unitarias xUnit + Moq
frontend/
└── src/
    ├── components/   Tarjetas de resumen y estado de factura
    ├── services/     Cliente HTTP y tipos de TypeScript
    └── App.tsx       Dashboard y tabla de facturas
```

### Servicios y modelos

- `Factura` representa la factura en el dominio; `FacturaDocument` mapea el documento MongoDB; la API devuelve DTOs de respuesta, no expone el documento de persistencia.
- `IFacturaRepository` y `IFacturaService` separan las reglas de negocio de MongoDB y HTTP. `IEmailService` permite reemplazar el correo mock sin cambiar el caso de uso.
- `IMongoClient` se registra como **Singleton**, adecuado para reutilizar el cliente/thread pool de MongoDB. Repositorio, caso de uso, servicio de correo y seed se registran como **Scoped**.
- Las respuestas de errores no controlados usan Problem Details.

## Flujo de recordatorios

`POST /api/recordatorios/procesar` obtiene las facturas cuyo estado sea `primerrecordatorio` o `segundorecordatorio`. Para cada factura:

1. Invoca el servicio de correo desacoplado mediante `IEmailService`.
2. Si el envío finaliza correctamente, actualiza el documento en MongoDB:
   - `primerrecordatorio` → `segundorecordatorio`.
   - `segundorecordatorio` → `desactivado`.
3. Si el correo falla, registra y devuelve el error; no actualiza el estado de esa factura y continúa con las siguientes.

La implementación actual ([EmailService.cs](./backend/Infrastructure/External/EmailService.cs)) solo registra destinatario, asunto y mensaje en los logs. Para enviar correos reales, hay que sustituirla por un adaptador SMTP o proveedor y configurar sus credenciales mediante secretos o variables de entorno. El envío y la escritura en MongoDB no son una transacción distribuida; para producción conviene incorporar idempotencia/outbox.

## Seed de datos de demostración

El inicializador [FacturaSeeder.cs](./backend/Infrastructure/Persistence/FacturaSeeder.cs) se ejecuta al iniciar la API cuando `MongoDb:SeedSampleData` está habilitado. La configuración de desarrollo ya lo activa en [appsettings.json](./backend/WebApi/appsettings.json). Crea o actualiza por correo tres registros ficticios:

| Cliente | Correo | Monto | Estado inicial |
|---|---|---:|---|
| Ana García | `ana@example.com` | 125.50 | `primerrecordatorio` |
| Luis Martínez | `luis@example.com` | 89.99 | `segundorecordatorio` |
| María López | `maria@example.com` | 210.00 | `primerrecordatorio` |

Los correos usan el dominio reservado `example.com`. La seed no borra otros documentos de la colección; al reiniciar, restablece los campos de estos tres registros a sus valores iniciales. Los identifica por correo para no duplicarlos.

Los datos se guardan por defecto en la base **`FacturacionDemo`**, colección **`Facturas`**, separada de bases reales. Antes de usar una base de producción, cambia la configuración y desactiva la seed.

## Requisitos

- .NET SDK 8.
- Node.js 20 o superior y npm.
- MongoDB local en `mongodb://localhost:27017` o una instancia accesible.

## Configuración

Configuración de desarrollo en `backend/WebApi/appsettings.json`:

```json
{
  "MongoDb": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "FacturacionDemo",
    "CollectionName": "Facturas",
    "SeedSampleData": true
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173"]
  }
}
```

Los documentos MongoDB tienen `_id`, `nombreCliente`, `correoCliente`, `monto` y `estado`. Los estados son texto. Las variables de entorno de .NET pueden reemplazar los valores del archivo; se usa doble guion bajo para separar secciones:

```powershell
$env:MongoDb__ConnectionString = "mongodb://localhost:27017"
$env:MongoDb__DatabaseName = "FacturacionDemo"
$env:MongoDb__CollectionName = "Facturas"
$env:MongoDb__SeedSampleData = "true"
```

El frontend utiliza `http://localhost:5000` por defecto. Para cambiar la dirección de la API, crea `frontend/.env.local`:

```dotenv
VITE_API_URL=http://localhost:5000
```

## Ejecutar la aplicación

Confirma primero que MongoDB esté iniciado. Desde la raíz del proyecto, abre una terminal y ejecuta:

```powershell
Set-Location C:\Users\steve\Desktop\FacturacionSystem
dotnet run --project .\backend\WebApi\Facturacion.WebApi.csproj --launch-profile Facturacion.WebApi
```

El perfil [launchSettings.json](./backend/WebApi/Properties/launchSettings.json) establece `Development`, usa el puerto `5000` y abre Swagger. Al arrancar, la API carga la seed en `FacturacionDemo` si está habilitada.

Abre una segunda terminal para iniciar el frontend:

```powershell
Set-Location C:\Users\steve\Desktop\FacturacionSystem\frontend
npm install
npm run dev
```

URLs locales:

- Dashboard React: <http://localhost:5173>
- Swagger UI: <http://localhost:5000/swagger/index.html>
- Documento OpenAPI: <http://localhost:5000/swagger/v1/swagger.json>
- Resumen de facturas: <http://localhost:5000/api/facturas/resumen>

Si aparece un `404` en Swagger, detén con `Ctrl+C` la instancia anterior de la API y vuelve a iniciar con el perfil `Facturacion.WebApi` indicado arriba. Swagger solo se habilita para el entorno `Development`.

## API

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/facturas/resumen` | Devuelve todas las facturas y el conteo por estado para el dashboard. |
| `POST` | `/api/recordatorios/procesar` | Procesa los recordatorios pendientes y devuelve el resultado y los errores por factura. |

Ejemplo para probar manualmente:

```powershell
Invoke-RestMethod http://localhost:5000/api/facturas/resumen
Invoke-RestMethod -Method Post http://localhost:5000/api/recordatorios/procesar
```

También se incluyen peticiones para la extensión REST Client en [Facturacion.http](./backend/WebApi/Facturacion.http).

## Dashboard

La aplicación React + TypeScript en `frontend/src` consulta el resumen a la API y presenta:

- Total de facturas y cantidad en primer recordatorio, segundo recordatorio y desactivadas.
- Tabla con cliente, correo, monto y estado actual.
- Búsqueda por cliente/correo y filtro por estado.
- Botón para procesar recordatorios y actualizar el resumen después del proceso.
- Indicadores de carga, errores y disponibilidad de la API.

El diseño está implementado con CSS en [styles.css](./frontend/src/styles.css), sin dependencia de Tailwind.

## Pruebas y compilación

Desde la raíz del proyecto:

```powershell
dotnet test .\backend\Tests\Facturacion.Tests.csproj
dotnet build .\backend\WebApi\Facturacion.WebApi.csproj
Set-Location .\frontend
npm run build
```

Las pruebas unitarias usan xUnit y Moq para comprobar las transiciones de estado y que una falla del correo no actualiza la factura.
