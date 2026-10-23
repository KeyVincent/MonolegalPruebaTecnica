using FacturacionApi.Domain;

namespace FacturacionApi.Application.Interfaces;

public interface IFacturaRepository
{
    Task<IReadOnlyList<Factura>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Factura>> GetPendientesDeRecordatorioAsync(CancellationToken cancellationToken);
    Task UpdateEstadoAsync(string id, string estado, CancellationToken cancellationToken);
}