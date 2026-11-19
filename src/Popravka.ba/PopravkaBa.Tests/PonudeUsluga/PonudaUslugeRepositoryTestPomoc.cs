using Popravka.ba.Data;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Models;
using PopravkaBa.Infrastructure.Repositories;

namespace PopravkaBa.Tests.PonudeUsluga;

internal static class PonudaUslugeRepositoryTestPomoc
{
    internal static async Task UbaciPonude(ApplicationDbContext baza, params PonudaUsluge[] ponude)
    {
        baza.PonudeUsluge.AddRange(ponude);
        await baza.SaveChangesAsync();
        baza.ChangeTracker.Clear();
    }

    internal static OglasUsluge NoviOglas(int id, string vlasnikId) => new()
    {
        OglasID = id,
        VlasnikOglasaID = vlasnikId,
        MjestoID = 1,
        Naslov = $"Oglas {id}",
        Opis = "Opis testnog oglasa"
    };

    internal static PonudaUsluge NovaPonuda(
        int id, int oglasId = 10, string izvrsilacId = "m1", int? cijena = 100) => new()
    {
        ID = id,
        OglasUslugeID = oglasId,
        IzvrsilacID = izvrsilacId,
        Cijena = cijena,
        DatumSlanja = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(id),
        DatumPocetkaUsluge = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc)
    };
}
