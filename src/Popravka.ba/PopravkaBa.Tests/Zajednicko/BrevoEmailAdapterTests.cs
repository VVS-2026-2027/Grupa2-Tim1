using System.Net.Sockets;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Models;
using PopravkaBa.Infrastructure.Adapters;
using PopravkaBa.Infrastructure.Adapters.Options;
using static PopravkaBa.Tests.Zajednicko.BrevoEmailAdapterTestHelper;

namespace PopravkaBa.Tests.Zajednicko;

// Pravi SMTP server se ne koristi: testovi koriste neispravne adrese primaoca (greška prije konekcije)
// i server koji ne postoji na lokalnoj mašini (greška pri konekciji).
[TestClass]
public class BrevoEmailAdapterTests
{
    private BrevoEmailAdapter _adapter = null!;

    [TestInitialize]
    public void Postavi()
    {
        var opcije = Microsoft.Extensions.Options.Options.Create(new BrevoEmailOptions
        {
            Host = "127.0.0.1",
            Port = 1, // na ovom portu niko ne sluša, konekcija se odbija
            Login = "login",
            SmtpKey = "kljuc",
            FromEmail = "no-reply@popravka.ba",
            FromName = "Popravka.ba"
        });
        _adapter = new BrevoEmailAdapter(opcije);
    }

    [TestMethod]
    public async Task PosaljiEmailAsync_NeispravnaAdresaPrimaoca_BacaFormatException()
    {
        // Act + Assert
        await Assert.ThrowsAsync<FormatException>(() =>
            _adapter.PosaljiEmailAsync("nije@@email", "Naslov", "<p>Tijelo</p>"));
    }

    [TestMethod]
    public async Task PosaljiEmailAsync_SmtpServerNedostupan_BacaIzuzetakPriKonekciji()
    {
        // Act + Assert
        await Assert.ThrowsAsync<SocketException>(() =>
            _adapter.PosaljiEmailAsync("primalac@popravka.ba", "Naslov", "<p>Tijelo</p>"));
    }

    [TestMethod]
    public async Task PosaljiEmailAsync_NotifikacijaOglasa_ProsljedjujeAdresuPrimaoca()
    {
        // Arrange
        var notifikacija = EmailNotifikacijaOglas.ZaBrisanjeZbogNeaktivnosti(
            "nije@@email", "Haso", "Popravak slavine", 7);

        // Act + Assert (neispravna adresa znači da je email iz notifikacije korišten kao primalac)
        await Assert.ThrowsAsync<FormatException>(() => _adapter.PosaljiEmailAsync(notifikacija));
    }

    public static IEnumerable<object[]> SlucajeviPrimaoca =>
        DataDrivenInputParser.CitajXml<bool, string, string, bool>("TestData/xml/BrevoEmailAdapter_neispravniPrimaoci.xml");

    [TestMethod]
    [DynamicData(nameof(SlucajeviPrimaoca))]
    public async Task PosaljiEmailAsync_VerifikacijaFirmePodaciIzXml_KoristiAdresuAdminaIliFirme(
        bool zaAdmina, string adminEmail, string firmaEmail, bool formatGreska)
    {
        // Arrange
        var verifikacija = NovaVerifikacija(adminEmail, firmaEmail);

        // Act (greška formata znači da je ta adresa uzeta kao primalac, inače je greška na konekciji)
        var izuzetak = await Assert.ThrowsAsync<Exception>(() => _adapter.PosaljiEmailAsync(verifikacija, zaAdmina));

        // Assert
        Assert.AreEqual(formatGreska, izuzetak is FormatException);
    }
}
