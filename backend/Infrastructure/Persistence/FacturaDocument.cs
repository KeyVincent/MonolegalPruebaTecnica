using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FacturacionApi.Infrastructure.Persistence;

internal sealed class FacturaDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("nombreCliente")]
    public string NombreCliente { get; set; } = string.Empty;

    [BsonElement("correoCliente")]
    public string CorreoCliente { get; set; } = string.Empty;

    [BsonElement("monto")]
    public decimal Monto { get; set; }

    [BsonElement("estado")]
    public string Estado { get; set; } = string.Empty;
}
