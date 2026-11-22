using Microsoft.AspNetCore.Http;
using PopravkaBa.Domain.Models;
using PopravkaBa.Web.Models.ViewModels;

namespace PopravkaBa.Tests.VerifikacijaFirme;

// Pomoćne metode za VerificationControllerTests
internal static class VerificationControllerTestHelper
{
    internal const string AdminEmail = "admin@popravka.ba";

    internal static Firma NeverificiranaFirma() => new()
    {
        Id = "f1", UserName = "firma1", NazivFirme = "Haso instalacije d.o.o.", Email = "firma@popravka.ba",
        WebStranica = "hasoinstalacije.ba", PhoneNumber = "061123456"
    };

    internal static IFormFile Fajl(string ime, long velicina = 2048)
    {
        var fajl = new Mock<IFormFile>();
        fajl.Setup(f => f.FileName).Returns(ime);
        fajl.Setup(f => f.Length).Returns(velicina);
        fajl.Setup(f => f.ContentType).Returns("application/pdf");
        fajl.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream());
        return fajl.Object;
    }

    // Forma sa oba obavezna dokumenta; licenca i logo se dodaju po potrebi
    internal static VerifikacijaFirmeViewModel IspravnaForma() => new()
    {
        NazivFirme = "Haso instalacije d.o.o.",
        JIB = "4200000000001",
        SjedisteFirme = "Sarajevo",
        KontaktTelefon = "061123456",
        OdgovornaOsobaIme = "Haso",
        OdgovornaOsobaPrezime = "Hasić",
        OdgovornaOsobaPozicija = "Direktor",
        OdgovornaOsobaEmail = "haso@popravka.ba",
        OdgovornaOsobaTelefon = "061123456",
        RjesenjeFajl = Fajl("rjesenje.pdf"),
        PoreznoUvjerenjeFajl = Fajl("porezno.pdf")
    };
}
