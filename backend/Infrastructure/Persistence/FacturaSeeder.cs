using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Options;

namespace FacturacionApi.Infrastructure.Persistence;

public sealed class FacturaSeeder
{
    private readonly IMongoCollection<FacturaDocument> _facturas;

    public FacturaSeeder(IMongoClient mongoClient, IOptions<MongoDbOptions> options)
    {
        var settings = options.Value;
        _facturas = mongoClient
            .GetDatabase(settings.DatabaseName)
            .GetCollection<FacturaDocument>(settings.CollectionName);
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var ejemplos = new[]
        {
            new
            {
                NombreCliente = "Ana García",
                CorreoCliente = "ana@example.com",
                Monto = 12500.50m,
                Estado = "primerrecordatorio"
            },
            new
            {
                NombreCliente = "Luis Martínez",
                CorreoCliente = "luis@example.com",
                Monto = 89000.99m,
                Estado = "segundorecordatorio"
            },
            new
            {
                NombreCliente = "María López",
                CorreoCliente = "maria@example.com",
                Monto = 210000m,
                Estado = "primerrecordatorio"
            }
        };

        foreach (var ejemplo in ejemplos)
        {
            var filtro = Builders<FacturaDocument>.Filter.Eq(
                factura => factura.CorreoCliente,
                ejemplo.CorreoCliente);
            var actualizacion = Builders<FacturaDocument>.Update
                .SetOnInsert(factura => factura.Id, ObjectId.GenerateNewId().ToString())
                .Set(factura => factura.NombreCliente, ejemplo.NombreCliente)
                .Set(factura => factura.Monto, ejemplo.Monto)
                .Set(factura => factura.Estado, ejemplo.Estado);

            await _facturas.UpdateOneAsync(
                filtro,
                actualizacion,
                new UpdateOptions { IsUpsert = true },
                cancellationToken);
        }
    }
}
