using PopravkaBa.Application.Services;
using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Interfaces.Repositories;
using PopravkaBa.Domain.Models;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Application.DTOs;

namespace PopravkaBa.Tests.Recenzije;

[TestClass]
[TestCategory("Service")]
public class RecenzijaServiceTests
{
    private Mock<IRecenzijaRepository> _recenzijaRepo = null!;
    private Mock<IPonudaUslugeRepository> _ponudaRepo = null!;
  private Mock<IIzvrsilacUslugeRepository> _izvrsilacRepo = null!;
    private RecenzijaService _servis = null!;

   [TestInitialize]
public void PripremiTest()
{
    _recenzijaRepo = new Mock<IRecenzijaRepository>();
    _ponudaRepo = new Mock<IPonudaUslugeRepository>();
    _izvrsilacRepo = new Mock<IIzvrsilacUslugeRepository>();

    _servis = new RecenzijaService(
        _recenzijaRepo.Object,
        _ponudaRepo.Object,
        _izvrsilacRepo.Object);
}

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task ObrisiRecenziju_NepostojecaRecenzija_BacaKeyNotFoundException()
    {
        // Arrange
        const int recenzijaId = 5;
        _recenzijaRepo
            .Setup(repo => repo.DajPoIdAsync(recenzijaId))
            .ReturnsAsync((Recenzija?)null);

        // Act
        Func<Task> obrisi = () => _servis.ObrisiRecenziju(recenzijaId);

        // Assert
        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(obrisi);
        _recenzijaRepo.Verify(repo => repo.ObrisiAsync(recenzijaId), Times.Never());
    }
    public static IEnumerable<object[]> SlucajeviIzCsv =>
    DataDrivenInputParser.CitajCsv<bool, bool, bool>(
        "TestData/csv/RecenzijaService_mozeOstavitiRecenziju.csv");

