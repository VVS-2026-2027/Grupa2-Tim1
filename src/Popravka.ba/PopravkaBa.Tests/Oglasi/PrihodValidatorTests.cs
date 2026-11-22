using System.ComponentModel.DataAnnotations;
using static PopravkaBa.Tests.Oglasi.OglasiTestHelper;

namespace PopravkaBa.Tests.Oglasi;

// PrihodValidator je internal, pa se testira preko oba DTO-a koji ga koriste (Validate iz IValidatableObject)
[TestClass]
[TestCategory("Domain")]
public class PrihodValidatorTests
{
    public static IEnumerable<object[]> Rasponi =>
        DataDrivenInputParser.CitajCsv<int?, int?, bool>("TestData/csv/PrihodValidator_rasponi.csv");

    [TestMethod]
    [TestCategory("DataDriven")]
    [Owner("Aid Mustafić")]
    [DynamicData(nameof(Rasponi))]
    public void Validate_ObjavaRasponIzCsv_GreskaSamoKadJeMaxManjiOdMin(int? minPrihod, int? maxPrihod, bool imaGresku)
    {
        // Arrange
        var dto = ObjaviRadnoMjestoDto(minPrihod, maxPrihod);

        // Act
        var greske = dto.Validate(new ValidationContext(dto)).ToList();

        // Assert
        Assert.AreEqual(imaGresku, greske.Count > 0);
    }

    [TestMethod]
    [TestCategory("DataDriven")]
    [Owner("Aid Mustafić")]
    [DynamicData(nameof(Rasponi))]
    public void Validate_UredjivanjeRasponIzCsv_GreskaSamoKadJeMaxManjiOdMin(int? minPrihod, int? maxPrihod, bool imaGresku)
    {
        // Arrange
        var dto = UrediRadnoMjestoDto(1, minPrihod, maxPrihod);

        // Act
        var greske = dto.Validate(new ValidationContext(dto)).ToList();

        // Assert
        Assert.AreEqual(imaGresku, greske.Count > 0);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public void Validate_MaxManjiOdMin_GreskaJeNaPoljuMaxPrihod()
    {
        // Arrange
        var dto = ObjaviRadnoMjestoDto(minPrihod: 2000, maxPrihod: 1000);

        // Act
        var greska = dto.Validate(new ValidationContext(dto)).Single();

        // Assert
        CollectionAssert.Contains(greska.MemberNames.ToList(), "MaxPrihod");
    }
}
