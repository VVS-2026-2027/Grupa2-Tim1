using PopravkaBa.Application.DTOs;
using PopravkaBa.Application.Services.Implementation;
using PopravkaBa.Application.Strategies.Interface;
using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Models;
using PopravkaBa.Domain.Specifications.Interface;
using PopravkaBa.Infrastructure.Wrappers;
using static PopravkaBa.Tests.Pretraga.PretragaServiceTestHelper;

namespace PopravkaBa.Tests.Pretraga;

[TestClass]
public class PretragaServiceTests
{
    private Mock<IIzvrsilacUslugeRepository> _izvrsioci = null!;
    private Mock<IOglasUslugeRepository> _usluge = null!;
    private Mock<IOglasRadnoMjestoRepository> _radnaMjesta = null!;
    private Mock<IPretragaStrategy> _strategija = null!;
    private PretragaService _servis = null!;

    [TestInitialize]
    public void PripremiTest()
    {
        _izvrsioci = new Mock<IIzvrsilacUslugeRepository>();
        _usluge = new Mock<IOglasUslugeRepository>();
        _radnaMjesta = new Mock<IOglasRadnoMjestoRepository>();
        _strategija = new Mock<IPretragaStrategy>();
        _strategija.Setup(s => s.DajDozvoljeneTabove())
            .Returns(new[] { "izvrsioci", "usluge", "radnomjesto" });

        _servis = new PretragaService(
            _izvrsioci.Object, _usluge.Object, _radnaMjesta.Object);
    }

    // ---------- Bez taba i specifikacije ----------

    [TestMethod]
    public async Task PretraziAsync_BezTaba_KoristiZadaniTabStrategije()
    {
        // Arrange
        var filteri = new FilterPretrageDto { AktivniTab = null };
        _strategija.Setup(s => s.DajDefaultniTab()).Returns("izvrsioci");

        // Act
        var rezultat = await _servis.PretraziAsync(filteri, _strategija.Object);

        // Assert
        Assert.AreEqual("izvrsioci", rezultat.AktivniTab);
        CollectionAssert.AreEqual(
            new[] { "izvrsioci", "usluge", "radnomjesto" },
            rezultat.DostupniTabovi.ToArray());
        _strategija.Verify(s => s.DajDefaultniTab(), Times.Once());
    }

    [TestMethod]
    public async Task PretraziAsync_NeispravnaStranicaIVelicina_KoristiPrvuStranicuOdDvadesetStavki()
    {
        // Arrange
        var filteri = new FilterPretrageDto { AktivniTab = null, Stranica = -2, StavkiPoStranici = 0 };
        _strategija.Setup(s => s.DajDefaultniTab()).Returns("izvrsioci");

        // Act
        var rezultat = await _servis.PretraziAsync(filteri, _strategija.Object);

        // Assert
        Assert.AreEqual(1, rezultat.TrenutnaStrana);
        Assert.AreEqual(20, rezultat.StavkiPoStranici);
    }

    [TestMethod]
    public async Task PretraziAsync_StrategijaBezSpecifikacije_VracaPrazneRezultateBezPozivaRepozitorija()
    {
        // Arrange: mock strategije ne vraća nijednu specifikaciju
        var filteri = new FilterPretrageDto { AktivniTab = null };
        _strategija.Setup(s => s.DajDefaultniTab()).Returns("izvrsioci");

        // Act
        var rezultat = await _servis.PretraziAsync(filteri, _strategija.Object);

        // Assert
        Assert.AreEqual(0, rezultat.UkupnoStavki);
        Assert.IsFalse(rezultat.Izvrsioci.Any());
        Assert.IsFalse(rezultat.OglasiUsluga.Any());
        Assert.IsFalse(rezultat.OglasiRadnihMjesta.Any());
        _izvrsioci.VerifyNoOtherCalls();
        _usluge.VerifyNoOtherCalls();
        _radnaMjesta.VerifyNoOtherCalls();
    }

