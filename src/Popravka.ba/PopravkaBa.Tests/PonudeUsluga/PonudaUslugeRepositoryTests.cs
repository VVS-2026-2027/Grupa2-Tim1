using Popravka.ba.Data;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Models;
using PopravkaBa.Infrastructure.Repositories;
using static PopravkaBa.Tests.PonudeUsluga.PonudaUslugeRepositoryTestPomoc;

namespace PopravkaBa.Tests.PonudeUsluga;

[TestClass]
public class PonudaUslugeRepositoryTests
{
    private ApplicationDbContext _baza = null!;
    private PonudaUslugeRepository _repozitorij = null!;

    [TestInitialize]
    public async Task PripremiBazu()
    {
        _baza = TestHelpers.NovaBaza();
        _repozitorij = new PonudaUslugeRepository(_baza);

        _baza.AddRange(
            new Klijent { Id = "k1", UserName = "klijent1" },
            new Klijent { Id = "k2", UserName = "klijent2" },
            new Majstor { Id = "m1", UserName = "majstor1", Adresa = "Adresa 1" },
            new Majstor { Id = "m2", UserName = "majstor2", Adresa = "Adresa 2" },
            new Mjesto { MjestoID = 1, Naziv = "Sarajevo" },
            new Kategorija { ID = 1, Naziv = "Vodoinstalacije" },
            new Kategorija { ID = 2, Naziv = "Soboslikarstvo" },
            NoviOglas(10, "k1"),
            NoviOglas(20, "k2"),
            new OglasKategorija { ID = 1, OglasID = 10, KategorijaID = 1 },
            new OglasKategorija { ID = 2, OglasID = 20, KategorijaID = 2 });

        await _baza.SaveChangesAsync();
        _baza.ChangeTracker.Clear();
    }

    [TestCleanup]
    public async Task OslobodiBazu()
    {
        await _baza.DisposeAsync();
    }

    [TestMethod]
    public async Task DajPoIdAsync_PostojecaPonuda_VracaPonuduSPovezanimPodacima()
    {
        // Arrange
        await UbaciPonude(_baza, NovaPonuda(1), NovaPonuda(2, 20, "m2"));

        // Act
        var rezultat = await _repozitorij.DajPoIdAsync(2);

        // Assert
        Assert.IsNotNull(rezultat);
        Assert.AreEqual(2, rezultat.ID);
        Assert.AreEqual("m2", rezultat.Izvrsilac.Id);
        Assert.AreEqual(20, rezultat.OglasUsluge.OglasID);
    }

    [TestMethod]
    public async Task DajPoIdAsync_NepostojecaPonuda_VracaNull()
    {
        // Arrange
        await UbaciPonude(_baza, NovaPonuda(1));

        // Act
        var rezultat = await _repozitorij.DajPoIdAsync(99);

        // Assert
        Assert.IsNull(rezultat);
    }

    [TestMethod]
    public async Task DajSvePonudeOglasaAsync_ViseOglasa_FiltriraISortiraOdNajstarije()
    {
        // Arrange
        await UbaciPonude(_baza,
            NovaPonuda(2, 10, "m2"),
            NovaPonuda(1),
            NovaPonuda(3, 20));

        // Act
        var rezultat = (await _repozitorij.DajSvePonudeOglasaAsync(10)).ToArray();

        // Assert
        CollectionAssert.AreEqual(new[] { 1, 2 }, rezultat.Select(p => p.ID).ToArray());
        Assert.AreEqual("m1", rezultat[0].Izvrsilac.Id);
        Assert.AreEqual("m2", rezultat[1].Izvrsilac.Id);
    }

    [TestMethod]
    public async Task DajSvePonudeIzvrsiocaAsync_ViseIzvrsilaca_FiltriraISortiraOdNajnovije()
    {
        // Arrange
        await UbaciPonude(_baza,
            NovaPonuda(1),
            NovaPonuda(2, 20),
            NovaPonuda(3, 10, "m2"));

        // Act
        var rezultat = (await _repozitorij.DajSvePonudeIzvrsiocaAsync("m1")).ToArray();

        // Assert
        CollectionAssert.AreEqual(new[] { 2, 1 }, rezultat.Select(p => p.ID).ToArray());
        Assert.AreEqual(20, rezultat[0].OglasUsluge.OglasID);
        Assert.AreEqual(10, rezultat[1].OglasUsluge.OglasID);
    }

