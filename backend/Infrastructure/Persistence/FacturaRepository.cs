using FacturacionApi.Application.Interfaces;
using FacturacionApi.Domain;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace FacturacionApi.Infrastructure.Persistence;

public sealed class FacturaRepository : IFacturaRepository
{
    private readonly IMongoCollection<FacturaDocument> _facturas;

    public FacturaRepository(IMongoClient mongoClient, IOptions<MongoDbOptions> options)
    {
        var settings = options.Value;
        _facturas = mongoClient
            .GetDatabase(settings.DatabaseName)
            .GetCollection<FacturaDocument>(settings.CollectionName);
    }

    public async Task<IReadOnlyList<Factura>> GetAllAsync(CancellationToken cancellationToken)
    {
        var documentos = await _facturas.Find(FilterDefinition<FacturaDocument>.Empty)
            .SortBy(documento => documento.NombreCliente)
            .ToListAsync(cancellationToken);
        return documentos.Select(ToDomain).ToArray();
    }

    public async Task<IReadOnlyList<Factura>> GetPendientesDeRecordatorioAsync(CancellationToken cancellationToken)
    {
        var filtro = Builders<FacturaDocument>.Filter.In(
            documento => documento.Estado,
            ["primerrecordatorio", "segundorecordatorio"]);
        var documentos = await _facturas.Find(filtro).ToListAsync(cancellationToken);
        return documentos.Select(ToDomain).ToArray();
    }

    public async Task UpdateEstadoAsync(string id, string estado, CancellationToken cancellationToken)
    {
        var resultado = await _facturas.UpdateOneAsync(
            documento => documento.Id == id,
            Builders<FacturaDocument>.Update.Set(documento => documento.Estado, estado),
            cancellationToken: cancellationToken);

        if (resultado.MatchedCount == 0)
        {
            throw new KeyNotFoundException($"No se encontró la factura {id} para actualizar su estado.");
        }
    }

    private static Factura ToDomain(FacturaDocument documento) => new()
    {
        Id = documento.Id,
        NombreCliente = documento.NombreCliente,
        CorreoCliente = documento.CorreoCliente,
        Monto = documento.Monto,
        Estado = documento.Estado
    };
}
