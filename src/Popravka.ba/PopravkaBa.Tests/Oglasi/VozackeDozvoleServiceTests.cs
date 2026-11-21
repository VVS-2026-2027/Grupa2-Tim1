using PopravkaBa.Application.Services;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Models;

namespace PopravkaBa.Tests.Oglasi;

[TestClass]
[TestCategory("Service")]
public class VozackeDozvoleServiceTests
{
    private Mock<IVozackeDozvoleRepository> _repo = null!;
    private VozackeDozvoleService _servis = null!;

    [TestInitialize]
    public void Postavi()
    {
        _repo = new Mock<IVozackeDozvoleRepository>();
        _servis = new VozackeDozvoleService(_repo.Object);
    }

    [TestMethod]
    [Owner("Nerina Čengić")]
    public async Task DajSveVozackeDozvoleOglasa_ProslijedjenId_VracaDozvole()
    {
        // Arrange
        var dozvole = new List<VozackaDozvola> { VozackaDozvola.B, VozackaDozvola.BE };
        _repo.Setup(r => r.DajSveVozackeDozvoleOglasa(10)).ReturnsAsync(dozvole);

        // Act
        var rezultat = await _servis.DajSveVozackeDozvoleOglasa(10);

        // Assert
        Assert.AreSame(dozvole, rezultat);
    }

    [TestMethod]
    [Owner("Nerina Čengić")]
    public async Task DodajVozackeDozvoleOglasu_ProslijedjeneDozvole_PozivaRepozitorij()
    {
        // Arrange
        var dozvole = new List<VozackaDozvola> { VozackaDozvola.B };

        // Act
        await _servis.DodajVozackeDozvoleOglasu(10, dozvole);

        // Assert
        _repo.Verify(r => r.DodajVozackeDozvoleOglasu(10, dozvole), Times.Once);
    }

    [TestMethod]
    [Owner("Nerina Čengić")]
    public async Task DodajVozackeDozvoleOglasu_NullLista_ProsljedjujeNull()
    {
        // Act
        await _servis.DodajVozackeDozvoleOglasu(10, null);

        // Assert
        _repo.Verify(r => r.DodajVozackeDozvoleOglasu(10, It.Is<List<VozackaDozvola>?>(d => d == null)), Times.Once);
    }

    [TestMethod]
    [Owner("Nerina Čengić")]
    public async Task UkloniSveVozackeDozvoleOglasa_ProslijedjenId_PozivaRepozitorij()
    {
        // Act
        await _servis.UkloniSveVozackeDozvoleOglasa(10);

        // Assert
        _repo.Verify(r => r.UkloniSveVozackeDozvoleOglasa(10), Times.Once);
    }

    [TestMethod]
    [Owner("Nerina Čengić")]
    public async Task AzurirajVozackeDozvoleOglasa_ProslijedjeneDozvole_PozivaRepozitorij()
    {
        // Arrange
        var dozvole = new List<VozackaDozvola> { VozackaDozvola.C };

        // Act
        await _servis.AzurirajVozackeDozvoleOglasa(10, dozvole);

        // Assert
        _repo.Verify(r => r.AzurirajVozackeDozvoleOglasa(10, dozvole), Times.Once);
    }

    [TestMethod]
    [Owner("Nerina Čengić")]
    public async Task DajSveVozackeDozvoleOglasa_RepozitorijBacaIzuzetak_PropagiraGa()
    {
        // Arrange
        _repo.Setup(r => r.DajSveVozackeDozvoleOglasa(10)).ThrowsAsync(new InvalidOperationException("greska"));

        // Act & Assert
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _servis.DajSveVozackeDozvoleOglasa(10));
    }
}
