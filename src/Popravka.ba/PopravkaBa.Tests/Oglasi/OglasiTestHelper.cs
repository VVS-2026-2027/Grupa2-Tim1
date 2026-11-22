using PopravkaBa.Application.DTOs;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Models;

namespace PopravkaBa.Tests.Oglasi;

// Testni oglasi i DTO-i za testove servisa, fasada, repozitorija i kontrolera oglasa
internal static class OglasiTestHelper
{
    internal const string VlasnikId = "v1";
    internal const string DrugiKorisnikId = "v2";

    internal static OglasMajstora OglasMajstora(int id = 1, string vlasnikId = VlasnikId,
        Status status = Status.Aktivan, double minCijena = 50) => new()
    {
        OglasID = id,
        Naslov = $"Majstor oglas {id}",
        Opis = "Popravak bojlera i slavina",
        MjestoID = 1,
        MinCijena = minCijena,
        TipIsplate = TipIsplate.PoSatu,
        VlasnikOglasaID = vlasnikId,
        StatusOglasa = status,
        Slika = "stara.png",
        DatumObjave = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(id)
    };

    internal static OglasUsluge OglasUsluge(int id = 1, string vlasnikId = VlasnikId,
        Status status = Status.Aktivan) => new()
    {
        OglasID = id,
        Naslov = $"Usluga oglas {id}",
        Opis = "Treba okrečiti stan",
        MjestoID = 1,
        MinBudzet = 100,
        MaxBudzet = 300,
        VlasnikOglasaID = vlasnikId,
        StatusOglasa = status,
        Slika = "stara.png",
        DatumObjave = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(id)
    };

    internal static OglasRadnoMjesto OglasRadnoMjesto(int id = 1, string vlasnikId = VlasnikId,
        Status status = Status.Aktivan) => new()
    {
        OglasID = id,
        Naslov = $"Radno mjesto {id}",
        Opis = "Tražimo električara",
        MjestoID = 1,
        BrojIzvrsilaca = 2,
        VrstaZaposlenja = VrstaZaposlenja.PunoRadnoVrijeme,
        MinPrihod = 1200,
        MaxPrihod = 1800,
        TipIsplate = TipIsplate.Mjesecno,
        VlasnikOglasaID = vlasnikId,
        StatusOglasa = status,
        Slika = "stara.png",
        DatumObjave = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(id),
        Uvjeti = new List<UvjetOglasa>(),
        VozackeDozvole = new List<OglasVozackaDozvola>(),
        Prijave = new List<PrijavaRadnoMjesto>()
    };

    internal static ObjaviOglasMajstoraDto ObjaviMajstoraDto(List<int>? kategorije = null) => new()
    {
        Naslov = "Novi oglas",
        Opis = "Opis novog oglasa",
        MjestoID = 2,
        MinCijena = 40,
        TipIsplate = TipIsplate.Jednokratno,
        KategorijeID = kategorije ?? new List<int> { 1, 2 },
        Slika = "nova.png"
    };

    internal static UrediOglasMajstoraDto UrediMajstoraDto(int id = 1, string? slika = "nova.png",
        List<int>? kategorije = null) => new()
    {
        OglasID = id,
        Naslov = "Uređen naslov",
        Opis = "Uređen opis",
        MjestoID = 3,
        MinCijena = 70,
        TipIsplate = TipIsplate.Jednokratno,
        KategorijeID = kategorije ?? new List<int> { 3 },
        Slika = slika
    };

    internal static ObjaviOglasUslugeDto ObjaviUslugeDto() => new()
    {
        Naslov = "Novi oglas usluge",
        Opis = "Opis oglasa usluge",
        MjestoID = 2,
        MinBudzet = 50,
        MaxBudzet = 150,
        KategorijeID = new List<int> { 1 },
        Slika = "nova.png"
    };

    internal static UrediOglasUslugeDto UrediUslugeDto(int id = 1, string? slika = "nova.png") => new()
    {
        OglasID = id,
        Naslov = "Uređen naslov",
        Opis = "Uređen opis",
        MjestoID = 3,
        MinBudzet = 80,
        MaxBudzet = 400,
        KategorijeID = new List<int> { 2 },
        Slika = slika
    };

    internal static ObjaviOglasRadnoMjestoDto ObjaviRadnoMjestoDto(int? minPrihod = 1000, int? maxPrihod = 2000) => new()
    {
        Naslov = "Novo radno mjesto",
        Opis = "Opis radnog mjesta",
        MjestoID = 2,
        BrojIzvrsilaca = 3,
        VrstaZaposlenja = VrstaZaposlenja.PunoRadnoVrijeme,
        MinPrihod = minPrihod,
        MaxPrihod = maxPrihod,
        TipIsplate = TipIsplate.Mjesecno,
        Uvjeti = new List<string> { "Iskustvo 2 godine" },
        VozackeDozvole = new List<VozackaDozvola> { VozackaDozvola.B },
        KategorijeID = new List<int> { 1 }
    };

    internal static UrediOglasRadnoMjestoDto UrediRadnoMjestoDto(int id = 1, int? minPrihod = 1500,
        int? maxPrihod = 2500, string? slika = "nova.png") => new()
    {
        OglasID = id,
        Naslov = "Uređeno radno mjesto",
        Opis = "Uređen opis",
        MjestoID = 3,
        BrojIzvrsilaca = 1,
        VrstaZaposlenja = VrstaZaposlenja.PunoRadnoVrijeme,
        MinPrihod = minPrihod,
        MaxPrihod = maxPrihod,
        TipIsplate = TipIsplate.Mjesecno,
        Uvjeti = new List<string> { "Vozačka B" },
        VozackeDozvole = new List<VozackaDozvola> { VozackaDozvola.C },
        KategorijeID = new List<int> { 2 },
        Slika = slika
    };
}
