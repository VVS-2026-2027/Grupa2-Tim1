using PopravkaBa.Application.DTOs;
using PopravkaBa.Application.Services.Implementation;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Models;

namespace PopravkaBa.Tests.VerifikacijaFirme;

// Pomoćne metode koje samo pripremaju podatke za testove
internal static class VerifikacijaFirmeServiceTestHelper
{
    internal static EmailVerifikacijaFirme ZahtjevNaCekanju(Firma? firma = null) => new()
    {
        VerifikacioniID = 1,
        FirmaID = "f1",
        Firma = firma!,
        StatusVerifikacije = Status.NaCekanju,
        NazivFirme = "Haso instalacije d.o.o.",
        OdgovornaOsobaIme = "Haso",
        OdgovornaOsobaPrezime = "Hasić",
        WebStranica = "hasoinstalacije.ba",
        KontaktTelefon = "061123456",
        Logotip = "logo.png",
        Rjesenje = "rjesenje.pdf",
        PoreznoUvjerenje = "porezno.pdf",
        LicencaDjelatnosti = "licenca.pdf"
    };
}
