using PopravkaBa.Application.Services;
using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Models;
using static PopravkaBa.Tests.Oglasi.OglasiTestHelper;

namespace PopravkaBa.Tests.Oglasi;

[TestClass]
[TestCategory("Service")]
public class OglasServiceTests
{
    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DajNedavne_TopN_VracaOglaseIzRepozitorija()
    {
        // Arrange
        var repo = new Mock<IOglasRepository>();
        var oglasi = new Oglas[] { OglasUsluge(1), OglasRadnoMjesto(2) };
        repo.Setup(r => r.DajNedavneAsync(4)).ReturnsAsync(oglasi);
        var servis = new OglasService(repo.Object);

        // Act
        var rezultat = await servis.DajNedavne(4);

        // Assert
        Assert.AreSame(oglasi, rezultat);
    }
}
