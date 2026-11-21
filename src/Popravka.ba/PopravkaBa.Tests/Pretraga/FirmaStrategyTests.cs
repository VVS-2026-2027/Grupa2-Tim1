using PopravkaBa.Application.DTOs;
using PopravkaBa.Application.Strategies.Implementation;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Models;
using PopravkaBa.Domain.Specifications.Interface;
using static PopravkaBa.Tests.Pretraga.PretragaStrategijaTestHelper;

namespace PopravkaBa.Tests.Pretraga;

// Strategija pretrage sklapa specifikaciju od filtera. Testira se tako da se specifikacija primijeni
// na listu objekata u memoriji i provjeri koji su objekti ostali.
[TestClass]
public class FirmaStrategyTests
{
    private FirmaStrategy _strategija = null!;

    [TestInitialize]
    public void Postavi() => _strategija = new FirmaStrategy();

    // ---------- Pomoćne metode (priprema podataka) ----------

    // ---------- Osnovni podaci strategije ----------

    [TestMethod]
    public void DajUlogu_Poziv_VracaFirma()
    {
        // Act
        var uloga = _strategija.DajUlogu();

        // Assert
        Assert.AreEqual(KorisnickeUloge.Firma, uloga);
    }

    [TestMethod]
    public void DajDozvoljeneTabove_Poziv_VracaSveTriTaba()
    {
        // Act
        var tabovi = _strategija.DajDozvoljeneTabove().ToList();

        // Assert
        CollectionAssert.AreEqual(new[] { "Izvrsioci usluga", "Oglasi za popravke", "Oglasi za posao" }, tabovi);
    }

    [TestMethod]
    public void DajDefaultniTab_Poziv_VracaIzvrsiociUsluga()
    {
        // Act
        var tab = _strategija.DajDefaultniTab();

        // Assert
        Assert.AreEqual("Izvrsioci usluga", tab);
    }

    // ---------- Pogrešan tab ----------

    [TestMethod]
    public void NapraviIzvrsilacUslugeSpec_PogresanTab_VracaNull()
    {
        // Act
        var specifikacija = _strategija.NapraviIzvrsilacUslugeSpec(FilterUsluga());

        // Assert
        Assert.IsNull(specifikacija);
    }

    [TestMethod]
    public void NapraviRadnoMjestoSpec_PogresanTab_VracaNull()
    {
        // Act
        var specifikacija = _strategija.NapraviRadnoMjestoSpec(FilterIzvrsilaca());

        // Assert
        Assert.IsNull(specifikacija);
    }

    [TestMethod]
    public void NapraviUslugeSpec_PogresanTab_VracaNull()
    {
        // Act
        var specifikacija = _strategija.NapraviUslugeSpec(FilterRadnihMjesta());

        // Assert
        Assert.IsNull(specifikacija);
    }

    // ---------- Izvršioci usluga ----------

    [TestMethod]
    public void NapraviIzvrsilacUslugeSpec_BezFiltera_VracaSveIzvrsioce()
    {
        // Act
        var rezultat = Primijeni(_strategija.NapraviIzvrsilacUslugeSpec(FilterIzvrsilaca()), Izvrsioci());

        // Assert
        Assert.AreEqual(4, rezultat.Count);
    }

    [TestMethod]
    public void NapraviIzvrsilacUslugeSpec_FilterKategorije_VracaIzvrsiocePoKategoriji()
    {
        // Arrange
        var filter = FilterIzvrsilaca();
        filter.KategorijaId = new List<int> { 2 };

        // Act
        var rezultat = Primijeni(_strategija.NapraviIzvrsilacUslugeSpec(filter), Izvrsioci());

        // Assert
        CollectionAssert.AreEquivalent(new[] { "m2", "f1" }, IdIzvrsilaca(rezultat));
    }

