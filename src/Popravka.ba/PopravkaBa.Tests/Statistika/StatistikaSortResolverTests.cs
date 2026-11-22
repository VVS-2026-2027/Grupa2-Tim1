using PopravkaBa.Application.Strategies.Implementation;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Application.Strategies.Interface;

namespace PopravkaBa.Tests.Statistika;

[TestClass]
[TestCategory("Service")]
public class StatistikaSortResolverTests
{
    private StatistikaSortResolver _resolver = null!;

    [TestInitialize]
    public void Postavi()
    {
        // Iste strategije kao u Program.cs
        _resolver = new StatistikaSortResolver(new IStatistikaStrategy[]
        {
            new SortStatistikaMajstor(), new SortStatistikaKategorija(), new SortStatistikaMjesto(),
            new SortStatistikaOcjena(), new SortStatistikaPoslovi()
        });
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    [DataRow(KoloneStatistike.Majstor)]
    [DataRow(KoloneStatistike.Kategorija)]
    [DataRow(KoloneStatistike.Lokacija)]
    [DataRow(KoloneStatistike.Ocjena)]
    [DataRow(KoloneStatistike.Poslovi)]
    public void SortPo_PostojecaKolona_VracaStrategijuZaTuKolonu(KoloneStatistike kolona)
    {
        // Act
        var strategija = _resolver.SortPo(kolona);

        // Assert
        Assert.AreEqual(kolona, strategija.KolonaSortiranja);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public void SortPo_KolonaBezStrategije_VracaSortiranjePoOcjeni()
    {
        // Arrange: resolver ima samo strategiju za ocjenu
        var resolver = new StatistikaSortResolver(new IStatistikaStrategy[] { new SortStatistikaOcjena() });

        // Act
        var strategija = resolver.SortPo(KoloneStatistike.Poslovi);

        // Assert
        Assert.AreEqual(KoloneStatistike.Ocjena, strategija.KolonaSortiranja);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public void Konstruktor_BezStrategijeZaOcjenu_BacaKeyNotFoundException()
    {
        // Act + Assert: ocjena je zadana kolona, pa bez nje resolver ne može raditi
        Assert.ThrowsExactly<KeyNotFoundException>(() =>
            new StatistikaSortResolver(new IStatistikaStrategy[] { new SortStatistikaPoslovi() }));
    }
}
