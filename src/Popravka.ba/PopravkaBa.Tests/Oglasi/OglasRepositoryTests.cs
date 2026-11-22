using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Models;
using PopravkaBa.Infrastructure.Repositories;
using static PopravkaBa.Tests.Oglasi.OglasiTestHelper;

namespace PopravkaBa.Tests.Oglasi;

// OglasRepository samo spaja nedavne oglase iz dva druga repozitorija, pa su oni mock
[TestClass]
[TestCategory("Repository")]
public class OglasRepositoryTests
{
    private Mock<IOglasUslugeRepository> _usluge = null!;
    private Mock<IOglasRadnoMjestoRepository> _radnaMjesta = null!;
    private OglasRepository _repo = null!;

    [TestInitialize]
    public void Postavi()
    {
        _usluge = new Mock<IOglasUslugeRepository>();
        _radnaMjesta = new Mock<IOglasRadnoMjestoRepository>();
        _repo = new OglasRepository(TestHelpers.NovaBaza(), _usluge.Object, _radnaMjesta.Object);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DajNedavneAsync_OglasiObaTipa_SpajaIhOdNajnovijeg()
    {
        // Arrange (veći id = kasniji datum objave)
        _usluge.Setup(r => r.DajNedavneAsync(4)).ReturnsAsync(new[] { OglasUsluge(1), OglasUsluge(4) });
        _radnaMjesta.Setup(r => r.DajNedavneAsync(4)).ReturnsAsync(new[] { OglasRadnoMjesto(3), OglasRadnoMjesto(2) });

        // Act
        var oglasi = (await _repo.DajNedavneAsync(4))!.Select(o => o.OglasID).ToList();

        // Assert
        CollectionAssert.AreEqual(new[] { 4, 3, 2, 1 }, oglasi);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DajNedavneAsync_ViseOdTopN_VracaSamoTopNNajnovijih()
    {
        // Arrange
        _usluge.Setup(r => r.DajNedavneAsync(2)).ReturnsAsync(new[] { OglasUsluge(1), OglasUsluge(3) });
        _radnaMjesta.Setup(r => r.DajNedavneAsync(2)).ReturnsAsync(new[] { OglasRadnoMjesto(2) });

        // Act
        var oglasi = (await _repo.DajNedavneAsync(2))!.Select(o => o.OglasID).ToList();

        // Assert
        CollectionAssert.AreEqual(new[] { 3, 2 }, oglasi);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DajNedavneAsync_RadnaMjestaNull_VracaSamoOglaseUsluga()
    {
        // Arrange
        _usluge.Setup(r => r.DajNedavneAsync(4)).ReturnsAsync(new[] { OglasUsluge(1) });
        _radnaMjesta.Setup(r => r.DajNedavneAsync(4)).ReturnsAsync((IEnumerable<OglasRadnoMjesto>?)null);

        // Act
        var oglasi = (await _repo.DajNedavneAsync(4))!.ToList();

        // Assert
        Assert.AreEqual(1, oglasi.Single().OglasID);
    }
}