    [TestMethod]
    public void NapraviIzvrsilacUslugeSpec_FilterLokacije_VracaIzvrsiocePoMjestu()
    {
        // Arrange
        var filter = FilterIzvrsilaca();
        filter.Lokacija = new List<int> { 1 };

        // Act
        var rezultat = Primijeni(_strategija.NapraviIzvrsilacUslugeSpec(filter), Izvrsioci());

        // Assert
        CollectionAssert.AreEquivalent(new[] { "m1", "f1" }, IdIzvrsilaca(rezultat));
    }

    [TestMethod]
    public void NapraviIzvrsilacUslugeSpec_KljucnaRijecUOpisu_VracaIzvrsiocaSaTimOpisom()
    {
        // Arrange
        var filter = FilterIzvrsilaca();
        filter.KljucneRijeci = "VODOINSTALATER";

        // Act
        var rezultat = Primijeni(_strategija.NapraviIzvrsilacUslugeSpec(filter), Izvrsioci());

        // Assert
        CollectionAssert.AreEquivalent(new[] { "m1" }, IdIzvrsilaca(rezultat));
    }

    [TestMethod]
    public void NapraviIzvrsilacUslugeSpec_KljucnaRijecUNazivuFirme_VracaFirmu()
    {
        // Arrange
        var filter = FilterIzvrsilaca();
        filter.KljucneRijeci = "elektro servis";

        // Act
        var rezultat = Primijeni(_strategija.NapraviIzvrsilacUslugeSpec(filter), Izvrsioci());

        // Assert
        CollectionAssert.AreEquivalent(new[] { "f1" }, IdIzvrsilaca(rezultat));
    }

    [TestMethod]
    public void NapraviIzvrsilacUslugeSpec_KljucnaRijecImeIPrezime_VracaMajstora()
    {
        // Arrange
        var filter = FilterIzvrsilaca();
        filter.KljucneRijeci = "suljo suljić";

        // Act
        var rezultat = Primijeni(_strategija.NapraviIzvrsilacUslugeSpec(filter), Izvrsioci());

        // Assert
        CollectionAssert.AreEquivalent(new[] { "m3" }, IdIzvrsilaca(rezultat));
    }

    public static IEnumerable<object[]> BudzetiIzvrsilaca =>
        DataDrivenInputParser.CitajXml<int?, int?, int>("TestData/xml/Strategije_budzetIzvrsioci.xml");

    [TestMethod]
    [DynamicData(nameof(BudzetiIzvrsilaca))]
    public void NapraviIzvrsilacUslugeSpec_BudzetIzXml_VracaOcekivaniBrojIzvrsilaca(
        int? minBudzet, int? maxBudzet, int ocekivano)
    {
        // Arrange
        var filter = FilterIzvrsilaca();
        filter.MinBudzet = minBudzet;
        filter.MaxBudzet = maxBudzet;

        // Act
        var rezultat = Primijeni(_strategija.NapraviIzvrsilacUslugeSpec(filter), Izvrsioci());

        // Assert
        Assert.AreEqual(ocekivano, rezultat.Count);
    }

    [TestMethod]
    public void NapraviIzvrsilacUslugeSpec_ObjeGraniceOcjene_VracaIzvrsiocePrekoMinimalneOcjene()
    {
        // Arrange
        var filter = FilterIzvrsilaca();
        filter.MinOcjena = 3.5m;
        filter.MaxOcjena = 5m;

        // Act
        var rezultat = Primijeni(_strategija.NapraviIzvrsilacUslugeSpec(filter), Izvrsioci());

        // Assert
        CollectionAssert.AreEquivalent(new[] { "m2", "f1" }, IdIzvrsilaca(rezultat));
    }

    [TestMethod]
    public void NapraviIzvrsilacUslugeSpec_SamoMinimalnaOcjena_NeFiltrira()
    {
        // Arrange
        var filter = FilterIzvrsilaca();
        filter.MinOcjena = 4m;

        // Act
        var rezultat = Primijeni(_strategija.NapraviIzvrsilacUslugeSpec(filter), Izvrsioci());

        // Assert
        Assert.AreEqual(4, rezultat.Count);
    }

