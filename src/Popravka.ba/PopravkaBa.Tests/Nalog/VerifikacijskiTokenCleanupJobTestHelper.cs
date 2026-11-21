using Microsoft.EntityFrameworkCore;
using Popravka.ba.Data;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Models;

namespace PopravkaBa.Tests.Nalog;

// Pomoćne metode za testove čišćenja tokena nad SQLite bazom
internal static class VerifikacijskiTokenCleanupJobTestHelper
{
    // Korisnik kojem pripadaju tokeni (SQLite provjerava strani ključ, pa korisnik mora postojati)
    internal static Klijent Vlasnik() => new() { Id = "k1", UserName = "haso", Email = "haso@popravka.ba" };

    internal static VerifikacijskiToken Token(string hash, int minutaDoIsteka) => new()
    {
        KorisnikID = "k1",
        TokenHash = hash,
        Tip = TipTokena.PotvrdaEmaila,
        VrijemeGenerisanja = DateTime.UtcNow.AddMinutes(-30),
        VrijemeIsteka = DateTime.UtcNow.AddMinutes(minutaDoIsteka)
    };
}

// DbContext koji javi testu kad ga job zatvori. Job zatvara kontekst na kraju svakog kruga čišćenja,
// pa test tako zna da je krug gotov, i kad job ništa ne loguje.
internal sealed class KontekstKojiJavljaKrajKruga : ApplicationDbContext
{
    private readonly TaskCompletionSource _krajKruga;

    public KontekstKojiJavljaKrajKruga(DbContextOptions<ApplicationDbContext> opcije, TaskCompletionSource krajKruga)
        : base(opcije) => _krajKruga = krajKruga;

    public override void Dispose()
    {
        base.Dispose();
        _krajKruga.TrySetResult();
    }
}
