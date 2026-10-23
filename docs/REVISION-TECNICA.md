# Guía para la revisión técnica

## Repositorio

[Abrir el repositorio en GitHub](https://github.com/KeyVincent/MonolegalPruebaTecnica)

## Resumen

Aplicación de facturación compuesta por una API en .NET 8, MongoDB y un dashboard en React + TypeScript. Procesa las facturas en los estados `primerrecordatorio` y `segundorecordatorio`; el envío de correo es simulado y queda registrado en el log del backend.

## Arquitectura

```text
React + TypeScript
      │ HTTP / JSON
      ▼
WebApi (Minimal APIs, Swagger, CORS y Problem Details)
      │
      ▼
Application (casos de uso, DTOs e interfaces)
      ├────────────────┐
      ▼                ▼
Infrastructure       Domain
(MongoDB y correo)   (Factura)
      │
      ▼
MongoDB
```

| Proyecto/capa | Responsabilidad |
|---|---|
| `backend/Domain` | Entidad y conceptos propios del dominio. |
| `backend/Application` | Caso de uso de recordatorios, interfaces de repositorio/correo y DTOs de API. |
| `backend/Infrastructure` | Adaptador MongoDB, inicializador de datos y servicio de correo simulado. |
| `backend/WebApi` | Configuración del host, endpoints HTTP, Swagger, CORS, inyección de dependencias y manejo de errores. |
| `backend/Tests` | Pruebas unitarias de Application con xUnit y Moq. |
| `frontend/src` | Dashboard, componentes visuales y cliente HTTP tipado. |

## Decisiones técnicas

- Se separan la entidad de dominio, el documento de MongoDB y las respuestas HTTP para no exponer el modelo de persistencia desde la API.
- `IFacturaRepository`, `IFacturaService` e `IEmailService` permiten sustituir infraestructura sin acoplarla al caso de uso.
- `IMongoClient` es Singleton para reutilizar su pool de conexiones; repositorio y servicios son Scoped por solicitud.
- El flujo envía primero el correo simulado y solo después persiste el nuevo estado. Un fallo de envío queda informado y no actualiza esa factura.
- La seed identifica los registros de demostración por correo y hace upsert: al iniciar actualiza los tres registros de ejemplo sin duplicarlos.
- Los ejemplos se guardan en la base `FacturacionDemo`. La seed está activada en la configuración actual de demostración; debe desactivarse antes de conectar una base con datos reales.
- Swagger solo está habilitado en `Development`. El perfil `Facturacion.WebApi` establece ese entorno y utiliza el puerto `5000`.
- Las pruebas unitarias cubren las transiciones de estado y la condición de no actualizar si el correo falla.

## Requisitos para ejecutar la revisión

- Git.
- .NET SDK 8.
- Node.js 20 o superior y npm.
- MongoDB accesible en `mongodb://localhost:27017`.

## Pasos de despliegue local

### 1. Obtener el código

Desde PowerShell:

```powershell
git clone https://github.com/KeyVincent/MonolegalPruebaTecnica.git
Set-Location .\MonolegalPruebaTecnica
```

Si ya se tiene el código local, abrir una terminal en la raíz del repositorio.

### 2. Iniciar MongoDB

Arrancar una instancia local de MongoDB que acepte conexiones en `mongodb://localhost:27017`. La aplicación utiliza la base `FacturacionDemo` y la colección `Facturas`.

La configuración revisable está en `backend/WebApi/appsettings.json`. Para cambiar los valores localmente, se pueden establecer variables de entorno en la terminal donde se iniciará el backend:

```powershell
$env:MongoDb__ConnectionString = "mongodb://localhost:27017"
$env:MongoDb__DatabaseName = "FacturacionDemo"
$env:MongoDb__CollectionName = "Facturas"
$env:MongoDb__SeedSampleData = "true"
```

### 3. Iniciar la API y cargar la seed

Desde la raíz del repositorio:

```powershell
dotnet run --project .\backend\WebApi\Facturacion.WebApi.csproj --launch-profile Facturacion.WebApi
```

El perfil activa el entorno `Development` y escucha en `http://localhost:5000`. Con `SeedSampleData=true`, la API crea o restablece los tres registros ficticios al arrancar. Confirma la respuesta de la API antes de abrir el frontend:

```powershell
Invoke-RestMethod http://localhost:5000/api/facturas/resumen
```

### 4. Iniciar el frontend

En una **segunda** terminal:

```powershell
Set-Location .\MonolegalPruebaTecnica\frontend
npm install
npm run dev
```

Abrir `http://localhost:5173`.

### 5. Revisar API y flujo

- Dashboard: <http://localhost:5173>
- Swagger UI: <http://localhost:5000/swagger/index.html>
- OpenAPI JSON: <http://localhost:5000/swagger/v1/swagger.json>
- Resumen: `GET http://localhost:5000/api/facturas/resumen`
- Procesamiento: `POST http://localhost:5000/api/recordatorios/procesar`

También se pueden ejecutar las solicitudes de `backend/WebApi/Facturacion.http` desde la extensión REST Client. Si Swagger devuelve 404, detener instancias previas de la API con `Ctrl+C` y volver a iniciar con `--launch-profile Facturacion.WebApi`.

### 6. Ejecutar pruebas y compilaciones

Desde la raíz:

```powershell
dotnet test .\backend\Tests\Facturacion.Tests.csproj
dotnet build .\backend\WebApi\Facturacion.WebApi.csproj
Set-Location .\frontend
npm run build
```

## Configuración previa a un despliegue real

Esta guía prepara una revisión técnica local; no representa una URL de producción publicada. Antes de desplegar en un entorno compartido o productivo:

1. Configurar `MongoDb:ConnectionString` con el gestor de secretos del entorno y apuntar a la base correcta.
2. Desactivar la seed: `MongoDb__SeedSampleData=false`. La seed de demostración restablece los estados de sus tres facturas en cada inicio.
3. Restringir `Cors:AllowedOrigins` al dominio del frontend desplegado.
4. Desplegar backend y frontend con HTTPS y configurar `VITE_API_URL` con la URL pública de la API antes de compilar el frontend.
5. No exponer Swagger en producción salvo que se proteja y se habilite deliberadamente.
6. Sustituir el correo simulado por el proveedor autorizado solo si se requiere entregar correos reales.

## Alcance del correo

`IEmailService` está implementado como simulación: escribe destinatario, asunto y mensaje en los logs y devuelve éxito al caso de uso. Por eso se puede revisar la lógica de cambios de estado sin enviar correos reales. El flujo no entrega mensajes a clientes.
