namespace FacturacionApi.Application.Models;

public sealed record ResumenFacturasResponse(
    int Total,
    int PrimerRecordatorio,
    int SegundoRecordatorio,
    int Desactivadas,
    int OtrosEstados,
    IReadOnlyList<FacturaResponse> Facturas);
