using PopravkaBa.Application.Strategies.Helper;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Models;
using PopravkaBa.Domain.Specifications.Subtype;

namespace PopravkaBa.Tests.Pretraga;

[TestClass]
[TestCategory("Domain")]
public class SortiranjeHelperTests
{
    [TestMethod]
    [Owner("Amina Bukvić")]
    [DataRow(SortiranjeIzvrsilacaUsluga.ProsjecnaOcjena_Asc, false, "m2,m3,m1")]
    [DataRow(SortiranjeIzvrsilacaUsluga.ProsjecnaOcjena_Desc, true, "m1,m3,m2")]
    [DataRow(SortiranjeIzvrsilacaUsluga.MinCijena_Asc, false, "m1,m2,m3")]
    [DataRow(SortiranjeIzvrsilacaUsluga.MinCijena_Desc, true, "m2,m1,m3")]
    public void ApplyIzvrsilacSortiranje_PoznataOpcija_SortiraIzvrsioce(
        SortiranjeIzvrsilacaUsluga opcija, bool opadajuce, string ocekivaniRedoslijed)
    {
        // Arrange
        var osnovna = new AllSpecification<IzvrsilacUsluge>();
        IzvrsilacUsluge[] izvrsioci =
        {
            new Majstor { Id = "m1", ProsjecnaOcjena = 5m, MinCijenaUsluge = 30 },
            new Majstor { Id = "m2", ProsjecnaOcjena = 2m, MinCijenaUsluge = 70 },
            new Majstor { Id = "m3", ProsjecnaOcjena = 3m, MinCijenaUsluge = null }
        };

        // Act
        var rezultat = SortiranjeHelper.ApplyIzvrsilacSortiranje(osnovna, opcija);

        // Assert
        Assert.AreEqual(opadajuce, rezultat.OrderByDescending is not null);
        Assert.AreEqual(!opadajuce, rezultat.OrderBy is not null);
        var sortirani = rezultat.OrderByDescending is { } silazni
            ? izvrsioci.OrderByDescending(silazni.Compile())
            : izvrsioci.OrderBy(rezultat.OrderBy!.Compile());
        CollectionAssert.AreEqual(
            ocekivaniRedoslijed.Split(','), sortirani.Select(m => m.Id).ToArray());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    [DataRow(SortiranjeOglasaUsluge.MinBudzet_Asc, false, "1,2,3")]
    [DataRow(SortiranjeOglasaUsluge.MinBudzet_Desc, true, "3,2,1")]
    [DataRow(SortiranjeOglasaUsluge.MaxBudzet_Asc, false, "2,3,1")]
    [DataRow(SortiranjeOglasaUsluge.MaxBudzet_Desc, true, "1,3,2")]
    public void ApplyUslugeSortiranje_PoznataOpcija_SortiraOglasePoBudzetu(
        SortiranjeOglasaUsluge opcija, bool opadajuce, string ocekivaniRedoslijed)
    {
        // Arrange
        var osnovna = new AllSpecification<OglasUsluge>();
        var oglasi = new[]
        {
            new OglasUsluge { OglasID = 1, MinBudzet = 10, MaxBudzet = 100 },
            new OglasUsluge { OglasID = 2, MinBudzet = 20, MaxBudzet = 60 },
            new OglasUsluge { OglasID = 3, MinBudzet = 30, MaxBudzet = 80 }
        };

        // Act
        var rezultat = SortiranjeHelper.ApplyUslugeSortiranje(osnovna, opcija);

        // Assert
        Assert.AreEqual(opadajuce, rezultat.OrderByDescending is not null);
        Assert.AreEqual(!opadajuce, rezultat.OrderBy is not null);
        var sortirani = rezultat.OrderByDescending is { } silazni
            ? oglasi.OrderByDescending(silazni.Compile())
            : oglasi.OrderBy(rezultat.OrderBy!.Compile());
        CollectionAssert.AreEqual(
            ocekivaniRedoslijed.Split(',').Select(int.Parse).ToArray(),
            sortirani.Select(o => o.OglasID).ToArray());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    [DataRow(SortiranjeOglasaRadnoMjesto.MinPrihod_Asc, false, "1,2,3")]
    [DataRow(SortiranjeOglasaRadnoMjesto.MinPrihod_Desc, true, "3,2,1")]
    [DataRow(SortiranjeOglasaRadnoMjesto.MaxPrihod_Asc, false, "2,3,1")]
    [DataRow(SortiranjeOglasaRadnoMjesto.MaxPrihod_Desc, true, "1,3,2")]
    public void ApplyRadnoMjestoSortiranje_PoznataOpcija_SortiraOglasePoPrihodu(
        SortiranjeOglasaRadnoMjesto opcija, bool opadajuce, string ocekivaniRedoslijed)
    {
        // Arrange
        var osnovna = new AllSpecification<OglasRadnoMjesto>();
        var oglasi = new[]
        {
            new OglasRadnoMjesto { OglasID = 1, MinPrihod = 500, MaxPrihod = 1500 },
            new OglasRadnoMjesto { OglasID = 2, MinPrihod = 600, MaxPrihod = 1000 },
            new OglasRadnoMjesto { OglasID = 3, MinPrihod = 700, MaxPrihod = 1200 }
        };

        // Act
        var rezultat = SortiranjeHelper.ApplyRadnoMjestoSortiranje(osnovna, opcija);

        // Assert
        Assert.AreEqual(opadajuce, rezultat.OrderByDescending is not null);
        Assert.AreEqual(!opadajuce, rezultat.OrderBy is not null);
        var sortirani = rezultat.OrderByDescending is { } silazni
            ? oglasi.OrderByDescending(silazni.Compile())
            : oglasi.OrderBy(rezultat.OrderBy!.Compile());
        CollectionAssert.AreEqual(
            ocekivaniRedoslijed.Split(',').Select(int.Parse).ToArray(),
            sortirani.Select(o => o.OglasID).ToArray());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public void Sortiranje_BezOpcije_VracaIzvorneSpecifikacije()
    {
        // Arrange
        var izvrsioci = new AllSpecification<IzvrsilacUsluge>();
        var usluge = new AllSpecification<OglasUsluge>();
        var radnaMjesta = new AllSpecification<OglasRadnoMjesto>();

        // Act
        var rezultatIzvrsilaca = SortiranjeHelper.ApplyIzvrsilacSortiranje(izvrsioci, null);
        var rezultatUsluga = SortiranjeHelper.ApplyUslugeSortiranje(usluge, null);
        var rezultatRadnihMjesta = SortiranjeHelper.ApplyRadnoMjestoSortiranje(radnaMjesta, null);

        // Assert
        Assert.AreSame(izvrsioci, rezultatIzvrsilaca);
        Assert.AreSame(usluge, rezultatUsluga);
        Assert.AreSame(radnaMjesta, rezultatRadnihMjesta);
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public void Sortiranje_NepoznataOpcija_VracaIzvorneSpecifikacije()
    {
        // Arrange
        const int nepoznataOpcija = 999;
        var izvrsioci = new AllSpecification<IzvrsilacUsluge>();
        var usluge = new AllSpecification<OglasUsluge>();
        var radnaMjesta = new AllSpecification<OglasRadnoMjesto>();

        // Act
        var rezultatIzvrsilaca = SortiranjeHelper.ApplyIzvrsilacSortiranje(
            izvrsioci, (SortiranjeIzvrsilacaUsluga)nepoznataOpcija);
        var rezultatUsluga = SortiranjeHelper.ApplyUslugeSortiranje(
            usluge, (SortiranjeOglasaUsluge)nepoznataOpcija);
        var rezultatRadnihMjesta = SortiranjeHelper.ApplyRadnoMjestoSortiranje(
            radnaMjesta, (SortiranjeOglasaRadnoMjesto)nepoznataOpcija);

        // Assert
        Assert.AreSame(izvrsioci, rezultatIzvrsilaca);
        Assert.AreSame(usluge, rezultatUsluga);
        Assert.AreSame(radnaMjesta, rezultatRadnihMjesta);
    }
}