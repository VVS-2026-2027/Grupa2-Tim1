using PopravkaBa.Application.Strategies.Implementation;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Application.Strategies.Interface;
using PopravkaBa.Domain.Models;
using static PopravkaBa.Tests.Statistika.StatistikaTestHelper;

namespace PopravkaBa.Tests.Statistika;

[TestClass]
[TestCategory("Service")]
public class StatistikaStrategyTests
{
    // Tri reda gdje je redoslijed različit po svakoj koloni
    private static IQueryable<MjesecnaStatistikaKompozicija> Redovi() => new[]
    {
        Red("a", ocjena: 4.0m, poslovi: 9, kategorija: "Stolarija", mjesto: "Zenica", ime: "Bakir"),
        Red("b", ocjena: 5.0m, poslovi: 1, kategorija: "Elektro", mjesto: "Mostar", ime: "Amar"),
        Red("c", ocjena: 3.0m, poslovi: 5, kategorija: "Vodoinstalacije", mjesto: "Sarajevo", ime: "Cerin")
    }.AsQueryable();

    private static string[] Poredaj(IStatistikaStrategy strategija, Sortiranje smjer) =>
        strategija.Primijeni(Redovi(), smjer).Select(r => r.IzvrsilacID).ToArray();

    [TestMethod]
    [Owner("Aid Mustafić")]
    public void Primijeni_OcjenaOpadajuce_NajboljaOcjenaPrva()
    {
        // Act
        var redoslijed = Poredaj(new SortStatistikaOcjena(), Sortiranje.Descending);

        // Assert
        CollectionAssert.AreEqual(new[] { "b", "a", "c" }, redoslijed);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public void Primijeni_OcjenaRastuce_NajlosijaOcjenaPrva()
    {
        // Act
        var redoslijed = Poredaj(new SortStatistikaOcjena(), Sortiranje.Ascending);

        // Assert
        CollectionAssert.AreEqual(new[] { "c", "a", "b" }, redoslijed);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public void Primijeni_PosloviOpadajuce_NajviseZavrsenihPoslovaPrvi()
    {
        // Act
        var redoslijed = Poredaj(new SortStatistikaPoslovi(), Sortiranje.Descending);

        // Assert
        CollectionAssert.AreEqual(new[] { "a", "c", "b" }, redoslijed);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public void Primijeni_MajstorRastuce_AbecednoPoImenu()
    {
        // Act
        var redoslijed = Poredaj(new SortStatistikaMajstor(), Sortiranje.Ascending);

        // Assert
        CollectionAssert.AreEqual(new[] { "b", "a", "c" }, redoslijed);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public void Primijeni_KategorijaRastuce_AbecednoPoKategoriji()
    {
        // Act
        var redoslijed = Poredaj(new SortStatistikaKategorija(), Sortiranje.Ascending);

        // Assert
        CollectionAssert.AreEqual(new[] { "b", "a", "c" }, redoslijed);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public void Primijeni_MjestoOpadajuce_ObrnutoAbecednoPoMjestu()
    {
        // Act
        var redoslijed = Poredaj(new SortStatistikaMjesto(), Sortiranje.Descending);

        // Assert
        CollectionAssert.AreEqual(new[] { "a", "c", "b" }, redoslijed);
    }
}