    public static IEnumerable<object[]> SortiranjaIzvrsilaca =>
        DataDrivenInputParser.CitajXml<SortiranjeIzvrsilacaUsluga, bool>("TestData/xml/Strategije_sortiranjeIzvrsioci.xml");

    [TestMethod]
    [DynamicData(nameof(SortiranjaIzvrsilaca))]
    public void NapraviIzvrsilacUslugeSpec_SortiranjeIzXml_PostavljaSmjerSortiranja(
        SortiranjeIzvrsilacaUsluga sortiranje, bool silazno)
    {
        // Arrange
        var filter = FilterIzvrsilaca();
        filter.SortiranjeIzvrsilaca = sortiranje;

        // Act
        var specifikacija = _strategija.NapraviIzvrsilacUslugeSpec(filter)!;

        // Assert
        Assert.AreEqual(silazno, specifikacija.OrderByDescending is not null);
        Assert.AreEqual(!silazno, specifikacija.OrderBy is not null);
    }

    [TestMethod]
    public void NapraviIzvrsilacUslugeSpec_BezSortiranja_KoristiOcjenuOpadajuce()
    {
        // Arrange
        var filter = FilterIzvrsilaca();
        filter.SortiranjeIzvrsilaca = null;

        // Act
        var specifikacija = _strategija.NapraviIzvrsilacUslugeSpec(filter)!;

        // Assert
        Assert.IsNotNull(specifikacija.OrderByDescending);
    }

    // ---------- Oglasi za popravke (usluge) ----------

