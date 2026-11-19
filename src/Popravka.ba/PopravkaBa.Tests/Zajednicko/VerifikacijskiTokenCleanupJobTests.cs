using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Popravka.ba.Data;
using PopravkaBa.Infrastructure.BackgroundServices;

namespace PopravkaBa.Tests.Tokeni;

// Job briše tokene sa ExecuteDeleteAsync, koji InMemory baza ne podržava. Zato se ovdje testira
// kako se job ponaša kad baza baci grešku: loguje je i nastavlja raditi (ne ruši aplikaciju).
[TestClass]
public class VerifikacijskiTokenCleanupJobTests
{
    private Mock<ILogger<VerifikacijskiTokenCleanupJob>> _logger = null!;
    private TaskCompletionSource _logiranje = null!;
    private ApplicationDbContext _db = null!;
    private ServiceProvider _provider = null!;
    private VerifikacijskiTokenCleanupJob _job = null!;

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
    }

    [TestCleanup]
    public void Pocisti()
    {
        _job.Dispose();
        _provider.Dispose();
    }

    [TestMethod]
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
}
