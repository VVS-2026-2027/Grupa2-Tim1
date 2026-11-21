using PopravkaBa.Application.DTOs;
using PopravkaBa.Application.Strategies.Implementation;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Models;
using static PopravkaBa.Tests.Pretraga.StrategijaTestHelper;

namespace PopravkaBa.Tests.Pretraga;

[TestClass]
public class MajstorStrategyTests
{
    private const string IzvrsiociTab = "Izvrsioci usluga";
    private const string PopravkeTab = "Oglasi za popravke";
    private const string PosaoTab = "Oglasi za posao";

    private readonly MajstorStrategy _strategija = new();

    [TestMethod]
    public void PostavkeStrategije_Majstor_VracajuUloguIDozvoljeneTabove()
    {
        // Act
        var uloga = _strategija.DajUlogu();
        var tabovi = _strategija.DajDozvoljeneTabove().ToArray();
        var zadaniTab = _strategija.DajDefaultniTab();

        // Assert
        Assert.AreEqual(KorisnickeUloge.Majstor, uloga);
        CollectionAssert.AreEqual(new[] { IzvrsiociTab, PopravkeTab, PosaoTab }, tabovi);
        Assert.AreEqual(IzvrsiociTab, zadaniTab);
    }

    [TestMethod]
    public void Specifikacije_NepoznatTab_VracajuNull()
    {
        // Arrange
        var filteri = new FilterPretrageDto { AktivniTab = "Nepoznat tab" };

        // Act
        var izvrsioci = _strategija.NapraviIzvrsilacUslugeSpec(filteri);
        var popravke = _strategija.NapraviUslugeSpec(filteri);
        var posao = _strategija.NapraviRadnoMjestoSpec(filteri);

        // Assert
        Assert.IsNull(izvrsioci);
        Assert.IsNull(popravke);
        Assert.IsNull(posao);
    }

    [TestMethod]
    [DataRow(null, null, "m1,m2,m3")]
    [DataRow(30, null, "m2,m3")]
    [DataRow(null, 50, "m1,m2")]
    [DataRow(30, 50, "m2")]
    public void NapraviIzvrsilacUslugeSpec_GraniceBudzeta_FiltriraPoCijeni(
        int? minimum, int? maksimum, string ocekivaniId)
    {
        // Arrange
        var filteri = new FilterPretrageDto
        {
            AktivniTab = IzvrsiociTab,
            MinBudzet = minimum,
            MaxBudzet = maksimum,
            SortiranjeIzvrsilaca = null
        };
        IzvrsilacUsluge[] podaci =
        {
            NoviMajstor("m1", 20, 3m),
            NoviMajstor("m2", 40, 4m),
            NoviMajstor("m3", 60, 5m)
        };

        // Act
        var spec = _strategija.NapraviIzvrsilacUslugeSpec(filteri);

        // Assert
        Assert.IsNotNull(spec);
        var podudarni = podaci.Where(spec.ToExpression().Compile())
            .Select(m => m.Id).ToArray();
        CollectionAssert.AreEqual(ocekivaniId.Split(','), podudarni);
        Assert.IsNull(spec.OrderBy);
        Assert.IsNotNull(spec.OrderByDescending);
        var kljucSortiranja = spec.OrderByDescending!.Compile();
        Assert.AreEqual(5m, (decimal)kljucSortiranja(podaci[2]));
    }

    [TestMethod]
    public void NapraviIzvrsilacUslugeSpec_ViseFiltera_PrimjenjujeSveUsloveICijenuRastuce()
    {
        // Arrange
        var filteri = new FilterPretrageDto
        {
            AktivniTab = IzvrsiociTab,
            KategorijaId = new List<int> { 1 },
            Lokacija = new List<int> { 1 },
            KljucneRijeci = "amar",
            MinOcjena = 4m,
            MaxOcjena = 5m,
            SortiranjeIzvrsilaca = SortiranjeIzvrsilacaUsluga.MinCijena_Asc
        };
        IzvrsilacUsluge[] podaci =
        {
            NoviMajstor("m1", 40, 4.5m),
            NoviMajstor("m2", 35, 4m, ime: "Tarik", opis: "Tim Amar"),
            NoviMajstor("m3", 30, 4.5m, kategorija: 2),
            NoviMajstor("m4", 30, 4.5m, mjesto: 2),
            NoviMajstor("m5", 30, 4.5m, ime: "Tarik"),
            NoviMajstor("m6", 30, 3m)
        };

        // Act
        var spec = _strategija.NapraviIzvrsilacUslugeSpec(filteri);

        // Assert
        Assert.IsNotNull(spec);
        Assert.IsNotNull(spec.OrderBy);
        Assert.IsNull(spec.OrderByDescending);
        var podudarni = podaci.Where(spec.ToExpression().Compile())
            .OrderBy(spec.OrderBy!.Compile())
            .Select(m => m.Id).ToArray();
        CollectionAssert.AreEqual(new[] { "m2", "m1" }, podudarni);
    }

