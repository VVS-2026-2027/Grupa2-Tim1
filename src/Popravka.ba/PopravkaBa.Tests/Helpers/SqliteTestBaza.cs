using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Popravka.ba.Data;

namespace PopravkaBa.Tests.Helpers;

// Prava SQL baza koja živi samo u memoriji dok traje test.
// Koristi se samo kad InMemory baza nije dovoljna, npr. za ExecuteDeleteAsync, koji InMemory ne podržava.
// Baza postoji dok je konekcija otvorena, zato se konekcija zatvara tek u Dispose.
public sealed class SqliteTestBaza : IDisposable
{
    private readonly SqliteConnection _konekcija;

    public DbContextOptions<ApplicationDbContext> Opcije { get; }

    public SqliteTestBaza()
    {
        _konekcija = new SqliteConnection("DataSource=:memory:");
        _konekcija.Open();
        Opcije = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_konekcija).Options;

        using var db = new ApplicationDbContext(Opcije);
        db.Database.EnsureCreated();
    }

    // Novi DbContext nad istom bazom (podaci ostaju između konteksta)
    public ApplicationDbContext NoviKontekst() => new(Opcije);

    public void Dispose() => _konekcija.Dispose();
}
