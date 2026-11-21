using Popravka.ba.Data;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Models;
using PopravkaBa.Infrastructure.Repositories;

namespace PopravkaBa.Tests.Nalog;

// Pomoćne metode koje samo pripremaju podatke za testove
internal static class VerifikacijskiTokenRepositoryTestHelper
{
    internal static VerifikacijskiToken Token(
        string hash, string korisnikId = "k1", TipTokena tip = TipTokena.PotvrdaEmaila,
        int minutaOdGenerisanja = 10, DateTime? koristen = null) => new()
    {
        KorisnikID = korisnikId,
        TokenHash = hash,
        Tip = tip,
        VrijemeGenerisanja = DateTime.UtcNow.AddMinutes(-minutaOdGenerisanja),
        VrijemeIsteka = DateTime.UtcNow.AddMinutes(15 - minutaOdGenerisanja),
        VrijemeKoristenja = koristen
    };
}
