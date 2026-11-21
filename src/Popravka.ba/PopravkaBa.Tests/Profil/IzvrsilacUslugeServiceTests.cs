using PopravkaBa.Application.Services.Implementation;
using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Models;
using static PopravkaBa.Tests.Profil.IzvrsilacUslugeServiceTestHelper;

namespace PopravkaBa.Tests.Profil;

[TestClass]
public class IzvrsilacUslugeServiceTests
{
    private Mock<IIzvrsilacUslugeRepository> _repo = null!;
    private IzvrsilacUslugeService _servis = null!;

    [TestInitialize]
    public void Postavi()
    {
        _repo = new Mock<IIzvrsilacUslugeRepository>();
        _servis = new IzvrsilacUslugeService(_repo.Object);
    }

    [TestMethod]
    public async Task DajProfilPoIdAsync_PostojeciId_VracaProfil()
    {
        // Arrange
        var majstor = Majstor("m1");
        _repo.Setup(r => r.DajProfilPoIdAsync("m1")).ReturnsAsync(majstor);

        // Act
        var rezultat = await _servis.DajProfilPoIdAsync("m1");

        // Assert
        Assert.AreSame(majstor, rezultat);
    }

    [TestMethod]
    public async Task DajProfilPoIdAsync_NepostojeciId_VracaNull()
    {
        // Arrange
        _repo.Setup(r => r.DajProfilPoIdAsync("nema")).ReturnsAsync((IzvrsilacUsluge?)null);

        // Act
        var rezultat = await _servis.DajProfilPoIdAsync("nema");

        // Assert
        Assert.IsNull(rezultat);
    }

    [TestMethod]
    public async Task UrediAsync_ProslijedjenIzvrsilac_PozivaRepozitorij()
    {
        // Arrange
        var majstor = Majstor("m1");

        // Act
        await _servis.UrediAsync(majstor);

        // Assert
        _repo.Verify(r => r.UrediAsync(majstor), Times.Once);
    }

    [TestMethod]
    public async Task DajSveAsync_PozivaRepozitorij_VracaSve()
    {
        // Arrange
        var izvrsioci = new List<IzvrsilacUsluge> { Majstor("m1") };
        _repo.Setup(r => r.DajSveAsync()).ReturnsAsync(izvrsioci);

        // Act
        var rezultat = await _servis.DajSveAsync();

        // Assert
        Assert.AreSame(izvrsioci, rezultat);
    }

    [TestMethod]
    public async Task DajProfilPoIdAsync_RepozitorijBacaIzuzetak_PropagiraGa()
    {
        // Arrange
        _repo.Setup(r => r.DajProfilPoIdAsync("m1")).ThrowsAsync(new InvalidOperationException("greska"));

        // Act & Assert
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _servis.DajProfilPoIdAsync("m1"));
    }
}
