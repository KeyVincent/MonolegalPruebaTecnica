using FacturacionApi.Application.Interfaces;
using FacturacionApi.Application.Services;
using FacturacionApi.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FacturacionApi.Tests;

public sealed class FacturaServiceTests
{
    [Fact]
    public async Task ProcesarRecordatoriosAsync_EnvíaCorreoYActualizaEstadoParaAmbasEtapas()
    {
        var facturas = new[]
        {
            new Factura
            {
                Id = "1", NombreCliente = "Ana", CorreoCliente = "ana@example.com",
                Estado = "primerrecordatorio"
            },
            new Factura
            {
                Id = "2", NombreCliente = "Luis", CorreoCliente = "luis@example.com",
                Estado = "segundorecordatorio"
            },
            new Factura
            {
                Id = "3", NombreCliente = "María", CorreoCliente = "maria@example.com",
                Estado = "primerrecordatorio"
            }
        };
        var repository = new Mock<IFacturaRepository>();
        repository.Setup(r => r.GetPendientesDeRecordatorioAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(facturas);
        var email = new Mock<IEmailService>();
        email.Setup(s => s.EnviarCorreoAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var service = new FacturaService(repository.Object, email.Object, NullLogger<FacturaService>.Instance);

        var resultado = await service.ProcesarRecordatoriosAsync(CancellationToken.None);

        Assert.Equal(3, resultado.Procesadas);
        Assert.Empty(resultado.Errores);
        email.Verify(s => s.EnviarCorreoAsync(
            "ana@example.com",
            "Segundo recordatorio de pago",
            It.Is<string>(mensaje => mensaje.Contains("segundo recordatorio")),
            It.IsAny<CancellationToken>()), Times.Once);
        email.Verify(s => s.EnviarCorreoAsync(
            "luis@example.com",
            "Aviso de desactivación de cuenta",
            It.Is<string>(mensaje => mensaje.Contains("desactivada")),
            It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.UpdateEstadoAsync("1", "segundorecordatorio", It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.UpdateEstadoAsync("2", "desactivado", It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.UpdateEstadoAsync("3", "segundorecordatorio", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcesarRecordatoriosAsync_SiElCorreoFalla_NoActualizaLaFactura()
    {
        var factura = new Factura
        {
            Id = "1", NombreCliente = "Ana", CorreoCliente = "ana@example.com",
            Estado = "primerrecordatorio"
        };
        var repository = new Mock<IFacturaRepository>();
        repository.Setup(r => r.GetPendientesDeRecordatorioAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([factura]);
        var email = new Mock<IEmailService>();
        email.Setup(s => s.EnviarCorreoAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP no disponible"));
        var service = new FacturaService(repository.Object, email.Object, NullLogger<FacturaService>.Instance);

        var resultado = await service.ProcesarRecordatoriosAsync(CancellationToken.None);

        Assert.Equal(0, resultado.Procesadas);
        Assert.Equal(1, resultado.Fallidas);
        Assert.Contains("SMTP no disponible", resultado.Errores[0]);
        repository.Verify(r => r.UpdateEstadoAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
