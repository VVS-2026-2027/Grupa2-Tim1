using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Popravka.ba.Data;
using PopravkaBa.Infrastructure.BackgroundServices;
using static PopravkaBa.Tests.Nalog.VerifikacijskiTokenCleanupJobTestHelper;

namespace PopravkaBa.Tests.Nalog;

// Job briše tokene sa ExecuteDeleteAsync. Dva načina testiranja:
// - _job radi nad InMemory bazom, koja ExecuteDeleteAsync ne podržava, pa se tu testira ponašanje kod greške
//   (loguje je i nastavlja raditi);
// - _jobSqlite radi nad SQLite bazom u memoriji, gdje brisanje stvarno radi, pa se tu testira uspješno čišćenje.
[TestClass]
[TestCategory("Infrastructure")]
public class VerifikacijskiTokenCleanupJobTests
{
    private Mock<ILogger<VerifikacijskiTokenCleanupJob>> _logger = null!;
    private TaskCompletionSource _logiranje = null!;
    private ApplicationDbContext _db = null!;
    private ServiceProvider _provider = null!;
    private VerifikacijskiTokenCleanupJob _job = null!;

    private SqliteTestBaza _sqlite = null!;
    private TaskCompletionSource _krajKruga = null!;
    private ServiceProvider _providerSqlite = null!;
    private VerifikacijskiTokenCleanupJob _jobSqlite = null!;

    [TestInitialize]
    public void Postavi()
    {
        _logiranje = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _logger = new Mock<ILogger<VerifikacijskiTokenCleanupJob>>();
        // Svaki poziv logera javlja testu da je job odradio prvi krug čišćenja
        _logger.Setup(l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()))
            .Callback(new InvocationAction(_ => _logiranje.TrySetResult()));

        // Pravi DI kontejner da job može napraviti scope i dobiti DbContext
        _db = TestHelpers.NovaBaza();
        _provider = new ServiceCollection().AddScoped(_ => _db).BuildServiceProvider();
        _job = new VerifikacijskiTokenCleanupJob(
            _provider.GetRequiredService<IServiceScopeFactory>(), _logger.Object);

        // SQLite: svaki scope dobija kontekst koji javi testu kad ga job zatvori na kraju kruga
        _sqlite = new SqliteTestBaza();
        _krajKruga = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _providerSqlite = new ServiceCollection()
            .AddScoped<ApplicationDbContext>(_ => new KontekstKojiJavljaKrajKruga(_sqlite.Opcije, _krajKruga))
            .BuildServiceProvider();
        _jobSqlite = new VerifikacijskiTokenCleanupJob(
            _providerSqlite.GetRequiredService<IServiceScopeFactory>(), _logger.Object);
    }

    [TestCleanup]
    public void Pocisti()
    {
        _job.Dispose();
        _provider.Dispose();
        _jobSqlite.Dispose();
        _providerSqlite.Dispose();
        _sqlite.Dispose();
    }

    [TestMethod]
    [Timeout(15000)]
    [Owner("Emina Vatrić")]
    public async Task ExecuteAsync_GreskaPriBrisanjuTokena_LogujeGresku()
    {
        // Arrange (InMemory baza baca izuzetak na ExecuteDeleteAsync)

        // Act
        await _job.StartAsync(CancellationToken.None);
        await _logiranje.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await _job.StopAsync(CancellationToken.None);

        // Assert
        _logger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()), Times.Once);
    }

    [TestMethod]
    [Timeout(15000)]
    [Owner("Emina Vatrić")]
    public async Task ExecuteAsync_GreskaPriBrisanjuTokena_JobNastavljaRadIPoslijeGreske()
    {
        // Act
        await _job.StartAsync(CancellationToken.None);
        await _logiranje.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var jobZavrsen = _job.ExecuteTask!.IsCompleted;
        await _job.StopAsync(CancellationToken.None);

        // Assert
        Assert.IsFalse(jobZavrsen);
    }

    [TestMethod]
    [Timeout(15000)]
    [Owner("Emina Vatrić")]
    public async Task StopAsync_JobUToku_ZavrsavaSeBezIzuzetka()
    {
        // Arrange
        await _job.StartAsync(CancellationToken.None);
        await _logiranje.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // Act
        await _job.StopAsync(CancellationToken.None);

        // Assert
        Assert.IsTrue(_job.ExecuteTask!.IsCompleted);
    }

    // ---------- Uspješno čišćenje (SQLite) ----------

    [TestMethod]
    [Timeout(15000)]
    [Owner("Emina Vatrić")]
    public async Task ExecuteAsync_IstekliIVazeciTokeni_BriseSamoIstekle()
    {
        // Arrange: jedan token je istekao prije 5 minuta, drugi ističe za 10 minuta
        using (var db = _sqlite.NoviKontekst())
        {
            db.Users.Add(Vlasnik());
            db.Tokeni.AddRange(Token("istekao", -5), Token("vazeci", 10));
            await db.SaveChangesAsync();
        }

        // Act
        await _jobSqlite.StartAsync(CancellationToken.None);
        await _krajKruga.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await _jobSqlite.StopAsync(CancellationToken.None);

        // Assert
        using var provjera = _sqlite.NoviKontekst();
        CollectionAssert.AreEqual(new[] { "vazeci" }, provjera.Tokeni.Select(t => t.TokenHash).ToArray());
    }

    [TestMethod]
    [Timeout(15000)]
    [Owner("Emina Vatrić")]
    public async Task ExecuteAsync_ImaIsteklihTokena_LogujeBrojObrisanih()
    {
        // Arrange: dva istekla tokena
        using (var db = _sqlite.NoviKontekst())
        {
            db.Users.Add(Vlasnik());
            db.Tokeni.AddRange(Token("prvi", -5), Token("drugi", -60));
            await db.SaveChangesAsync();
        }

        // Act
        await _jobSqlite.StartAsync(CancellationToken.None);
        await _krajKruga.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await _jobSqlite.StopAsync(CancellationToken.None);

        // Assert: jedna Information poruka koja sadrži broj obrisanih tokena
        _logger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((poruka, _) => poruka.ToString()!.Contains("Obrisano 2")),
            null,
            (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()), Times.Once);
    }

    [TestMethod]
    [Timeout(15000)]
    [Owner("Emina Vatrić")]
    public async Task ExecuteAsync_NemaIsteklihTokena_NistaNeLoguje()
    {
        // Arrange: samo važeći token
        using (var db = _sqlite.NoviKontekst())
        {
            db.Users.Add(Vlasnik());
            db.Tokeni.Add(Token("vazeci", 10));
            await db.SaveChangesAsync();
        }

        // Act
        await _jobSqlite.StartAsync(CancellationToken.None);
        await _krajKruga.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await _jobSqlite.StopAsync(CancellationToken.None);

        // Assert: krug je završen, a logger nije pozvan (ni informacija ni greška)
        _logger.Verify(l => l.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()), Times.Never);
    }
}
