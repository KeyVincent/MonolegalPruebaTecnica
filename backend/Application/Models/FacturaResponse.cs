namespace FacturacionApi.Application.Models;

public sealed record FacturaResponse(
    string Id,
    string NombreCliente,
    string CorreoCliente,
    decimal Monto,
    string Estado);
