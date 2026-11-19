using PopravkaBa.Application.DTOs;
using PopravkaBa.Application.Services.Implementation;
using PopravkaBa.Application.Strategies.Interface;
using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Models;
using PopravkaBa.Domain.Specifications.Interface;
using PopravkaBa.Infrastructure.Wrappers;

namespace PopravkaBa.Tests.Pretraga;

internal static class PretragaServiceTestPomoc
{
    internal static FilterPretrageDto NoviFilter(string tab) => new()
    {
        AktivniTab = tab,
        Stranica = 2,
        StavkiPoStranici = 5
    };

    internal static Mjesto NovoMjesto() => new() { Naziv = "Sarajevo" };

    internal static Klijent NoviVlasnik() => new()
    {
        Ime = "Aida",
        Prezime = "Kovač",
        Slika = "profil.png"
    };

    internal static void ProvjeriPaginaciju(RezultatPretrageDto rezultat, string tab)
    {
        Assert.AreEqual(tab, rezultat.AktivniTab);
        Assert.AreEqual(2, rezultat.TrenutnaStrana);
        Assert.AreEqual(5, rezultat.StavkiPoStranici);
        Assert.AreEqual(12, rezultat.UkupnoStavki);
        Assert.AreEqual(3, rezultat.UkupnoStranica);
        Assert.AreEqual(6, rezultat.PrikazOd);
        Assert.AreEqual(10, rezultat.PrikazDo);
    }
}
