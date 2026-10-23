using FacturacionApi.Application.Interfaces;
using FacturacionApi.Application.Models;
using Microsoft.Extensions.Logging;

namespace FacturacionApi.Application.Services;

public sealed class FacturaService(
    IFacturaRepository facturaRepository,
    IEmailService emailService,
    ILogger<FacturaService> logger) : IFacturaService
{
    public async Task<ResumenFacturasResponse> ObtenerResumenAsync(CancellationToken cancellationToken)
    {
        var facturas = await facturaRepository.GetAllAsync(cancellationToken);
        var respuestas = facturas
            .Select(factura => new FacturaResponse(
                factura.Id,
                factura.NombreCliente,
                factura.CorreoCliente,
                factura.Monto,
                factura.Estado))
            .ToArray();

        return new ResumenFacturasResponse(
            respuestas.Length,
            Contar(respuestas, "primerrecordatorio"),
            Contar(respuestas, "segundorecordatorio"),
            Contar(respuestas, "desactivado"),
            respuestas.Count(factura => !EsEstadoProcesable(factura.Estado) &&
                                         !string.Equals(factura.Estado, "desactivado", StringComparison.OrdinalIgnoreCase)),
            respuestas);
    }

    public async Task<ProcesamientoRecordatoriosResponse> ProcesarRecordatoriosAsync(
        CancellationToken cancellationToken)
    {
        var facturas = await facturaRepository.GetPendientesDeRecordatorioAsync(cancellationToken);
        var procesadas = 0;
        var errores = new List<string>();

        foreach (var factura in facturas)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (nuevoEstado, asunto, mensaje) = factura.Estado.ToLowerInvariant() switch
            {
                "primerrecordatorio" => (
                    "segundorecordatorio",
                    "Segundo recordatorio de pago",
                    $"Estimado/a {factura.NombreCliente}, su factura ha pasado a segundo recordatorio."),
                "segundorecordatorio" => (
                    "desactivado",
                    "Aviso de desactivación de cuenta",
                    $"Estimado/a {factura.NombreCliente}, su cuenta va a ser desactivada por falta de pago."),
                _ => throw new InvalidOperationException($"Estado de recordatorio no esperado para la factura {factura.Id}.")
            };

            try
            {
                await emailService.EnviarCorreoAsync(
                    factura.CorreoCliente, asunto, mensaje, cancellationToken);
                await facturaRepository.UpdateEstadoAsync(factura.Id, nuevoEstado, cancellationToken);
                procesadas++;
                logger.LogInformation(
                    "Factura {FacturaId} procesada: estado {EstadoAnterior} -> {EstadoNuevo}.",
                    factura.Id,
                    factura.Estado,
                    nuevoEstado);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                errores.Add($"Factura {factura.Id}: {exception.Message}");
                logger.LogError(exception, "No se pudo procesar el recordatorio de la factura {FacturaId}.", factura.Id);
            }
        }

        return new ProcesamientoRecordatoriosResponse(procesadas, errores.Count, errores);
    }

    private static int Contar(IEnumerable<FacturaResponse> facturas, string estado) =>
        facturas.Count(factura => string.Equals(factura.Estado, estado, StringComparison.OrdinalIgnoreCase));

    private static bool EsEstadoProcesable(string estado) =>
        string.Equals(estado, "primerrecordatorio", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(estado, "segundorecordatorio", StringComparison.OrdinalIgnoreCase);
}
