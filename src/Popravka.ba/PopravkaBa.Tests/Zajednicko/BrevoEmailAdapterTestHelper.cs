using System.Net.Sockets;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Models;
using PopravkaBa.Infrastructure.Adapters;
using PopravkaBa.Infrastructure.Adapters.Options;

namespace PopravkaBa.Tests.Zajednicko;

// Pomoćne metode koje samo pripremaju podatke za testove
internal static class BrevoEmailAdapterTestHelper
{
    internal static EmailVerifikacijaFirme NovaVerifikacija(string adminEmail, string firmaEmail) => new()
    {
        NazivFirme = "Haso instalacije d.o.o.",
        AdminEmail = adminEmail,
        StatusVerifikacije = Status.Prihvaceno,
        Firma = new Firma { Email = firmaEmail, NazivFirme = "Haso instalacije d.o.o." }
    };
}
