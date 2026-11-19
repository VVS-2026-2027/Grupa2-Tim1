using PopravkaBa.Application.DTOs;
using PopravkaBa.Application.Services.Implementation;
using PopravkaBa.Application.Strategies.Interface;
using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Models;
using PopravkaBa.Domain.Specifications.Interface;
using PopravkaBa.Infrastructure.Wrappers;
using static PopravkaBa.Tests.Pretraga.PretragaServiceTestPomoc;

namespace PopravkaBa.Tests.Pretraga;

[TestClass]
public class PretragaServiceTests
{
    private static readonly string DugiOpis = new string('x', 160);
    private static readonly string SkraceniOpis = new string('x', 150) + "...";

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

    [TestMethod]
    public async Task PretraziAsync_BezTabaISpecifikacija_VracaZadanePostavke()
    {
        // Arrange
        var filteri = new FilterPretrageDto
        {
            AktivniTab = null,
            Stranica = -2,
            StavkiPoStranici = 0
        };
        _strategija.Setup(s => s.DajDefaultniTab()).Returns("izvrsioci");

        // Act
        var rezultat = await _servis.PretraziAsync(filteri, _strategija.Object);

        // Assert
        Assert.AreEqual("izvrsioci", rezultat.AktivniTab);
        Assert.AreEqual(1, rezultat.TrenutnaStrana);
        Assert.AreEqual(20, rezultat.StavkiPoStranici);
        Assert.AreEqual(0, rezultat.UkupnoStavki);
        CollectionAssert.AreEqual(
            new[] { "izvrsioci", "usluge", "radnomjesto" },
            rezultat.DostupniTabovi.ToArray());
        Assert.IsFalse(rezultat.Izvrsioci.Any());
        Assert.IsFalse(rezultat.OglasiUsluga.Any());
        Assert.IsFalse(rezultat.OglasiRadnihMjesta.Any());
        _strategija.Verify(s => s.DajDefaultniTab(), Times.Once());
        _izvrsioci.VerifyNoOtherCalls();
        _usluge.VerifyNoOtherCalls();
        _radnaMjesta.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task PretraziAsync_SpecifikacijaIzvrsilaca_MapiraIzvrsioceIPaginaciju()
    {
        // Arrange
        var filteri = NoviFilter("izvrsioci");
        var spec = Mock.Of<ISpecification<IzvrsilacUsluge>>();
        var majstor = new Majstor
        {
            Id = "m1",
            UserName = "majstor1",
            Ime = "Amar",
            Prezime = "Kovač",
            Opis = DugiOpis,
            ProsjecnaOcjena = 4.5m,
            MinCijenaUsluge = 40,
            BrojZavrsenihPoslova = 6,
            BrojRecenzija = 4,
            Mjesta = new List<KorisnikMjesto>
            {
                new KorisnikMjesto { Mjesto = NovoMjesto() }
            },
            Kategorije = new List<IzvrsilacKategorija>
            {
                new IzvrsilacKategorija
                {
                    Kategorija = new Kategorija { Naziv = "Vodoinstalacije" }
                }
            }
        };
        _strategija.Setup(s => s.NapraviIzvrsilacUslugeSpec(filteri)).Returns(spec);
        _izvrsioci.Setup(r => r.PronadjiAsync(spec, 2, 5))
            .ReturnsAsync(new StraniceniRezultat<IzvrsilacUsluge>
            {
                Stavke = new List<IzvrsilacUsluge>
                {
                    majstor,
                    new Majstor { Id = "m2", Opis = null }
                },
                Ukupno = 12
            });

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
        ProvjeriPaginaciju(rezultat, "izvrsioci");
        _izvrsioci.Verify(r => r.PronadjiAsync(spec, 2, 5), Times.Once());
        _strategija.Verify(s => s.DajDefaultniTab(), Times.Never());
        _usluge.VerifyNoOtherCalls();
        _radnaMjesta.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task PretraziAsync_SpecifikacijaUsluga_MapiraOglaseUslugaIPaginaciju()
    {
        // Arrange
        var filteri = NoviFilter("usluge");
        var spec = Mock.Of<ISpecification<OglasUsluge>>();
        var vlasnik = NoviVlasnik();
        var oglas = new OglasUsluge
        {
            OglasID = 10,
            Naslov = "Popravka slavine",
            Opis = DugiOpis,
            MinBudzet = 30,
            MaxBudzet = 90,
            VlasnikOglasa = vlasnik,
            Mjesto = NovoMjesto(),
            Ponude = new List<PonudaUsluge> { new PonudaUsluge() }
        };
        _strategija.Setup(s => s.NapraviUslugeSpec(filteri)).Returns(spec);
        _usluge.Setup(r => r.PronadjiAsync(spec, 2, 5))
            .ReturnsAsync(new StraniceniRezultat<OglasUsluge>
            {
                Stavke = new List<OglasUsluge>
                {
                    oglas,
                    new OglasUsluge
                    {
                        OglasID = 11,
                        Opis = "Kratak opis",
                        VlasnikOglasa = vlasnik
                    }
                },
                Ukupno = 12
            });

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
        ProvjeriPaginaciju(rezultat, "usluge");
        _usluge.Verify(r => r.PronadjiAsync(spec, 2, 5), Times.Once());
        _izvrsioci.VerifyNoOtherCalls();
        _radnaMjesta.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task PretraziAsync_SpecifikacijaRadnihMjesta_MapiraRadnaMjestaIPaginaciju()
    {
        // Arrange
        var filteri = NoviFilter("radnomjesto");
        var spec = Mock.Of<ISpecification<OglasRadnoMjesto>>();
        var vlasnik = NoviVlasnik();
        var oglas = new OglasRadnoMjesto
        {
            OglasID = 20,
            Naslov = "Vodoinstalater",
            Opis = DugiOpis,
            MinPrihod = 800,
            MaxPrihod = 1200,
            VlasnikOglasa = vlasnik,
            Mjesto = NovoMjesto(),
            Prijave = new List<PrijavaRadnoMjesto> { new PrijavaRadnoMjesto() }
        };
        _strategija.Setup(s => s.NapraviRadnoMjestoSpec(filteri)).Returns(spec);
        _radnaMjesta.Setup(r => r.PronadjiAsync(spec, 2, 5))
            .ReturnsAsync(new StraniceniRezultat<OglasRadnoMjesto>
            {
                Stavke = new List<OglasRadnoMjesto>
                {
                    oglas,
                    new OglasRadnoMjesto
                    {
                        OglasID = 21,
                        Opis = "Kratak opis",
                        VlasnikOglasa = vlasnik
                    }
                },
                Ukupno = 12
            });

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
        ProvjeriPaginaciju(rezultat, "radnomjesto");
        _radnaMjesta.Verify(r => r.PronadjiAsync(spec, 2, 5), Times.Once());
        _izvrsioci.VerifyNoOtherCalls();
        _usluge.VerifyNoOtherCalls();
    }
}