namespace FacturacionApi.Domain;

public sealed class Factura
{
    public string Id { get; init; } = string.Empty;
    public string NombreCliente { get; init; } = string.Empty;
    public string CorreoCliente { get; init; } = string.Empty;
    public decimal Monto { get; init; }
    public string Estado { get; init; } = string.Empty;
}