    [TestMethod]
    [DataRow(null, null, "1,2,3")]
    [DataRow(30, null, "2,3")]
    [DataRow(null, 40, "1,2")]
    [DataRow(35, 45, "2")]
    public void NapraviUslugeSpec_GraniceBudzeta_FiltriraAktivneOglase(
        int? minimum, int? maksimum, string ocekivaniId)
    {
        // Arrange
        var filteri = new FilterPretrageDto
        {
            AktivniTab = PopravkeTab,
            MinBudzet = minimum,
            MaxBudzet = maksimum,
            SortiranjeUsluge = null
        };
        var podaci = new[]
        {
            NoviOglas(1, 10, 20),
            NoviOglas(2, 30, 40),
            NoviOglas(3, 50, 60),
            NoviOglas(4, 30, 40, status: Status.Neaktivan)
        };

        // Act
        var spec = _strategija.NapraviUslugeSpec(filteri);

        // Assert
        Assert.IsNotNull(spec);
        var podudarni = podaci.Where(spec.ToExpression().Compile())
            .Select(o => o.OglasID).ToArray();
        CollectionAssert.AreEqual(
            ocekivaniId.Split(',').Select(int.Parse).ToArray(), podudarni);
        Assert.IsNull(spec.OrderBy);
        Assert.IsNotNull(spec.OrderByDescending);
        var kljucSortiranja = spec.OrderByDescending!.Compile();
        Assert.AreEqual(50, (int)kljucSortiranja(podaci[2]));
    }

    [TestMethod]
    public void NapraviUslugeSpec_ViseFiltera_PrimjenjujeSveUsloveIMaksimalniBudzetRastuce()
    {
        // Arrange
        var filteri = new FilterPretrageDto
        {
            AktivniTab = PopravkeTab,
            KategorijaId = new List<int> { 1 },
            Lokacija = new List<int> { 1 },
            KljucneRijeci = "slavina",
            SortiranjeUsluge = SortiranjeOglasaUsluge.MaxBudzet_Asc
        };
        var podaci = new[]
        {
            NoviOglas(1, 10, 80, naslov: "Slavina"),
            NoviOglas(2, 20, 50, opis: "Stara slavina curi"),
            NoviOglas(3, 10, 40, kategorija: 2, naslov: "Slavina"),
            NoviOglas(4, 10, 40, mjesto: 2, naslov: "Slavina"),
            NoviOglas(5, 10, 40)
        };

        // Act
        var spec = _strategija.NapraviUslugeSpec(filteri);

        // Assert
        Assert.IsNotNull(spec);
        Assert.IsNotNull(spec.OrderBy);
        Assert.IsNull(spec.OrderByDescending);
        var podudarni = podaci.Where(spec.ToExpression().Compile())
            .OrderBy(spec.OrderBy!.Compile())
            .Select(o => o.OglasID).ToArray();
        CollectionAssert.AreEqual(new[] { 2, 1 }, podudarni);
    }

    [TestMethod]
    [DataRow(null, null, "1,2,3")]
    [DataRow(900, null, "2,3")]
    [DataRow(null, 1200, "1,2")]
    [DataRow(1000, 1300, "2")]
    public void NapraviRadnoMjestoSpec_GranicePrihoda_FiltriraAktivneOglase(
        int? minimum, int? maksimum, string ocekivaniId)
    {
        // Arrange
        var filteri = new FilterPretrageDto
        {
            AktivniTab = PosaoTab,
            MinBudzet = minimum,
            MaxBudzet = maksimum,
            SortiranjeRadnoMjesto = null
        };
        var oglasi = new[]
        {
            NovoRadnoMjesto(1, 500, 800),
            NovoRadnoMjesto(2, 900, 1200),
            NovoRadnoMjesto(3, 1400, 1800),
            NovoRadnoMjesto(4, 950, 1100, status: Status.Neaktivan)
        };

        // Act
        var spec = _strategija.NapraviRadnoMjestoSpec(filteri);

        // Assert
        Assert.IsNotNull(spec);
        var podudarni = oglasi.Where(spec.ToExpression().Compile())
            .Select(o => o.OglasID).ToArray();
        CollectionAssert.AreEqual(
            ocekivaniId.Split(',').Select(int.Parse).ToArray(), podudarni);
        Assert.IsNull(spec.OrderBy);
        Assert.IsNotNull(spec.OrderByDescending);
        Assert.AreEqual(1400, (int)spec.OrderByDescending!.Compile()(oglasi[2]));
    }

    [TestMethod]
    public void NapraviRadnoMjestoSpec_ViseFiltera_PrimjenjujeUsloveISortiraPoPrihodu()
    {
        // Arrange
        var filteri = new FilterPretrageDto
        {
            AktivniTab = PosaoTab,
            KategorijaId = new List<int> { 1 },
            Lokacija = new List<int> { 1 },
            KljucneRijeci = "elektro",
            GodineIskustva = 2,
            TipZaposlenja = VrstaZaposlenja.PunoRadnoVrijeme,
            SortiranjeRadnoMjesto = SortiranjeOglasaRadnoMjesto.MaxPrihod_Asc
        };
        var oglasi = new[]
        {
            NovoRadnoMjesto(1, 900, 1500),
            NovoRadnoMjesto(2, 800, 1200, naslov: "Serviser", opis: "Elektro radovi"),
            NovoRadnoMjesto(3, 800, 1000, kategorija: 2),
            NovoRadnoMjesto(4, 800, 1000, mjesto: 2),
            NovoRadnoMjesto(5, 800, 1000, naslov: "Vodoinstalater"),
            NovoRadnoMjesto(6, 800, 1000, iskustvo: 1),
            NovoRadnoMjesto(7, 800, 1000, tip: VrstaZaposlenja.Praksa)
        };

        // Act
        var spec = _strategija.NapraviRadnoMjestoSpec(filteri);

        // Assert
        Assert.IsNotNull(spec);
        Assert.IsNotNull(spec.OrderBy);
        Assert.IsNull(spec.OrderByDescending);
        var podudarni = oglasi.Where(spec.ToExpression().Compile())
            .OrderBy(spec.OrderBy!.Compile())
            .Select(o => o.OglasID).ToArray();
        CollectionAssert.AreEqual(new[] { 2, 1 }, podudarni);
    }
}
