using FacturacionApi.Application.Models;

namespace FacturacionApi.Application.Interfaces;

public interface IFacturaService
{
    Task<ResumenFacturasResponse> ObtenerResumenAsync(CancellationToken cancellationToken);
    Task<ProcesamientoRecordatoriosResponse> ProcesarRecordatoriosAsync(CancellationToken cancellationToken);
}
