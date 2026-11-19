using PopravkaBa.Application.DTOs;
using PopravkaBa.Application.Strategies.Implementation;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Models;

namespace PopravkaBa.Tests.Pretraga;

internal static class StrategijaTestPodaci
{
    internal static Majstor NoviMajstor(
        string id, int cijena, decimal ocjena, int kategorija = 1, int mjesto = 1,
        string ime = "Amar", string opis = "Servis") => new()
    {
        Id = id,
        Ime = ime,
        Opis = opis,
        MinCijenaUsluge = cijena,
        ProsjecnaOcjena = ocjena,
        Kategorije = new List<IzvrsilacKategorija>
        {
            new IzvrsilacKategorija { KategorijaID = kategorija }
        },
        Mjesta = new List<KorisnikMjesto>
        {
            new KorisnikMjesto { MjestoID = mjesto }
        }
    };

    internal static OglasUsluge NoviOglas(
        int id, int minimum, int maksimum, int kategorija = 1, int mjesto = 1,
        string naslov = "Popravka", string opis = "Opis popravke",
        Status status = Status.Aktivan) => new()
    {
        OglasID = id,
        MinBudzet = minimum,
        MaxBudzet = maksimum,
        MjestoID = mjesto,
        Naslov = naslov,
        Opis = opis,
        StatusOglasa = status,
        Kategorije = new List<OglasKategorija>
        {
            new OglasKategorija { KategorijaID = kategorija }
        }
    };

    internal static OglasRadnoMjesto NovoRadnoMjesto(
        int id, int minimum, int maksimum, int kategorija = 1, int mjesto = 1,
        int iskustvo = 2, VrstaZaposlenja tip = VrstaZaposlenja.PunoRadnoVrijeme,
        string naslov = "Elektro serviser", string opis = "Opis posla",
        Status status = Status.Aktivan) => new()
    {
        OglasID = id,
        MinPrihod = minimum,
        MaxPrihod = maksimum,
        MjestoID = mjesto,
        MinIskustvo = iskustvo,
        VrstaZaposlenja = tip,
        Naslov = naslov,
        Opis = opis,
        StatusOglasa = status,
        Kategorije = new List<OglasKategorija>
        {
            new OglasKategorija { KategorijaID = kategorija }
        }
    };
}
