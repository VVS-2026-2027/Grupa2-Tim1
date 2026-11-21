using Microsoft.AspNetCore.Identity;
using PopravkaBa.Application.Services.Implementation;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Models;

namespace PopravkaBa.Tests.Nalog;

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

    // Priprema repozitorij i UserManager prema redu iz XML-a
    internal static void PostaviStanje(Mock<IVerifikacijskiTokenRepository> tokenRepo, Mock<UserManager<ApplicationUser>> userManager,
        bool tokenPostoji, bool iskoristen, bool istekao, bool korisnikPostoji)
    {
        var token = tokenPostoji
            ? Token(iskoristen ? DateTime.UtcNow : null, istekao ? -1 : 10)
            : null;
        tokenRepo.Setup(r => r.DajTokenPoHashuAsync(It.IsAny<string>(), It.IsAny<TipTokena>())).ReturnsAsync(token);
        userManager.PronadjiPoId(korisnikPostoji ? Korisnik() : null, "k1");
    }
}
