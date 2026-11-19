using PopravkaBa.Application.DTOs;
using PopravkaBa.Application.Services.Implementation;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Models;

namespace PopravkaBa.Tests.PonudeUsluga;

internal static class PonudaUslugeServiceTestPomoc
{
    internal const int OglasId = 10;
    internal const string KlijentId = "k1";
    internal const string IzvrsilacId = "m1";

    internal static OglasUsluge PostaviOglas(Mock<IOglasUslugeRepository> oglasi, Status status)
    {
        var oglas = new OglasUsluge
        {
            VlasnikOglasaID = KlijentId,
            StatusOglasa = status
        };
        oglasi.Setup(r => r.DajPoIdAsync(OglasId)).ReturnsAsync(oglas);
        return oglas;
    }

    internal static PonudaUsluge NovaPonuda(int id, Status status) => new()
    {
        ID = id,
        OglasUslugeID = OglasId,
        IzvrsilacID = IzvrsilacId,
        StatusPonude = status
    };

    internal static KreirajPonudaUslugeDto NovaObjava(DateTime? datumZavrsetka) => new()
    {
        OglasUslugeID = OglasId,
        DatumPocetkaUsluge = new DateTime(2026, 11, 22),
        DatumOcekivanogZavrsetka = datumZavrsetka,
        Cijena = 100,
        Poruka = "Mogu preuzeti posao."
    };
}
