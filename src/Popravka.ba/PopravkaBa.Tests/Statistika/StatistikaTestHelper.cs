using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Models;
using PopravkaBa.Domain.Records;

namespace PopravkaBa.Tests.Statistika;

// Testni redovi rang liste za testove servisa, strategija, repozitorija i posla statistike
internal static class StatistikaTestHelper
{
    internal const int Godina = 2026;
    internal const int Mjesec = 11;

    internal static MjesecnaStatistikaKompozicija Red(string izvrsilacId, int rang = 1, decimal ocjena = 4.5m,
        int poslovi = 3, int godina = Godina, int mjesec = Mjesec, int? kategorijaId = 1, string? kategorija = "Vodoinstalacije",
        int? mjestoId = 1, string? mjesto = "Sarajevo", string? ime = null) => new()
    {
        Godina = godina,
        Mjesec = mjesec,
        IzvrsilacID = izvrsilacId,
        Username = izvrsilacId,
        DisplayName = ime ?? $"Majstor {izvrsilacId}",
        Slika = $"{izvrsilacId}.png",
        KategorijaID = kategorijaId,
        KategorijaNaziv = kategorija,
        MjestoID = mjestoId,
        MjestoNaziv = mjesto,
        ProsjecnaOcjena = ocjena,
        BrojPoslova = poslovi,
        RangStandardni = rang,
        TipKorisnika = KorisnickeUloge.Majstor,
        VrijemeAzuriranja = new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc)
    };

    internal static StatistikaMetaRecord PraznaMeta() =>
        new(null, Array.Empty<StatistikaRecord>(), Array.Empty<StatistikaRecord>());
}