    // ---------- Izvršioci ----------

    [TestMethod]
    public async Task PretraziAsync_SpecifikacijaIzvrsilaca_MapiraPoljaIzvrsilaca()
    {
        // Arrange
        var filteri = NoviFilter("izvrsioci");
        var (_, majstor) = PostaviStranicuIzvrsilaca(_strategija, _izvrsioci, filteri);

        // Act
        var rezultat = await _servis.PretraziAsync(filteri, _strategija.Object);

        // Assert
        var stavke = rezultat.Izvrsioci.ToArray();
        Assert.AreEqual(2, stavke.Length);
        Assert.AreEqual("m1", stavke[0].Id);
        Assert.AreEqual("majstor1", stavke[0].Username);
        Assert.AreEqual(majstor.DisplayName, stavke[0].DisplayName);
        Assert.AreEqual(SkraceniOpis, stavke[0].Opis);
        Assert.AreEqual(4.5m, stavke[0].ProsjecnaOcjena);
        Assert.AreEqual((int?)40, stavke[0].MinCijenaUsluge);
        Assert.AreEqual(6, stavke[0].BrojZavrsenihPoslova);
        Assert.AreEqual(4, stavke[0].BrojRecenzija);
        Assert.AreEqual("Sarajevo", stavke[0].Lokacija);
        Assert.AreEqual("Vodoinstalacije", stavke[0].PrvaKategorija);
        Assert.IsNull(stavke[1].Opis);
        Assert.IsNull(stavke[1].Lokacija);
        Assert.IsNull(stavke[1].PrvaKategorija);
    }

    [TestMethod]
    public async Task PretraziAsync_SpecifikacijaIzvrsilaca_ProsljedjujeStranicuIVelicinu()
    {
        // Arrange
        var filteri = NoviFilter("izvrsioci");
        var (spec, _) = PostaviStranicuIzvrsilaca(_strategija, _izvrsioci, filteri);

        // Act
        var rezultat = await _servis.PretraziAsync(filteri, _strategija.Object);

        // Assert
        ProvjeriPaginaciju(rezultat, "izvrsioci");
        _izvrsioci.Verify(r => r.PronadjiAsync(spec, 2, 5), Times.Once());
        _strategija.Verify(s => s.DajDefaultniTab(), Times.Never());
    }

    [TestMethod]
    public async Task PretraziAsync_SpecifikacijaIzvrsilaca_NePozivaOstaleRepozitorije()
    {
        // Arrange
        var filteri = NoviFilter("izvrsioci");
        PostaviStranicuIzvrsilaca(_strategija, _izvrsioci, filteri);

        // Act
        await _servis.PretraziAsync(filteri, _strategija.Object);

        // Assert
        _usluge.VerifyNoOtherCalls();
        _radnaMjesta.VerifyNoOtherCalls();
    }

    // ---------- Oglasi usluga ----------

    [TestMethod]
    public async Task PretraziAsync_SpecifikacijaUsluga_MapiraPoljaOglasaUsluga()
    {
        // Arrange
        var filteri = NoviFilter("usluge");
        var (_, vlasnik) = PostaviStranicuUsluga(_strategija, _usluge, filteri);

        // Act
        var rezultat = await _servis.PretraziAsync(filteri, _strategija.Object);

        // Assert
        var stavke = rezultat.OglasiUsluga.ToArray();
        Assert.AreEqual(2, stavke.Length);
        Assert.AreEqual(10, stavke[0].Id);
        Assert.AreEqual("Popravka slavine", stavke[0].Naslov);
        Assert.AreEqual(SkraceniOpis, stavke[0].Opis);
        Assert.AreEqual(1, stavke[0].BrojPonuda);
        Assert.AreEqual(30, stavke[0].MinBudzet);
        Assert.AreEqual(90, stavke[0].MaxBudzet);
        Assert.AreEqual(vlasnik.SkracenoIme, stavke[0].VlasnikSkracenoIme);
        Assert.AreEqual("profil.png", stavke[0].VlasnikSlika);
        Assert.AreEqual("Sarajevo", stavke[0].Lokacija);
        Assert.AreEqual("Kratak opis", stavke[1].Opis);
        Assert.IsNull(stavke[1].Lokacija);
    }

