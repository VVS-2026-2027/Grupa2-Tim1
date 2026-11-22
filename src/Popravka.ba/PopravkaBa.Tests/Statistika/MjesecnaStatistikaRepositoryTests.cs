using Popravka.ba.Data;
using PopravkaBa.Application.Strategies.Implementation;
using PopravkaBa.Application.Strategies.Interface;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Models;
using PopravkaBa.Infrastructure.Repositories;
using static PopravkaBa.Tests.Statistika.StatistikaTestHelper;

namespace PopravkaBa.Tests.Statistika;

// Čitanje se testira nad InMemory bazom. ZamijeniSnapshot koristi transakciju i ExecuteDeleteAsync,
// koje InMemory ne podržava, pa se on testira nad SQLite bazom u memoriji.
[TestClass]
[TestCategory("Repository")]
public class MjesecnaStatistikaRepositoryTests
{
    private ApplicationDbContext _baza = null!;
    private MjesecnaStatistikaRepository _repo = null!;
    private StatistikaSortResolver _sort = null!;

    [TestInitialize]
    public void Postavi()
    {
        _sort = new StatistikaSortResolver(new IStatistikaStrategy[]
        {
            new SortStatistikaMajstor(), new SortStatistikaKategorija(), new SortStatistikaMjesto(),
            new SortStatistikaOcjena(), new SortStatistikaPoslovi()
        });
        _baza = TestHelpers.NovaBaza();
        _repo = new MjesecnaStatistikaRepository(_baza, _sort);
    }

    [TestCleanup]
    public async Task OslobodiBazu() => await _baza.DisposeAsync();

