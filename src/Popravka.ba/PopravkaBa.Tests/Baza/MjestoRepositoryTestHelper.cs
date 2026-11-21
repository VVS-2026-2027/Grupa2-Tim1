using Popravka.ba.Data;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Models;
using PopravkaBa.Infrastructure.Repositories;

namespace PopravkaBa.Tests.Baza;

// Pomoćne metode koje samo pripremaju podatke za testove
internal static class MjestoRepositoryTestHelper
{
    internal static Mjesto Mjesto(int id, string naziv, Kanton kanton = Kanton.Sarajevski) =>
        new() { MjestoID = id, Naziv = naziv, Kanton = kanton };
}
