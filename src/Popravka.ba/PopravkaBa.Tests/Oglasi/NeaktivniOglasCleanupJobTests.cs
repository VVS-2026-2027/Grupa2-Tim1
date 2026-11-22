using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Popravka.ba.Data;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Models;
using PopravkaBa.Infrastructure.BackgroundServices;
using PopravkaBa.Tests.Nalog;
using static PopravkaBa.Tests.Oglasi.OglasiTestHelper;

namespace PopravkaBa.Tests.Oglasi;

// Job se pokrene, test čeka da odradi prvi krug čišćenja (log ili zatvaranje konteksta), pa ga zaustavi.
// Brisanje radi preko RemoveRange, pa je InMemory baza dovoljna.
[TestClass]
[TestCategory("Infrastructure")]
public class NeaktivniOglasCleanupJobTests
{
    private DbContextOptions<ApplicationDbContext> _opcije = null!;
    private Mock<IEmailSender> _email = null!;
    private Mock<ILogger<NeaktivniOglasCleanupJob>> _logger = null!;
    private TaskCompletionSource _logInformacija = null!;
    private TaskCompletionSource _logGreska = null!;
    private TaskCompletionSource _krajKruga = null!;
    private ServiceProvider _provider = null!;
    private NeaktivniOglasCleanupJob _job = null!;

    [TestInitialize]
    public void Postavi()
    {
        _opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _email = new Mock<IEmailSender>();
        _logInformacija = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _logGreska = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _krajKruga = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        _logger = new Mock<ILogger<NeaktivniOglasCleanupJob>>();
        _logger.Setup(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(), (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()))
            .Callback(new InvocationAction(poziv =>
                (((LogLevel)poziv.Arguments[0]) == LogLevel.Error ? _logGreska : _logInformacija).TrySetResult()));

        _provider = new ServiceCollection()
            .AddScoped<ApplicationDbContext>(_ => new KontekstKojiJavljaKrajKruga(_opcije, _krajKruga))
            .AddScoped(_ => _email.Object)
            .BuildServiceProvider();
        _job = new NeaktivniOglasCleanupJob(_provider.GetRequiredService<IServiceScopeFactory>(), _logger.Object);
    }

    [TestCleanup]
    public void Pocisti()
    {
        _job.Dispose();
        _provider.Dispose();
    }

    private async Task Ubaci(params object[] entiteti)
    {
        await using var db = new ApplicationDbContext(_opcije);
        db.AddRange(new Klijent { Id = VlasnikId, UserName = "klijent1", Email = "klijent@popravka.ba", Ime = "Haso" });
        db.AddRange(entiteti);
        await db.SaveChangesAsync();
    }

    private async Task PokreniJedanKrug(Task signal)
    {
        await _job.StartAsync(CancellationToken.None);
        await signal.WaitAsync(TimeSpan.FromSeconds(10));
        await _job.StopAsync(CancellationToken.None);
    }

    private int[] PreostaliOglasi()
    {
        using var db = new ApplicationDbContext(_opcije);
        return db.OglasiUsluga.Select(o => o.OglasID)
            .Concat(db.OglasiRadnogMjesta.Select(o => o.OglasID))
            .OrderBy(id => id).ToArray();
    }

    private static T StarDanaUnazad<T>(T oglas, int dana) where T : Oglas
    {
        oglas.DatumObjave = DateTime.UtcNow.AddDays(-dana);
        return oglas;
    }

    [TestMethod]
    [Timeout(15000)]
    [Owner("Aid Mustafić")]
    public async Task ExecuteAsync_OglasiRazlicitogStanja_BriseZastarjeleBezAktivnostiINeaktivne()
    {
        // Arrange
        var stariSaPonudom = StarDanaUnazad(OglasUsluge(3), 20);
        stariSaPonudom.Ponude = new List<PonudaUsluge>
        {
            new() { IzvrsilacID = "m1", DatumPocetkaUsluge = DateTime.UtcNow }
        };
        await Ubaci(
            StarDanaUnazad(OglasUsluge(1), 20),                     // star, bez ponuda -> briše se
            StarDanaUnazad(OglasUsluge(2), 2),                      // nov -> ostaje
            stariSaPonudom,                                         // star, ali ima ponudu -> ostaje
            StarDanaUnazad(OglasUsluge(4, status: Status.Neaktivan), 1), // neaktivan -> briše se
            StarDanaUnazad(OglasRadnoMjesto(5), 20),                // star, bez prijava -> briše se
            StarDanaUnazad(OglasRadnoMjesto(6), 3));                // nov -> ostaje

        // Act
        await PokreniJedanKrug(_logInformacija.Task);

        // Assert
        CollectionAssert.AreEqual(new[] { 2, 3, 6 }, PreostaliOglasi());
    }