    [TestMethod]
    public async Task PretraziAsync_SpecifikacijaUsluga_ProsljedjujeStranicuIVelicinu()
    {
        // Arrange
        var filteri = NoviFilter("usluge");
        var (spec, _) = PostaviStranicuUsluga(_strategija, _usluge, filteri);

        // Act
        var rezultat = await _servis.PretraziAsync(filteri, _strategija.Object);

        // Assert
        ProvjeriPaginaciju(rezultat, "usluge");
        _usluge.Verify(r => r.PronadjiAsync(spec, 2, 5), Times.Once());
    }

    [TestMethod]
    public async Task PretraziAsync_SpecifikacijaUsluga_NePozivaOstaleRepozitorije()
    {
        // Arrange
        var filteri = NoviFilter("usluge");
        PostaviStranicuUsluga(_strategija, _usluge, filteri);

        // Act
        await _servis.PretraziAsync(filteri, _strategija.Object);

        // Assert
        _izvrsioci.VerifyNoOtherCalls();
        _radnaMjesta.VerifyNoOtherCalls();
    }

    // ---------- Radna mjesta ----------

    [TestMethod]
    public async Task PretraziAsync_SpecifikacijaRadnihMjesta_MapiraPoljaRadnihMjesta()
    {
        // Arrange
        var filteri = NoviFilter("radnomjesto");
        var (_, vlasnik) = PostaviStranicuRadnihMjesta(_strategija, _radnaMjesta, filteri);

        // Act
        var rezultat = await _servis.PretraziAsync(filteri, _strategija.Object);

        // Assert
        var stavke = rezultat.OglasiRadnihMjesta.ToArray();
        Assert.AreEqual(2, stavke.Length);
        Assert.AreEqual(20, stavke[0].Id);
        Assert.AreEqual("Vodoinstalater", stavke[0].Naslov);
        Assert.AreEqual(SkraceniOpis, stavke[0].Opis);
        Assert.AreEqual(1, stavke[0].BrojPrijava);
        Assert.AreEqual(800, stavke[0].MinPrihod);
        Assert.AreEqual(1200, stavke[0].MaxPrihod);
        Assert.AreEqual(vlasnik.DisplayName, stavke[0].VlasnikDisplayName);
        Assert.AreEqual("profil.png", stavke[0].VlasnikSlika);
        Assert.AreEqual("Sarajevo", stavke[0].Lokacija);
        Assert.AreEqual("Kratak opis", stavke[1].Opis);
        Assert.IsNull(stavke[1].Lokacija);
    }

    [TestMethod]
    public async Task PretraziAsync_SpecifikacijaRadnihMjesta_ProsljedjujeStranicuIVelicinu()
    {
        // Arrange
        var filteri = NoviFilter("radnomjesto");
        var (spec, _) = PostaviStranicuRadnihMjesta(_strategija, _radnaMjesta, filteri);

        // Act
        var rezultat = await _servis.PretraziAsync(filteri, _strategija.Object);

        // Assert
        ProvjeriPaginaciju(rezultat, "radnomjesto");
        _radnaMjesta.Verify(r => r.PronadjiAsync(spec, 2, 5), Times.Once());
    }

    [TestMethod]
    public async Task PretraziAsync_SpecifikacijaRadnihMjesta_NePozivaOstaleRepozitorije()
    {
        // Arrange
        var filteri = NoviFilter("radnomjesto");
        PostaviStranicuRadnihMjesta(_strategija, _radnaMjesta, filteri);

        // Act
        await _servis.PretraziAsync(filteri, _strategija.Object);

        // Assert
        _izvrsioci.VerifyNoOtherCalls();
        _usluge.VerifyNoOtherCalls();
    }
}