    [TestMethod]
    [TestCategory("DataDriven")]
    [Owner("Amina Bukvić")]
    [DynamicData(nameof(SlucajeviIzCsv))]
    public async Task MozeOstavitiRecenziju_PodaciIzCsv_VracaOcekivano(
        bool zavrsenPosao, bool vecPostoji, bool ocekivano)
    {
        // Arrange
        const string klijentId = "k1";
        const string izvrsilacId = "m1";

        _ponudaRepo
            .Setup(repo => repo.PostojiZavrseniPosaoAsync(klijentId, izvrsilacId))
            .ReturnsAsync(zavrsenPosao);

        _recenzijaRepo
            .Setup(repo => repo.PostojiAsync(klijentId, izvrsilacId))
            .ReturnsAsync(vecPostoji);

        // Act
        bool rezultat = await _servis.MozeOstavitiRecenziju(klijentId, izvrsilacId);

        // Assert
        Assert.AreEqual(ocekivano, rezultat);
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
public async Task DajRecenzijuPoId_PostojecaRecenzija_VracaRecenziju()
{
    // Arrange
    const int recenzijaId = 5;
    var ocekivanaRecenzija = new Recenzija { RecenzijaID = recenzijaId };
    _recenzijaRepo
        .Setup(repo => repo.DajPoIdAsync(recenzijaId))
        .ReturnsAsync(ocekivanaRecenzija);

    // Act
    var rezultat = await _servis.DajRecenzijuPoId(recenzijaId);

    // Assert
    Assert.AreSame(ocekivanaRecenzija, rezultat);
}

[TestMethod]
[Owner("Amina Bukvić")]
public async Task OdbaciPrijavu_NepostojecaRecenzija_BacaKeyNotFoundException()
{
    // Arrange
    const int recenzijaId = 5;
    _recenzijaRepo
        .Setup(repo => repo.DajPoIdAsync(recenzijaId))
        .ReturnsAsync((Recenzija?)null);

    // Act
    Func<Task> odbaciPrijavu = () => _servis.OdbaciPrijavu(recenzijaId);

    // Assert
    await Assert.ThrowsExactlyAsync<KeyNotFoundException>(odbaciPrijavu);
}

[TestMethod]
[Owner("Amina Bukvić")]
public async Task OdbaciPrijavu_PostojecaPrijava_OdbijaPrijavuICuvaRecenziju()
{
    // Arrange
    const int recenzijaId = 5;
    var recenzija = new Recenzija
    {
        RecenzijaID = recenzijaId,
        Prijavljena = true,
        StatusPrijave = Status.NaCekanju
    };

    _recenzijaRepo
        .Setup(repo => repo.DajPoIdAsync(recenzijaId))
        .ReturnsAsync(recenzija);

    // Act
    await _servis.OdbaciPrijavu(recenzijaId);

    // Assert
    Assert.IsFalse(recenzija.Prijavljena);
    Assert.AreEqual(Status.Odbijeno, recenzija.StatusPrijave);
    _recenzijaRepo.Verify(repo => repo.SacuvajAsync(recenzija), Times.Once());
}

[TestMethod]
[Owner("Amina Bukvić")]
public async Task PrijaviRecenziju_NeprijavljenaRecenzija_PostavljaStatusNaCekanjuICuva()
{
    // Arrange
    const int recenzijaId = 5;
    var recenzija = new Recenzija { RecenzijaID = recenzijaId };
    var prijava = new PrijaviRecenzijuDto
    {
        RecenzijaID = recenzijaId,
        RazlogPrijave = "Neprimjeren sadržaj"
    };

    _recenzijaRepo
        .Setup(repo => repo.DajPoIdAsync(recenzijaId))
        .ReturnsAsync(recenzija);

    // Act
    await _servis.PrijaviRecenziju(prijava);

    // Assert
    Assert.IsTrue(recenzija.Prijavljena);
    Assert.AreEqual(Status.NaCekanju, recenzija.StatusPrijave);
    _recenzijaRepo.Verify(repo => repo.SacuvajAsync(recenzija), Times.Once());
}

[TestMethod]
[Owner("Amina Bukvić")]
public async Task PrijaviRecenziju_PrijavaVecNaCekanju_BacaInvalidOperationException()
{
    // Arrange
    const int recenzijaId = 5;
    var recenzija = new Recenzija
    {
        RecenzijaID = recenzijaId,
        Prijavljena = true,
        StatusPrijave = Status.NaCekanju
    };
    var prijava = new PrijaviRecenzijuDto
    {
        RecenzijaID = recenzijaId,
        RazlogPrijave = "Ponovljena prijava"
    };

    _recenzijaRepo
        .Setup(repo => repo.DajPoIdAsync(recenzijaId))
        .ReturnsAsync(recenzija);

    // Act
    Func<Task> prijavi = () => _servis.PrijaviRecenziju(prijava);

    // Assert
    await Assert.ThrowsExactlyAsync<InvalidOperationException>(prijavi);
    _recenzijaRepo.Verify(
        repo => repo.SacuvajAsync(It.IsAny<Recenzija>()),
        Times.Never());
}

[TestMethod]
[Owner("Amina Bukvić")]
public async Task PrijaviRecenziju_NepostojecaRecenzija_BacaKeyNotFoundException()
{
    // Arrange
    const int recenzijaId = 5;
    var prijava = new PrijaviRecenzijuDto
    {
        RecenzijaID = recenzijaId,
        RazlogPrijave = "Neprimjeren sadržaj"
    };

    _recenzijaRepo
        .Setup(repo => repo.DajPoIdAsync(recenzijaId))
        .ReturnsAsync((Recenzija?)null);

    // Act
    Func<Task> prijavi = () => _servis.PrijaviRecenziju(prijava);

    // Assert
    await Assert.ThrowsExactlyAsync<KeyNotFoundException>(prijavi);
    _recenzijaRepo.Verify(
        repo => repo.SacuvajAsync(It.IsAny<Recenzija>()),
        Times.Never());
}
[TestMethod]
[Owner("Amina Bukvić")]
public async Task ObjaviRecenziju_NezavrsenPosao_BacaInvalidOperationException()
{
    // Arrange
    const string klijentId = "k1";
    const string izvrsilacId = "m1";
    var recenzija = new KreirajRecenzijuDto
    {
        IzvrsilacID = izvrsilacId,
        Ocjena = 5,
        Komentar = "Odlična usluga"
    };

    _ponudaRepo
        .Setup(repo => repo.PostojiZavrseniPosaoAsync(klijentId, izvrsilacId))
        .ReturnsAsync(false);

    // Act
    Func<Task> objavi = () => _servis.ObjaviRecenziju(recenzija, klijentId);

    // Assert
    await Assert.ThrowsExactlyAsync<InvalidOperationException>(objavi);
    _recenzijaRepo.Verify(
        repo => repo.DodajAsync(It.IsAny<Recenzija>()),
        Times.Never());
}
[TestMethod]
[Owner("Amina Bukvić")]
public async Task ObjaviRecenziju_ZavrsenPosaoBezRecenzije_CuvaRecenzijuIOsvjezavaOcjenu()
{
    // Arrange
    const string klijentId = "k1";
    const string izvrsilacId = "m1";
    var zahtjev = new KreirajRecenzijuDto
    {
        IzvrsilacID = izvrsilacId,
        Ocjena = 5,
        Komentar = "  Odlična usluga  "
    };

    _ponudaRepo
        .Setup(repo => repo.PostojiZavrseniPosaoAsync(klijentId, izvrsilacId))
        .ReturnsAsync(true);

    _recenzijaRepo
        .Setup(repo => repo.PostojiAsync(klijentId, izvrsilacId))
        .ReturnsAsync(false);

    _recenzijaRepo
        .Setup(repo => repo.DajRecenzijeIzvrsiocaAsync(izvrsilacId))
        .ReturnsAsync(new List<Recenzija>
        {
            new Recenzija { Ocjena = 4 },
            new Recenzija { Ocjena = 5 }
        });

    // Act
    await _servis.ObjaviRecenziju(zahtjev, klijentId);

    // Assert
    _recenzijaRepo.Verify(
        repo => repo.DodajAsync(It.Is<Recenzija>(r =>
            r.KlijentID == klijentId &&
            r.IzvrsilacID == izvrsilacId &&
            r.Ocjena == 5 &&
            r.Komentar == "Odlična usluga")),
        Times.Once());

    _izvrsilacRepo.Verify(
        repo => repo.OsvjeziOcjeneAsync(izvrsilacId, 4.5m, 2),
        Times.Once());
}
[TestMethod]
[Owner("Amina Bukvić")]
public async Task ObrisiRecenziju_PosljednjaRecenzija_BriseRecenzijuIPostavljaOcjenuNaNulu()
{
    // Arrange
    const int recenzijaId = 5;
    const string izvrsilacId = "m1";

    var recenzija = new Recenzija
    {
        RecenzijaID = recenzijaId,
        IzvrsilacID = izvrsilacId
    };

    _recenzijaRepo
        .Setup(repo => repo.DajPoIdAsync(recenzijaId))
        .ReturnsAsync(recenzija);

    _recenzijaRepo
        .Setup(repo => repo.DajRecenzijeIzvrsiocaAsync(izvrsilacId))
        .ReturnsAsync(new List<Recenzija>());

    // Act
    await _servis.ObrisiRecenziju(recenzijaId);

    // Assert
    _recenzijaRepo.Verify(
        repo => repo.ObrisiAsync(recenzijaId),
        Times.Once());

    _izvrsilacRepo.Verify(
        repo => repo.OsvjeziOcjeneAsync(izvrsilacId, 0m, 0),
        Times.Once());
}

[TestMethod]
[Owner("Amina Bukvić")]
public async Task DajSveRecenzije_PostojecaRecenzija_VracaListuIzRepozitorija()
{
    // Arrange
    var ocekivaneRecenzije = new List<Recenzija>
    {
        new Recenzija { RecenzijaID = 5 }
    };

    _recenzijaRepo
        .Setup(repo => repo.DajSveAsync())
        .ReturnsAsync(ocekivaneRecenzije);

    // Act
    var rezultat = await _servis.DajSveRecenzije();

    // Assert
    CollectionAssert.AreEqual(ocekivaneRecenzije, rezultat.ToList());
}

[TestMethod]
[Owner("Amina Bukvić")]
public async Task DajRecenzijePoId_ZadaniIzvrsilac_VracaNjegoveRecenzije()
{
    // Arrange
    const string izvrsilacId = "m1";
    var ocekivaneRecenzije = new List<Recenzija>
    {
        new Recenzija { RecenzijaID = 5, IzvrsilacID = izvrsilacId }
    };

    _recenzijaRepo
        .Setup(repo => repo.DajRecenzijeIzvrsiocaAsync(izvrsilacId))
        .ReturnsAsync(ocekivaneRecenzije);

    // Act
    var rezultat = await _servis.DajRecenzijePoId(izvrsilacId);

    // Assert
    CollectionAssert.AreEqual(ocekivaneRecenzije, rezultat.ToList());
    _recenzijaRepo.Verify(
        repo => repo.DajRecenzijeIzvrsiocaAsync(izvrsilacId),
        Times.Once());
}

[TestMethod]
[Owner("Amina Bukvić")]
public async Task DajRecenzijeKlijenta_ZadaniKlijent_VracaNjegoveRecenzije()
{
    // Arrange
    const string klijentId = "k1";
    var ocekivaneRecenzije = new List<Recenzija>
    {
        new Recenzija { RecenzijaID = 5, KlijentID = klijentId }
    };

    _recenzijaRepo
        .Setup(repo => repo.DajRecenzijeKlijentaAsync(klijentId))
        .ReturnsAsync(ocekivaneRecenzije);

    // Act
    var rezultat = await _servis.DajRecenzijeKlijenta(klijentId);

    // Assert
    CollectionAssert.AreEqual(ocekivaneRecenzije, rezultat.ToList());
    _recenzijaRepo.Verify(
        repo => repo.DajRecenzijeKlijentaAsync(klijentId),
        Times.Once());
}

[TestMethod]
[Owner("Amina Bukvić")]
public async Task DajPrijavljeneRecenzije_PostojecaPrijava_VracaPrijavljeneRecenzije()
{
    // Arrange
    var ocekivaneRecenzije = new List<Recenzija>
    {
        new Recenzija { RecenzijaID = 5, Prijavljena = true }
    };

    _recenzijaRepo
        .Setup(repo => repo.DajPrijavljeneAsync())
        .ReturnsAsync(ocekivaneRecenzije);

    // Act
    var rezultat = await _servis.DajPrijavljeneRecenzije();

    // Assert
    CollectionAssert.AreEqual(ocekivaneRecenzije, rezultat.ToList());
}
}