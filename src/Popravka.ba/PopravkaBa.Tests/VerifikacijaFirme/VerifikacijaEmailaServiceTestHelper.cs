using Microsoft.AspNetCore.Identity;
using PopravkaBa.Application.Services.Implementation;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Models;

namespace PopravkaBa.Tests.VerifikacijaEmaila;

// Pomoćne metode koje samo pripremaju podatke za testove
internal static class VerifikacijaEmailaServiceTestHelper
{
    internal const string HashSirovogTokena = "A81AA4D06D67C8A71A50FEC7796D0008FC87DA4CE398CBCFD94C94F4B6129285";

    internal static ApplicationUser Korisnik() => new() { Id = "k1", Email = "k1@popravka.ba" };

    internal static VerifikacijskiToken Token(DateTime? koristen = null, int minutaDoIsteka = 10) => new()
    {
        ID = 1,
        KorisnikID = "k1",
        TokenHash = HashSirovogTokena,
        Tip = TipTokena.PotvrdaEmaila,
        VrijemeGenerisanja = DateTime.UtcNow.AddMinutes(-5),
        VrijemeIsteka = DateTime.UtcNow.AddMinutes(minutaDoIsteka),
        VrijemeKoristenja = koristen
    };
}
