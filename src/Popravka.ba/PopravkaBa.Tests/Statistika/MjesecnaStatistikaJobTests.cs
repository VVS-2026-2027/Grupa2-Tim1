using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Popravka.ba.Data;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Models;
using PopravkaBa.Infrastructure.BackgroundServices;

namespace PopravkaBa.Tests.Statistika;

// Job računa rang listu iz baze i predaje je repozitoriju. Repozitorij je mock, pa test
// uhvati redove koje je job napravio. Test čeka prvi log (uspjeh ili greška), pa zaustavi job.
[TestClass]
[TestCategory("Infrastructure")]
public class MjesecnaStatistikaJobTests
{
    private DbContextOptions<ApplicationDbContext> _opcije = null!;
    private Mock<IMjesecnaStatistikaRepository> _repo = null!;
    private Mock<ILogger<MjesecnaStatistikaJob>> _logger = null!;
    private TaskCompletionSource<LogLevel> _log = null!;
    private ServiceProvider _provider = null!;
    private MjesecnaStatistikaJob _job = null!;
    private IReadOnlyList<MjesecnaStatistikaKompozicija> _redovi = Array.Empty<MjesecnaStatistikaKompozicija>();
    private DateTime _sada;

    [TestInitialize]
    public void Postavi()
    {
        _sada = DateTime.UtcNow;
        _opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _log = new TaskCompletionSource<LogLevel>(TaskCreationOptions.RunContinuationsAsynchronously);

        _repo = new Mock<IMjesecnaStatistikaRepository>();
        _repo.Setup(r => r.ZamijeniSnapshot(It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<IReadOnlyList<MjesecnaStatistikaKompozicija>>(), It.IsAny<CancellationToken>()))
            .Callback<int, int, IReadOnlyList<MjesecnaStatistikaKompozicija>, CancellationToken>((_, _, redovi, _) => _redovi = redovi)
            .Returns(Task.CompletedTask);

        _logger = new Mock<ILogger<MjesecnaStatistikaJob>>();
        _logger.Setup(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(), (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()))
            .Callback(new InvocationAction(poziv => _log.TrySetResult((LogLevel)poziv.Arguments[0])));

        _provider = new ServiceCollection()
            .AddScoped(_ => new ApplicationDbContext(_opcije))
            .AddScoped(_ => _repo.Object)
            .BuildServiceProvider();
        _job = new MjesecnaStatistikaJob(_provider.GetRequiredService<IServiceScopeFactory>(), _logger.Object);

        using var db = new ApplicationDbContext(_opcije);
        db.AddRange(
            new Kategorija { ID = 1, Naziv = "Vodoinstalacije" },
            new Mjesto { MjestoID = 1, Naziv = "Sarajevo" },
            new IdentityRole { Id = "r-firma", Name = "Firma" });
        db.SaveChanges();
    }

    [TestCleanup]
    public void Pocisti()
    {
        _job.Dispose();
        _provider.Dispose();
    }

    private async Task<LogLevel> PokreniJedanKrug()
    {
        await _job.StartAsync(CancellationToken.None);
        var nivo = await _log.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await _job.StopAsync(CancellationToken.None);
        return nivo;
    }

    // Majstor sa mjestom i kategorijom, sa zadanim brojem završenih poslova i ocjenama u ovom mjesecu
    private void DodajIzvrsioca(ApplicationDbContext db, IzvrsilacUsluge izvrsilac, int poslova, params int[] ocjene)
    {
        izvrsilac.Kategorije = new List<IzvrsilacKategorija> { new() { KategorijaID = 1 } };
        izvrsilac.Mjesta ??= new List<KorisnikMjesto> { new() { MjestoID = 1 } };
        db.Add(izvrsilac);
        for (int i = 0; i < poslova; i++)
            db.Add(new PonudaUsluge
            {
                IzvrsilacID = izvrsilac.Id, StatusPonude = Status.Isporuceno,
                DatumPocetkaUsluge = _sada, DatumIzvrsavanjaUsluge = _sada
            });
        foreach (var ocjena in ocjene)
            db.Add(new Recenzija { IzvrsilacID = izvrsilac.Id, KlijentID = "k1", Ocjena = ocjena, Komentar = "Dobro", DatumRecenzije = _sada });
    }

    private static Majstor Majstor(string id, string? ime = null) =>
        new() { Id = id, UserName = id, Ime = ime ?? $"Majstor {id}", Prezime = "", Adresa = "Adresa" };

    private async Task Sacuvaj(Action<ApplicationDbContext> priprema)
    {
        await using var db = new ApplicationDbContext(_opcije);
        priprema(db);
        await db.SaveChangesAsync();
    }

    [TestMethod]
    [Timeout(15000)]
    [Owner("Aid Mustafić")]
    public async Task ExecuteAsync_IzvrsilacBezPoslaUMjesecu_NijeNaRangListi()
    {
        // Arrange
        await Sacuvaj(db =>
        {
            DodajIzvrsioca(db, Majstor("m1"), poslova: 2, 5);
            DodajIzvrsioca(db, Majstor("m2"), poslova: 0, 5);
        });

        // Act
        await PokreniJedanKrug();

        // Assert
        CollectionAssert.AreEqual(new[] { "m1" }, _redovi.Select(r => r.IzvrsilacID).ToList());
    }

