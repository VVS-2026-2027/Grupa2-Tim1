using Popravka.ba.Data;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Models;
using PopravkaBa.Infrastructure.Repositories;

namespace PopravkaBa.Tests.PrijaveOglasRadnoMjesto;

internal static class PrijavaRadnoMjestoRepositoryTestHelper
{
    internal static async Task UbaciPrijave(ApplicationDbContext baza, params PrijavaRadnoMjesto[] prijave)
    {
        baza.PrijaveRadnoMjesto.AddRange(prijave);
        await baza.SaveChangesAsync();
        baza.ChangeTracker.Clear();
    }

    internal static OglasRadnoMjesto NoviOglas(int id) => new()
    {
        OglasID = id,
        VlasnikOglasaID = "k1",
        MjestoID = 1,
        Naslov = $"Radno mjesto {id}",
        Opis = "Opis testnog oglasa"
    };

    internal static PrijavaRadnoMjesto NovaPrijava(
        int id, int oglasId = 10, string majstorId = "m1") => new()
    {
        ID = id,
        OglasID = oglasId,
        MajstorID = majstorId,
        StatusPrijave = Status.NaCekanju,
        VrijemePrijave = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddDays(id)
    };
}
