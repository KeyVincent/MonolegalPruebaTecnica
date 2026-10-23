namespace FacturacionApi.Application.Models;

public sealed record ProcesamientoRecordatoriosResponse(
    int Procesadas,
    int Fallidas,
    IReadOnlyList<string> Errores);