    [TestMethod]
    [Timeout(15000)]
    [Owner("Aid Mustafić")]
    public async Task ExecuteAsync_ViseIzvrsilaca_RangPoOcjeniPaPosloviPaImenu()
    {
        // Arrange
        await Sacuvaj(db =>
        {
            DodajIzvrsioca(db, Majstor("m1", "Bakir"), poslova: 1, 4);
            DodajIzvrsioca(db, Majstor("m2", "Cerin"), poslova: 3, 5);
            DodajIzvrsioca(db, Majstor("m3", "Amar"), poslova: 3, 5);
            DodajIzvrsioca(db, Majstor("m4", "Dino"), poslova: 1, 5);
        });

        // Act
        await PokreniJedanKrug();

        // Assert
        CollectionAssert.AreEqual(new[] { "m3", "m2", "m4", "m1" }, _redovi.Select(r => r.IzvrsilacID).ToList());
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, _redovi.Select(r => r.RangStandardni).ToList());
    }

    [TestMethod]
    [Timeout(15000)]
    [Owner("Aid Mustafić")]
    public async Task ExecuteAsync_ViseOd15Izvrsilaca_RangListaImaNajvise15()
    {
        // Arrange
        await Sacuvaj(db =>
        {
            for (int i = 1; i <= 17; i++)
                DodajIzvrsioca(db, Majstor($"m{i:00}"), poslova: 1, 4);
        });

        // Act
        await PokreniJedanKrug();

        // Assert
        Assert.AreEqual(15, _redovi.Count);
    }

    [TestMethod]
    [Timeout(15000)]
    [Owner("Aid Mustafić")]
    public async Task ExecuteAsync_IzvrsilacNaListi_PopunjavaKategorijuMjestoIOcjenu()
    {
        // Arrange: prosjek 4 i 5 je 4.5, a 4, 4 i 5 je 4.333 pa se zaokružuje na 4.3
        await Sacuvaj(db => DodajIzvrsioca(db, Majstor("m1"), poslova: 2, 4, 4, 5));

        // Act
        await PokreniJedanKrug();

        // Assert
        var red = _redovi.Single();
        Assert.AreEqual("Vodoinstalacije", red.KategorijaNaziv);
        Assert.AreEqual("Sarajevo", red.MjestoNaziv);
        Assert.AreEqual(4.3m, red.ProsjecnaOcjena);
        Assert.AreEqual(2, red.BrojPoslova);
        Assert.AreEqual(_sada.Year * 100 + _sada.Month, red.Godina * 100 + red.Mjesec);
    }

    [TestMethod]
    [Timeout(15000)]
    [Owner("Aid Mustafić")]
    public async Task ExecuteAsync_FirmaSaUlogom_TipKorisnikaJeFirma()
    {
        // Arrange
        await Sacuvaj(db =>
        {
            DodajIzvrsioca(db, new Firma { Id = "f1", UserName = "f1", NazivFirme = "Elektro d.o.o.", Adresa = "Adresa" }, poslova: 1, 5);
            db.Add(new IdentityUserRole<string> { UserId = "f1", RoleId = "r-firma" });
        });

        // Act
        await PokreniJedanKrug();

        // Assert
        Assert.AreEqual(KorisnickeUloge.Firma, _redovi.Single().TipKorisnika);
    }

    [TestMethod]
    [Timeout(15000)]
    [Owner("Aid Mustafić")]
    public async Task ExecuteAsync_IzvrsilacBezUloge_TipKorisnikaJeMajstor()
    {
        // Arrange
        await Sacuvaj(db => DodajIzvrsioca(db, Majstor("m1"), poslova: 1, 5));

        // Act
        await PokreniJedanKrug();

        // Assert
        Assert.AreEqual(KorisnickeUloge.Majstor, _redovi.Single().TipKorisnika);
    }

    [TestMethod]
    [Timeout(15000)]
    [Owner("Aid Mustafić")]
    public async Task ExecuteAsync_PosaoIzProslogMjeseca_NeRacunaSe()
    {
        // Arrange
        var prosliMjesec = new DateTime(_sada.Year, _sada.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(-1);
        await Sacuvaj(db =>
        {
            DodajIzvrsioca(db, Majstor("m1"), poslova: 0);
            db.Add(new PonudaUsluge
            {
                IzvrsilacID = "m1", StatusPonude = Status.Isporuceno,
                DatumPocetkaUsluge = prosliMjesec, DatumIzvrsavanjaUsluge = prosliMjesec
            });
        });

        // Act
        await PokreniJedanKrug();

        // Assert
        Assert.AreEqual(0, _redovi.Count);
    }

    [TestMethod]
    [Timeout(15000)]
    [Owner("Aid Mustafić")]
    public async Task ExecuteAsync_IzvrsilacBezMjesta_UlaziNaListuBezMjesta()
    {
        // Arrange
        var bezMjesta = Majstor("m1");
        bezMjesta.Mjesta = new List<KorisnikMjesto>();
        await Sacuvaj(db => DodajIzvrsioca(db, bezMjesta, poslova: 1, 5));

        // Act
        var nivo = await PokreniJedanKrug();

        // Assert
        Assert.AreEqual(LogLevel.Information, nivo);
        Assert.IsNull(_redovi.Single().MjestoID);
    }

    [TestMethod]
    [Timeout(15000)]
    [Owner("Aid Mustafić")]
    public async Task ExecuteAsync_RepozitorijBaciIzuzetak_LogujeGresku()
    {
        // Arrange
        _repo.Setup(r => r.ZamijeniSnapshot(It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<IReadOnlyList<MjesecnaStatistikaKompozicija>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Baza nije dostupna"));

        // Act
        var nivo = await PokreniJedanKrug();

        // Assert
        Assert.AreEqual(LogLevel.Error, nivo);
    }
}
