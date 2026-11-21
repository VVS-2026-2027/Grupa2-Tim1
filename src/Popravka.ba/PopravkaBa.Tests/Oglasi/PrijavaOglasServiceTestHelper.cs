using PopravkaBa.Application.DTOs;
using PopravkaBa.Application.Services.Implementation;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Models;

namespace PopravkaBa.Tests.Oglasi;

internal static class PrijavaOglasServiceTestHelper
{
    internal const int OglasId = 10;
    internal const string MajstorId = "m1";

    internal static OglasRadnoMjesto PostaviOglas(Mock<IOglasRadnoMjestoRepository> oglasi,
        int brojIzvrsilaca, Status status = Status.Aktivan)
    {
        var oglas = new OglasRadnoMjesto
        {
            OglasID = OglasId,
            BrojIzvrsilaca = brojIzvrsilaca,
            StatusOglasa = status
        };

        oglasi.Setup(r => r.DajPoIdAsync(OglasId)).ReturnsAsync(oglas);
        return oglas;
    }

    internal static PrijavaRadnoMjesto NovaPrijava(
        int id, string majstorId = MajstorId, Status status = Status.NaCekanju) => new()
    {
        ID = id,
        OglasID = OglasId,
        MajstorID = majstorId,
        StatusPrijave = status
    };

    internal static KreirajPrijavaRadnoMjestoDto NoviZahtjev() => new()
    {
        OglasID = OglasId,
        MajstorID = MajstorId
    };
}
