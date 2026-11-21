using Microsoft.VisualStudio.TestTools.UnitTesting;
using PopravkaBa.Domain.Models;
using PopravkaBa.Domain.Specifications.Subtype;
// Klasa u projektu ima cirilicno slovo "e" u imenu (Budz\u0435tSpecification),
// pa se ovdje koristi alias da test ne zavisi od tog slova.
using BudzetSpecification = PopravkaBa.Domain.Specifications.Subtype.Budz\u0435tSpecification;

namespace PopravkaBa.Tests.Pretraga.Specifikacije
{
    [TestClass]
    public class BudzetSpecificationTests
    {
        [TestMethod]
        [DataRow(100, 200, 150, 300, true)]
        [DataRow(100, 200, 200, 300, true)]
        [DataRow(100, 200, 0, 100, true)]
        [DataRow(100, 200, 201, 300, false)]
        [DataRow(100, 200, 0, 99, false)]
        [DataRow(100, 200, 300, 150, false)]
        public void ToExpression_RazlicitiRasponi_VracaOcekivano(
            int minBudzet, int maxBudzet, int filterOd, int filterDo, bool ocekivano)
        {
            // Arrange
            var specifikacija = new BudzetSpecification(filterOd, filterDo);
            var oglas = new OglasUsluge { MinBudzet = minBudzet, MaxBudzet = maxBudzet };

            // Act
            var rezultat = specifikacija.ToExpression().Compile()(oglas);

            // Assert
            Assert.AreEqual(ocekivano, rezultat);
        }
    }

    [TestClass]
    public class PlataSpecificationTests
    {
        [TestMethod]
        [DataRow(1000, 2000, 1500, 3000, true)]
        [DataRow(1000, 2000, 2000, 3000, true)]
        [DataRow(1000, 2000, 0, 1000, true)]
        [DataRow(1000, 2000, 2001, 3000, false)]
        [DataRow(1000, 2000, 0, 999, false)]
        public void ToExpression_ValidanRaspon_VracaOcekivano(
            int minPrihod, int maxPrihod, int filterOd, int filterDo, bool ocekivano)
        {
            // Arrange
            var specifikacija = new PlataSpecification(filterOd, filterDo);
            var oglas = new OglasRadnoMjesto { MinPrihod = minPrihod, MaxPrihod = maxPrihod };

            // Act
            var rezultat = specifikacija.ToExpression().Compile()(oglas);

            // Assert
            Assert.AreEqual(ocekivano, rezultat);
        }

        [TestMethod]
        [DataRow(-1, -1, true)]
        [DataRow(500, 800, false)]
        [DataRow(-1, 5, false)]
        public void ToExpression_ObrnutRaspon_VracaTrueSamoZaNegativnePrihode(
            int minPrihod, int maxPrihod, bool ocekivano)
        {
            // Arrange
            var specifikacija = new PlataSpecification(3000, 1500);
            var oglas = new OglasRadnoMjesto { MinPrihod = minPrihod, MaxPrihod = maxPrihod };

            // Act
            var rezultat = specifikacija.ToExpression().Compile()(oglas);

            // Assert
            Assert.AreEqual(ocekivano, rezultat);
        }
    }
}
