using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Models;

namespace PopravkaBa.Tests.VerifikacijaFirme;

// Zahtjev za verifikaciju sa svim obaveznim poljima popunjenim, da ga baza prihvati
internal static class VerifikacijaFirmeRepositoryTestHelper
{
    internal static EmailVerifikacijaFirme Zahtjev(int id, string firmaId, Status status = Status.NaCekanju, int danaUnazad = 0) => new()
    {
        VerifikacioniID = id,
        FirmaID = firmaId,
        NazivFirme = $"Firma {firmaId}",
        StatusVerifikacije = status,
        AdminEmail = "admin@popravka.ba",
        JIB = "4200000000001",
        SjedisteFirme = "Sarajevo",
        KontaktTelefon = "061123456",
        OdgovornaOsobaIme = "Haso",
        OdgovornaOsobaPrezime = "Hasić",
        OdgovornaOsobaEmail = "haso@popravka.ba",
        OdgovornaOsobaPozicija = "Direktor",
        OdgovornaOsobaTelefon = "061123456",
        Rjesenje = "rjesenje.pdf",
        PoreznoUvjerenje = "porezno.pdf",
        Logotip = "logo.png",
        DatumPodnosenja = new DateTime(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc).AddDays(-danaUnazad)
    };
}