    [TestMethod]
    public void NapraviUslugeSpec_BezFiltera_VracaSamoAktivneOglase()
    {
        // Act
        var rezultat = Primijeni(_strategija.NapraviUslugeSpec(FilterUsluga()), OglasiUsluga());

        // Assert
        CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, IdOglasa(rezultat));
    }

    [TestMethod]
    public void NapraviUslugeSpec_FilterKategorije_VracaOglasePoKategoriji()
    {
        // Arrange
        var filter = FilterUsluga();
        filter.KategorijaId = new List<int> { 2 };

        // Act
        var rezultat = Primijeni(_strategija.NapraviUslugeSpec(filter), OglasiUsluga());

        // Assert
        CollectionAssert.AreEquivalent(new[] { 2, 3 }, IdOglasa(rezultat));
    }

    [TestMethod]
    public void NapraviUslugeSpec_FilterLokacije_VracaAktivneOglasePoMjestu()
    {
        // Arrange
        var filter = FilterUsluga();
        filter.Lokacija = new List<int> { 1 };

        // Act
        var rezultat = Primijeni(_strategija.NapraviUslugeSpec(filter), OglasiUsluga());

        // Assert
        CollectionAssert.AreEquivalent(new[] { 1, 3 }, IdOglasa(rezultat));
    }

    [TestMethod]
    public void NapraviUslugeSpec_KljucnaRijecUNaslovu_VracaOglasSaTimNaslovom()
    {
        // Arrange
        var filter = FilterUsluga();
        filter.KljucneRijeci = "SLAVINE";

        // Act
        var rezultat = Primijeni(_strategija.NapraviUslugeSpec(filter), OglasiUsluga());

        // Assert
        CollectionAssert.AreEquivalent(new[] { 1 }, IdOglasa(rezultat));
    }

    [TestMethod]
    public void NapraviUslugeSpec_KljucnaRijecUOpisu_VracaOglasSaTimOpisom()
    {
        // Arrange
        var filter = FilterUsluga();
        filter.KljucneRijeci = "okrečiti";

        // Act
        var rezultat = Primijeni(_strategija.NapraviUslugeSpec(filter), OglasiUsluga());

        // Assert
        CollectionAssert.AreEquivalent(new[] { 2 }, IdOglasa(rezultat));
    }

    public static IEnumerable<object[]> BudzetiUsluga =>
        DataDrivenInputParser.CitajXml<int?, int?, int>("TestData/xml/Strategije_budzetOglasiUsluga.xml");

    [TestMethod]
    [DynamicData(nameof(BudzetiUsluga))]
    public void NapraviUslugeSpec_BudzetIzXml_VracaOcekivaniBrojOglasa(int? minBudzet, int? maxBudzet, int ocekivano)
    {
        // Arrange
        var filter = FilterUsluga();
        filter.MinBudzet = minBudzet;
        filter.MaxBudzet = maxBudzet;

        // Act
        var rezultat = Primijeni(_strategija.NapraviUslugeSpec(filter), OglasiUsluga());

        // Assert
        Assert.AreEqual(ocekivano, rezultat.Count);
    }

    public static IEnumerable<object[]> SortiranjaUsluga =>
        DataDrivenInputParser.CitajXml<SortiranjeOglasaUsluge, bool>("TestData/xml/Strategije_sortiranjeOglasiUsluga.xml");

    [TestMethod]
    [DynamicData(nameof(SortiranjaUsluga))]
    public void NapraviUslugeSpec_SortiranjeIzXml_PostavljaSmjerSortiranja(SortiranjeOglasaUsluge sortiranje, bool silazno)
    {
        // Arrange
        var filter = FilterUsluga();
        filter.SortiranjeUsluge = sortiranje;

        // Act
        var specifikacija = _strategija.NapraviUslugeSpec(filter)!;

        // Assert
        Assert.AreEqual(silazno, specifikacija.OrderByDescending is not null);
        Assert.AreEqual(!silazno, specifikacija.OrderBy is not null);
    }

    [TestMethod]
    public void NapraviUslugeSpec_BezSortiranja_KoristiMinimalniBudzetOpadajuce()
    {
        // Arrange
        var filter = FilterUsluga();
        filter.SortiranjeUsluge = null;

        // Act
        var specifikacija = _strategija.NapraviUslugeSpec(filter)!;

        // Assert
        Assert.IsNotNull(specifikacija.OrderByDescending);
    }

    // ---------- Oglasi za posao ----------

    [TestMethod]
    public void NapraviRadnoMjestoSpec_BezFiltera_VracaSamoAktivneOglase()
    {
        // Act
        var rezultat = Primijeni(_strategija.NapraviRadnoMjestoSpec(FilterRadnihMjesta()), OglasiRadnihMjesta());

        // Assert
        CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, IdOglasa(rezultat));
    }

    [TestMethod]
    public void NapraviRadnoMjestoSpec_FilterKategorije_VracaOglasePoKategoriji()
    {
        // Arrange
        var filter = FilterRadnihMjesta();
        filter.KategorijaId = new List<int> { 1 };

        // Act
        var rezultat = Primijeni(_strategija.NapraviRadnoMjestoSpec(filter), OglasiRadnihMjesta());

        // Assert
        CollectionAssert.AreEquivalent(new[] { 1, 3 }, IdOglasa(rezultat));
    }

    [TestMethod]
    public void NapraviRadnoMjestoSpec_FilterLokacije_VracaOglasePoMjestu()
    {
        // Arrange
        var filter = FilterRadnihMjesta();
        filter.Lokacija = new List<int> { 2 };

        // Act
        var rezultat = Primijeni(_strategija.NapraviRadnoMjestoSpec(filter), OglasiRadnihMjesta());

        // Assert
        CollectionAssert.AreEquivalent(new[] { 2 }, IdOglasa(rezultat));
    }

    [TestMethod]
    public void NapraviRadnoMjestoSpec_KljucnaRijecUNaslovu_VracaOglasSaTimNaslovom()
    {
        // Arrange
        var filter = FilterRadnihMjesta();
        filter.KljucneRijeci = "POMOĆNI";

        // Act
        var rezultat = Primijeni(_strategija.NapraviRadnoMjestoSpec(filter), OglasiRadnihMjesta());

        // Assert
        CollectionAssert.AreEquivalent(new[] { 3 }, IdOglasa(rezultat));
    }

    [TestMethod]
    public void NapraviRadnoMjestoSpec_KljucnaRijecUOpisu_VracaOglasSaTimOpisom()
    {
        // Arrange
        var filter = FilterRadnihMjesta();
        filter.KljucneRijeci = "gradilištu";

        // Act
        var rezultat = Primijeni(_strategija.NapraviRadnoMjestoSpec(filter), OglasiRadnihMjesta());

        // Assert
        CollectionAssert.AreEquivalent(new[] { 3 }, IdOglasa(rezultat));
    }

    public static IEnumerable<object[]> IskustvaRadnihMjesta =>
        DataDrivenInputParser.CitajXml<int?, int>("TestData/xml/Strategije_iskustvoRadnaMjesta.xml");

    [TestMethod]
    [DynamicData(nameof(IskustvaRadnihMjesta))]
    public void NapraviRadnoMjestoSpec_GodineIskustvaIzXml_VracaOcekivaniBrojOglasa(int? godine, int ocekivano)
    {
        // Arrange
        var filter = FilterRadnihMjesta();
        filter.GodineIskustva = godine;

        // Act
        var rezultat = Primijeni(_strategija.NapraviRadnoMjestoSpec(filter), OglasiRadnihMjesta());

        // Assert
        Assert.AreEqual(ocekivano, rezultat.Count);
    }

    [TestMethod]
    public void NapraviRadnoMjestoSpec_TipZaposlenja_VracaOglaseSaTimTipom()
    {
        // Arrange
        var filter = FilterRadnihMjesta();
        filter.TipZaposlenja = VrstaZaposlenja.PolaRadnogVrijema;

        // Act
        var rezultat = Primijeni(_strategija.NapraviRadnoMjestoSpec(filter), OglasiRadnihMjesta());

        // Assert
        CollectionAssert.AreEquivalent(new[] { 2 }, IdOglasa(rezultat));
    }

    public static IEnumerable<object[]> PrihodiRadnihMjesta =>
        DataDrivenInputParser.CitajXml<int?, int?, int>("TestData/xml/Strategije_budzetRadnaMjesta.xml");

    [TestMethod]
    [DynamicData(nameof(PrihodiRadnihMjesta))]
    public void NapraviRadnoMjestoSpec_PrihodIzXml_VracaOcekivaniBrojOglasa(int? minPrihod, int? maxPrihod, int ocekivano)
    {
        // Arrange
        var filter = FilterRadnihMjesta();
        filter.MinBudzet = minPrihod;
        filter.MaxBudzet = maxPrihod;

        // Act
        var rezultat = Primijeni(_strategija.NapraviRadnoMjestoSpec(filter), OglasiRadnihMjesta());

        // Assert
        Assert.AreEqual(ocekivano, rezultat.Count);
    }

    public static IEnumerable<object[]> SortiranjaRadnihMjesta =>
        DataDrivenInputParser.CitajXml<SortiranjeOglasaRadnoMjesto, bool>("TestData/xml/Strategije_sortiranjeRadnaMjesta.xml");

    [TestMethod]
    [DynamicData(nameof(SortiranjaRadnihMjesta))]
    public void NapraviRadnoMjestoSpec_SortiranjeIzXml_PostavljaSmjerSortiranja(
        SortiranjeOglasaRadnoMjesto sortiranje, bool silazno)
    {
        // Arrange
        var filter = FilterRadnihMjesta();
        filter.SortiranjeRadnoMjesto = sortiranje;

        // Act
        var specifikacija = _strategija.NapraviRadnoMjestoSpec(filter)!;

        // Assert
        Assert.AreEqual(silazno, specifikacija.OrderByDescending is not null);
        Assert.AreEqual(!silazno, specifikacija.OrderBy is not null);
    }

    [TestMethod]
    public void NapraviRadnoMjestoSpec_BezSortiranja_KoristiMinimalniPrihodOpadajuce()
    {
        // Arrange
        var filter = FilterRadnihMjesta();
        filter.SortiranjeRadnoMjesto = null;

        // Act
        var specifikacija = _strategija.NapraviRadnoMjestoSpec(filter)!;

        // Assert
        Assert.IsNotNull(specifikacija.OrderByDescending);
    }
}
