using FacturacionApi.Application.Interfaces;
using FacturacionApi.Application.Models;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FacturacionApi.WebApi.Endpoints;

public static class FacturaEndpoints
{
    public static IEndpointRouteBuilder MapFacturaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/facturas").WithTags("Facturas");

        group.MapGet("/resumen", async Task<Ok<ResumenFacturasResponse>> (
                IFacturaService service,
                CancellationToken cancellationToken) =>
                TypedResults.Ok(await service.ObtenerResumenAsync(cancellationToken)))
            .WithName("ObtenerResumenFacturas")
            .WithSummary("Lista las facturas y su distribución por estado.")
            .WithDescription("Devuelve todas las facturas para el dashboard.")
            .Produces<ResumenFacturasResponse>();

        endpoints.MapPost("/api/recordatorios/procesar", async Task<Ok<ProcesamientoRecordatoriosResponse>> (
                IFacturaService service,
                CancellationToken cancellationToken) =>
                TypedResults.Ok(await service.ProcesarRecordatoriosAsync(cancellationToken)))
            .WithTags("Recordatorios")
            .WithName("ProcesarRecordatorios")
            .WithSummary("Envía los recordatorios pendientes y actualiza sus estados.")
            .WithDescription("El estado solo cambia después de que el envío simulado del correo finaliza correctamente.")
            .Produces<ProcesamientoRecordatoriosResponse>();

        return endpoints;
    }
}
