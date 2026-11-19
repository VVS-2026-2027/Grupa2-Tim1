using Popravka.ba.Data;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Models;
using PopravkaBa.Infrastructure.Repositories;

namespace PopravkaBa.Tests.Recenzije;

internal static class RecenzijaRepositoryTestPomoc
{
    internal static Recenzija NovaRecenzija(
        int id, string klijentId = "k1", string izvrsilacId = "m1")
    {
        return new Recenzija
        {
            RecenzijaID = id,
            KlijentID = klijentId,
            IzvrsilacID = izvrsilacId,
            Ocjena = 4,
            Komentar = "Testna recenzija",
            DatumRecenzije = new DateTime(2026, 11, 1).AddDays(id)
        };
    }

    internal static async Task UbaciRecenzije(ApplicationDbContext baza, params Recenzija[] recenzije)
    {
        baza.Recenzije.AddRange(recenzije);
        await baza.SaveChangesAsync();
        baza.ChangeTracker.Clear();
    }
}
