using PopravkaBa.Application.DTOs;
using PopravkaBa.Application.Strategies.Implementation;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Models;
using PopravkaBa.Domain.Specifications.Interface;

namespace PopravkaBa.Tests.Strategije;

// Pomoćne metode koje samo pripremaju podatke za testove
internal static class PretragaStrategijaTestHelper
{
    internal static List<IzvrsilacKategorija> Kat(params int[] ids) =>
        ids.Select(id => new IzvrsilacKategorija { KategorijaID = id }).ToList();

    internal static List<KorisnikMjesto> Mjesta(params int[] ids) =>
        ids.Select(id => new KorisnikMjesto { MjestoID = id }).ToList();

    internal static List<OglasKategorija> OglasKat(params int[] ids) =>
        ids.Select(id => new OglasKategorija { KategorijaID = id }).ToList();

    internal static List<IzvrsilacUsluge> Izvrsioci() => new()
    {
        new Majstor { Id = "m1", Ime = "Haso", Prezime = "Hasić", Opis = "Vodoinstalater za kuće",
            MinCijenaUsluge = 50, ProsjecnaOcjena = 3.0m, Kategorije = Kat(1), Mjesta = Mjesta(1) },
        new Majstor { Id = "m2", Ime = "Mujo", Prezime = "Mujić", Opis = "Električar",
            MinCijenaUsluge = 100, ProsjecnaOcjena = 4.5m, Kategorije = Kat(2), Mjesta = Mjesta(2) },
        new Firma { Id = "f1", NazivFirme = "Elektro Servis d.o.o.", Opis = "Popravke svih vrsta",
            MinCijenaUsluge = 200, ProsjecnaOcjena = 5.0m, Kategorije = Kat(2), Mjesta = Mjesta(1) },
        new Majstor { Id = "m3", Ime = "Suljo", Prezime = "Suljić", Opis = null,
            MinCijenaUsluge = null, ProsjecnaOcjena = 2.0m, Kategorije = Kat(), Mjesta = Mjesta() }
    };

    internal static List<OglasUsluge> OglasiUsluga() => new()
    {
        new OglasUsluge { OglasID = 1, Naslov = "Popravak slavine", Opis = "Curi voda u kuhinji",
            MjestoID = 1, MinBudzet = 100, MaxBudzet = 300, Kategorije = OglasKat(1) },
        new OglasUsluge { OglasID = 2, Naslov = "Farbanje stana", Opis = "Potrebno okrečiti dvije sobe",
            MjestoID = 2, MinBudzet = 500, MaxBudzet = 1000, Kategorije = OglasKat(2) },
        new OglasUsluge { OglasID = 3, Naslov = "Zamjena sijalica", Opis = "Potreban električar",
            MjestoID = 1, MinBudzet = 0, MaxBudzet = 50, Kategorije = OglasKat(2) },
        new OglasUsluge { OglasID = 4, Naslov = "Neaktivan oglas", Opis = "Nije vidljiv",
            MjestoID = 1, MinBudzet = 100, MaxBudzet = 300, Kategorije = OglasKat(1), StatusOglasa = Status.Neaktivan }
    };

    internal static List<OglasRadnoMjesto> OglasiRadnihMjesta() => new()
    {
        new OglasRadnoMjesto { OglasID = 1, Naslov = "Vodoinstalater", Opis = "Traži se iskusan majstor",
            MjestoID = 1, VrstaZaposlenja = VrstaZaposlenja.PunoRadnoVrijeme,
            MinPrihod = 1000, MaxPrihod = 1500, MinIskustvo = 2, Kategorije = OglasKat(1) },
        new OglasRadnoMjesto { OglasID = 2, Naslov = "Električar", Opis = "Potreban za manje radove",
            MjestoID = 2, VrstaZaposlenja = VrstaZaposlenja.PolaRadnogVrijema,
            MinPrihod = 500, MaxPrihod = 800, MinIskustvo = 0, Kategorije = OglasKat(2) },
        new OglasRadnoMjesto { OglasID = 3, Naslov = "Pomoćni radnik", Opis = "Rad na gradilištu",
            MjestoID = 1, VrstaZaposlenja = VrstaZaposlenja.PunoRadnoVrijeme,
            MinPrihod = 1500, MaxPrihod = 2500, MinIskustvo = 5, Kategorije = OglasKat(1) },
        new OglasRadnoMjesto { OglasID = 4, Naslov = "Neaktivan posao", Opis = "Nije vidljiv",
            MjestoID = 1, VrstaZaposlenja = VrstaZaposlenja.PunoRadnoVrijeme,
            MinPrihod = 1000, MaxPrihod = 1500, MinIskustvo = 0, Kategorije = OglasKat(1), StatusOglasa = Status.Neaktivan }
    };

    internal static List<T> Primijeni<T>(ISpecification<T>? specifikacija, IEnumerable<T> podaci) =>
        podaci.Where(specifikacija!.ToExpression().Compile()).ToList();

    internal static List<string> IdIzvrsilaca(IEnumerable<IzvrsilacUsluge> izvrsioci) =>
        izvrsioci.Select(i => i.Id).ToList();

    internal static List<int> IdOglasa(IEnumerable<Oglas> oglasi) =>
        oglasi.Select(o => o.OglasID).ToList();

    internal static FilterPretrageDto FilterIzvrsilaca() => new() { AktivniTab = "Izvrsioci usluga" };

    internal static FilterPretrageDto FilterUsluga() => new() { AktivniTab = "Oglasi za popravke" };

    internal static FilterPretrageDto FilterRadnihMjesta() => new() { AktivniTab = "Oglasi za posao" };
}
