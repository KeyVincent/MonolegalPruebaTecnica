namespace FacturacionApi.Infrastructure.Persistence;

public sealed class MongoDbOptions
{
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "FacturacionDemo";
    public string CollectionName { get; set; } = "Facturas";
    public bool SeedSampleData { get; set; }
}
