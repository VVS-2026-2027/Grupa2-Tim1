using PopravkaBa.Application.Services;
using PopravkaBa.Domain.Interfaces.Repositories;
using PopravkaBa.Domain.Models;

namespace PopravkaBa.Tests.Zajednicko;

// Pomoćne metode koje samo pripremaju podatke za testove
internal static class KategorijaServiceTestHelper
{
    internal static Kategorija Kategorija(int id, string naziv) => new() { ID = id, Naziv = naziv };
}