    [TestMethod]
    [Timeout(15000)]
    [Owner("Aid Mustafić")]
    public async Task ExecuteAsync_ObrisanOglas_SaljeObavijestVlasniku()
    {
        // Arrange
        await Ubaci(StarDanaUnazad(OglasUsluge(1), 20));

        // Act
        await PokreniJedanKrug(_logInformacija.Task);

        // Assert
        _email.Verify(e => e.PosaljiEmailAsync(It.Is<EmailNotifikacijaOglas>(n =>
            n.EmailPrimalac == "klijent@popravka.ba" && n.OglasNaslov == "Usluga oglas 1")), Times.Once);
    }

    [TestMethod]
    [Timeout(15000)]
    [Owner("Aid Mustafić")]
    public async Task ExecuteAsync_VlasnikBezEmaila_BriseOglasBezObavijesti()
    {
        // Arrange
        await Ubaci(
            new Firma { Id = "f1", UserName = "firma1", NazivFirme = "Elektro d.o.o.", Adresa = "Adresa 1" },
            StarDanaUnazad(OglasRadnoMjesto(1, vlasnikId: "f1"), 20));

        // Act
        await PokreniJedanKrug(_logInformacija.Task);

        // Assert
        Assert.AreEqual(0, PreostaliOglasi().Length);
        _email.Verify(e => e.PosaljiEmailAsync(It.IsAny<EmailNotifikacijaOglas>()), Times.Never);
    }

    [TestMethod]
    [Timeout(15000)]
    [Owner("Aid Mustafić")]
    public async Task ExecuteAsync_SlanjeObavijestiNeuspjesno_LogujeGreskuIZavrsavaKrug()
    {
        // Arrange
        await Ubaci(StarDanaUnazad(OglasUsluge(1), 20));
        _email.Setup(e => e.PosaljiEmailAsync(It.IsAny<EmailNotifikacijaOglas>()))
            .ThrowsAsync(new InvalidOperationException("SMTP nedostupan"));

        // Act
        await PokreniJedanKrug(_logInformacija.Task);

        // Assert: greška pri slanju je logovana, a oglas je ipak obrisan
        Assert.IsTrue(_logGreska.Task.IsCompleted);
        Assert.AreEqual(0, PreostaliOglasi().Length);
    }

    [TestMethod]
    [Timeout(15000)]
    [Owner("Aid Mustafić")]
    public async Task ExecuteAsync_NemaOglasaZaBrisanje_NistaNeLogujeNiSalje()
    {
        // Arrange
        await Ubaci(StarDanaUnazad(OglasUsluge(1), 2));

        // Act
        await PokreniJedanKrug(_krajKruga.Task);

        // Assert
        Assert.IsFalse(_logInformacija.Task.IsCompleted);
        _email.Verify(e => e.PosaljiEmailAsync(It.IsAny<EmailNotifikacijaOglas>()), Times.Never);
    }

    [TestMethod]
    [Timeout(15000)]
    [Owner("Aid Mustafić")]
    public async Task ExecuteAsync_GreskaUKrugu_LogujeGresku()
    {
        // Arrange: DI bez servisa za email, pa job ne može dobiti IEmailSender
        using var bezEmaila = new ServiceCollection()
            .AddScoped<ApplicationDbContext>(_ => new ApplicationDbContext(_opcije))
            .BuildServiceProvider();
        using var job = new NeaktivniOglasCleanupJob(bezEmaila.GetRequiredService<IServiceScopeFactory>(), _logger.Object);

        // Act
        await job.StartAsync(CancellationToken.None);
        await _logGreska.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await job.StopAsync(CancellationToken.None);

        // Assert
        Assert.IsTrue(_logGreska.Task.IsCompleted);
    }
}
