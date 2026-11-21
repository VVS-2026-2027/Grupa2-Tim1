using PopravkaBa.Application.DTOs;
using PopravkaBa.Application.Services.Implementation;
using PopravkaBa.Application.Strategies.Interface;
using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Models;
using PopravkaBa.Domain.Specifications.Interface;
using PopravkaBa.Infrastructure.Wrappers;

namespace PopravkaBa.Tests.Pretraga;

internal static class PretragaServiceTestHelper
{
    internal static readonly string DugiOpis = new string('x', 160);
    internal static readonly string SkraceniOpis = new string('x', 150) + "...";

    internal static FilterPretrageDto NoviFilter(string tab) => new()
    {
        AktivniTab = tab,
        Stranica = 2,
        StavkiPoStranici = 5
    };

    internal static Mjesto NovoMjesto() => new() { Naziv = "Sarajevo" };

    internal static Klijent NoviVlasnik() => new()
    {
        Ime = "Aida",
        Prezime = "Kovač",
        Slika = "profil.png"
    };

    internal static void ProvjeriPaginaciju(RezultatPretrageDto rezultat, string tab)
    {
        Assert.AreEqual(tab, rezultat.AktivniTab);
        Assert.AreEqual(2, rezultat.TrenutnaStrana);
        Assert.AreEqual(5, rezultat.StavkiPoStranici);
        Assert.AreEqual(12, rezultat.UkupnoStavki);
        Assert.AreEqual(3, rezultat.UkupnoStranica);
        Assert.AreEqual(6, rezultat.PrikazOd);
        Assert.AreEqual(10, rezultat.PrikazDo);
    }

    // Stranica 2 (po 5 stavki) od ukupno 12 izvršilaca: prvi ima sve podatke, drugi nema opis, mjesto ni kategoriju
    internal static (ISpecification<IzvrsilacUsluge> Spec, Majstor Majstor) PostaviStranicuIzvrsilaca(
        Mock<IPretragaStrategy> strategija, Mock<IIzvrsilacUslugeRepository> izvrsioci, FilterPretrageDto filteri)
    {
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
        strategija.Setup(s => s.NapraviIzvrsilacUslugeSpec(filteri)).Returns(spec);
        izvrsioci.Setup(r => r.PronadjiAsync(spec, 2, 5))
            .ReturnsAsync(new StraniceniRezultat<IzvrsilacUsluge>
            {
                Stavke = new List<IzvrsilacUsluge>
                {
                    majstor,
                    new Majstor { Id = "m2", Opis = null }
                },
                Ukupno = 12
            });
        return (spec, majstor);
    }

    // Stranica 2 (po 5 stavki) od ukupno 12 oglasa usluga: prvi ima sve podatke, drugi kratak opis i bez mjesta
    internal static (ISpecification<OglasUsluge> Spec, Klijent Vlasnik) PostaviStranicuUsluga(
        Mock<IPretragaStrategy> strategija, Mock<IOglasUslugeRepository> usluge, FilterPretrageDto filteri)
    {
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
        strategija.Setup(s => s.NapraviUslugeSpec(filteri)).Returns(spec);
        usluge.Setup(r => r.PronadjiAsync(spec, 2, 5))
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
        return (spec, vlasnik);
    }

    // Stranica 2 (po 5 stavki) od ukupno 12 radnih mjesta: prvo ima sve podatke, drugo kratak opis i bez mjesta
    internal static (ISpecification<OglasRadnoMjesto> Spec, Klijent Vlasnik) PostaviStranicuRadnihMjesta(
        Mock<IPretragaStrategy> strategija, Mock<IOglasRadnoMjestoRepository> radnaMjesta, FilterPretrageDto filteri)
    {
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
        strategija.Setup(s => s.NapraviRadnoMjestoSpec(filteri)).Returns(spec);
        radnaMjesta.Setup(r => r.PronadjiAsync(spec, 2, 5))
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
        return (spec, vlasnik);
    }
}