    private async Task Ubaci(params MjesecnaStatistikaKompozicija[] redovi)
    {
        _baza.MjesecnaStatistikaKompozicija.AddRange(redovi);
        await _baza.SaveChangesAsync();
        _baza.ChangeTracker.Clear();
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DohvatiFiltrirano_RedoviIzViseMjeseci_VracaSamoTrazeniMjesec()
    {
        // Arrange
        await Ubaci(Red("m1"), Red("m2", mjesec: 10), Red("m3", godina: 2025));

        // Act
        var redovi = await _repo.DohvatiFiltrirano(Godina, Mjesec, null, null, KoloneStatistike.Ocjena, Sortiranje.Descending);

        // Assert
        CollectionAssert.AreEqual(new[] { "m1" }, redovi.Select(r => r.IzvrsilacID).ToList());
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DohvatiFiltrirano_FilterKategorije_VracaSamoTuKategoriju()
    {
        // Arrange
        await Ubaci(Red("m1", kategorijaId: 1), Red("m2", kategorijaId: 2));

        // Act
        var redovi = await _repo.DohvatiFiltrirano(Godina, Mjesec, 2, null, KoloneStatistike.Ocjena, Sortiranje.Descending);

        // Assert
        CollectionAssert.AreEqual(new[] { "m2" }, redovi.Select(r => r.IzvrsilacID).ToList());
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DohvatiFiltrirano_FilterMjesta_VracaSamoToMjesto()
    {
        // Arrange
        await Ubaci(Red("m1", mjestoId: 1), Red("m2", mjestoId: 5));

        // Act
        var redovi = await _repo.DohvatiFiltrirano(Godina, Mjesec, null, 5, KoloneStatistike.Ocjena, Sortiranje.Descending);

        // Assert
        CollectionAssert.AreEqual(new[] { "m2" }, redovi.Select(r => r.IzvrsilacID).ToList());
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DohvatiFiltrirano_SortPoPoslovima_PoredanoPoBrojuPoslova()
    {
        // Arrange
        await Ubaci(Red("m1", poslovi: 2), Red("m2", poslovi: 9), Red("m3", poslovi: 5));

        // Act
        var redovi = await _repo.DohvatiFiltrirano(Godina, Mjesec, null, null, KoloneStatistike.Poslovi, Sortiranje.Descending);

        // Assert
        CollectionAssert.AreEqual(new[] { "m2", "m3", "m1" }, redovi.Select(r => r.IzvrsilacID).ToList());
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DohvatiFiltrirano_IstaOcjena_RedoslijedPoRangu()
    {
        // Arrange
        await Ubaci(Red("m1", rang: 2, ocjena: 4.5m), Red("m2", rang: 1, ocjena: 4.5m));

        // Act
        var redovi = await _repo.DohvatiFiltrirano(Godina, Mjesec, null, null, KoloneStatistike.Ocjena, Sortiranje.Descending);

        // Assert
        CollectionAssert.AreEqual(new[] { "m2", "m1" }, redovi.Select(r => r.IzvrsilacID).ToList());
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DohvatiMeta_RedoviMjeseca_VracaZadnjeVrijemeAzuriranja()
    {
        // Arrange
        var stariji = Red("m1");
        var noviji = Red("m2");
        noviji.VrijemeAzuriranja = stariji.VrijemeAzuriranja.AddHours(3);
        await Ubaci(stariji, noviji);

        // Act
        var meta = await _repo.DohvatiMeta(Godina, Mjesec);

        // Assert
        Assert.AreEqual(noviji.VrijemeAzuriranja, meta.ZadnjeAzuriranje);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DohvatiMeta_PrazanMjesec_VrijemeAzuriranjaJeNull()
    {
        // Act
        var meta = await _repo.DohvatiMeta(Godina, Mjesec);

        // Assert
        Assert.IsNull(meta.ZadnjeAzuriranje);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DohvatiMeta_PonovljeneKategorije_VracaJedinstveneAbecedno()
    {
        // Arrange
        await Ubaci(
            Red("m1", kategorijaId: 2, kategorija: "Stolarija"),
            Red("m2", kategorijaId: 1, kategorija: "Elektro"),
            Red("m3", kategorijaId: 2, kategorija: "Stolarija"),
            Red("m4", kategorijaId: null, kategorija: null));

        // Act
        var meta = await _repo.DohvatiMeta(Godina, Mjesec);

        // Assert
        CollectionAssert.AreEqual(new[] { "Elektro", "Stolarija" }, meta.Kategorije.Select(k => k.Naziv).ToList());
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DohvatiMeta_PonovljenaMjesta_VracaJedinstvenaAbecedno()
    {
        // Arrange
        await Ubaci(
            Red("m1", mjestoId: 3, mjesto: "Zenica"),
            Red("m2", mjestoId: 1, mjesto: "Mostar"),
            Red("m3", mjestoId: 3, mjesto: "Zenica"),
            Red("m4", mjestoId: null, mjesto: null));

        // Act
        var meta = await _repo.DohvatiMeta(Godina, Mjesec);

        // Assert
        CollectionAssert.AreEqual(new[] { "Mostar", "Zenica" }, meta.Lokacije.Select(l => l.Naziv).ToList());
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ZamijeniSnapshot_PostojeciMjesec_BriseStareIUpisujeNoveRedove()
    {
        // Arrange
        using var sqlite = new SqliteTestBaza();
        await using (var db = sqlite.NoviKontekst())
        {
            db.MjesecnaStatistikaKompozicija.AddRange(Red("stari1"), Red("stari2"), Red("drugi", mjesec: 10));
            await db.SaveChangesAsync();
        }
        await using var kontekst = sqlite.NoviKontekst();
        var repo = new MjesecnaStatistikaRepository(kontekst, _sort);

        // Act
        await repo.ZamijeniSnapshot(Godina, Mjesec, new[] { Red("novi") });

        // Assert
        await using var provjera = sqlite.NoviKontekst();
        CollectionAssert.AreEquivalent(new[] { "novi", "drugi" },
            provjera.MjesecnaStatistikaKompozicija.Select(r => r.IzvrsilacID).ToList());
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ZamijeniSnapshot_BezNovihRedova_SamoBriseStareRedove()
    {
        // Arrange
        using var sqlite = new SqliteTestBaza();
        await using (var db = sqlite.NoviKontekst())
        {
            db.MjesecnaStatistikaKompozicija.Add(Red("stari"));
            await db.SaveChangesAsync();
        }
        await using var kontekst = sqlite.NoviKontekst();
        var repo = new MjesecnaStatistikaRepository(kontekst, _sort);

        // Act
        await repo.ZamijeniSnapshot(Godina, Mjesec, Array.Empty<MjesecnaStatistikaKompozicija>());

        // Assert
        await using var provjera = sqlite.NoviKontekst();
        Assert.AreEqual(0, provjera.MjesecnaStatistikaKompozicija.Count());
    }
}