    [TestMethod]
    public async Task DodajAsync_NovaPonuda_SpremaPonuduUBazu()
    {
        // Arrange
        var ponuda = NovaPonuda(1, cijena: 120);
        ponuda.Poruka = "Mogu preuzeti posao.";

        // Act
        await _repozitorij.DodajAsync(ponuda);

        // Assert
        _baza.ChangeTracker.Clear();
        var spremljena = await _baza.PonudeUsluge.FindAsync(1);
        Assert.IsNotNull(spremljena);
        Assert.AreEqual(10, spremljena.OglasUslugeID);
        Assert.AreEqual("m1", spremljena.IzvrsilacID);
        Assert.AreEqual((int?)120, spremljena.Cijena);
        Assert.AreEqual("Mogu preuzeti posao.", spremljena.Poruka);
    }

    [TestMethod]
    public async Task UrediAsync_IzmijenjenaPonuda_SpremaNoveVrijednosti()
    {
        // Arrange
        var ponuda = NovaPonuda(1);
        await UbaciPonude(_baza, ponuda);
        ponuda.Cijena = 130;
        ponuda.StatusPonude = Status.Prihvaceno;

        // Act
        await _repozitorij.UrediAsync(ponuda);

        // Assert
        _baza.ChangeTracker.Clear();
        var spremljena = await _baza.PonudeUsluge.FindAsync(1);
        Assert.IsNotNull(spremljena);
        Assert.AreEqual((int?)130, spremljena.Cijena);
        Assert.AreEqual(Status.Prihvaceno, spremljena.StatusPonude);
    }

    [TestMethod]
    public async Task ObrisiAsync_PostojecaPonuda_BriseSamoZadanuPonudu()
    {
        // Arrange
        await UbaciPonude(_baza, NovaPonuda(1), NovaPonuda(2, 10, "m2"));

        // Act
        await _repozitorij.ObrisiAsync(1);

        // Assert
        _baza.ChangeTracker.Clear();
        Assert.IsNull(await _baza.PonudeUsluge.FindAsync(1));
        Assert.IsNotNull(await _baza.PonudeUsluge.FindAsync(2));
    }

    [TestMethod]
    public async Task ObrisiAsync_NepostojecaPonuda_OstavljaPostojecePonude()
    {
        // Arrange
        await UbaciPonude(_baza, NovaPonuda(1));

        // Act
        await _repozitorij.ObrisiAsync(99);

        // Assert
        _baza.ChangeTracker.Clear();
        Assert.IsNotNull(await _baza.PonudeUsluge.FindAsync(1));
        Assert.AreEqual(1, _baza.PonudeUsluge.Count());
    }

    [TestMethod]
    public async Task DajProsjekCijenePoKategorijama_PrazneKategorije_VracaNull()
    {
        // Arrange
        await UbaciPonude(_baza, NovaPonuda(1, cijena: 100));

        // Act
        var rezultat = await _repozitorij.DajProsjekCijenePoKategorijama(
            Array.Empty<int>());

        // Assert
        Assert.IsNull(rezultat);
    }

    [TestMethod]
    public async Task DajProsjekCijenePoKategorijama_KategorijaBezCijena_VracaNull()
    {
        // Arrange
        await UbaciPonude(_baza,
            NovaPonuda(1, cijena: null),
            NovaPonuda(2, 20, cijena: 200));

        // Act
        var rezultat = await _repozitorij.DajProsjekCijenePoKategorijama(new[] { 1 });

        // Assert
        Assert.IsNull(rezultat);
    }

    [TestMethod]
    public async Task DajProsjekCijenePoKategorijama_RazlicitePonude_RacunaProsjekSamoOdgovarajucihCijena()
    {
        // Arrange
        await UbaciPonude(_baza,
            NovaPonuda(1, cijena: 100),
            NovaPonuda(2, 10, "m2", cijena: 151),
            NovaPonuda(3, cijena: null),
            NovaPonuda(4, 20, cijena: 500));

        // Act
        var rezultat = await _repozitorij.DajProsjekCijenePoKategorijama(new[] { 1 });

        // Assert
        Assert.AreEqual((decimal?)125.5m, rezultat);
    }

    [TestMethod]
    [DataRow("k1", "m1", true)]
    [DataRow("k1", "m2", false)]
    [DataRow("k2", "m1", false)]
    public async Task PostojiZavrseniPosaoAsync_RazlicitiParovi_VracaOcekivaniRezultat(
        string klijentId, string izvrsilacId, bool ocekivano)
    {
        // Arrange
        var zavrsena = NovaPonuda(1);
        zavrsena.StatusPonude = Status.Isporuceno;
        var prihvacena = NovaPonuda(2, 10, "m2");
        prihvacena.StatusPonude = Status.Prihvaceno;
        await UbaciPonude(_baza, zavrsena, prihvacena);

        // Act
        var rezultat = await _repozitorij.PostojiZavrseniPosaoAsync(
            klijentId, izvrsilacId);

        // Assert
        Assert.AreEqual(ocekivano, rezultat);
    }
}