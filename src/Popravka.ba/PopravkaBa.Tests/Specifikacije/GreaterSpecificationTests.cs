using Microsoft.VisualStudio.TestTools.UnitTesting;
using PopravkaBa.Domain.Models;
using PopravkaBa.Tests.Helpers;
using PopravkaBa.Domain.Specifications.Subtype;
using System.Collections.Generic;

namespace PopravkaBa.Tests.Specifikacije
{
    [TestClass]
    public class GreaterThanSpecificationTests
    {
        // Data-driven test: podaci se citaju iz TestData/csv/GreaterThanSpecification_poredjenjeVrijednosti.csv
        public static IEnumerable<object[]> SlucajeviIzCsv =>
            DataDrivenInputParser.CitajCsv<int, int, bool>("TestData/csv/GreaterThanSpecification_poredjenjeVrijednosti.csv");

        [TestMethod]
        [DynamicData(nameof(SlucajeviIzCsv))]
        public void ToExpression_PodaciIzCsv_VracaOcekivano(int vrijednost, int prag, bool ocekivano)
        {
            // Arrange
            var specifikacija = new GreaterThanSpecification<OglasUsluge, int>(o => o.MinBudzet, prag);
            var oglas = new OglasUsluge { MinBudzet = vrijednost };

            // Act
            var rezultat = specifikacija.ToExpression().Compile()(oglas);

            // Assert
            Assert.AreEqual(ocekivano, rezultat);
        }
    }

    [TestClass]
    public class GreaterThanOrEqualSpecificationTests
    {
        [TestMethod]
        [DataRow(101, 100, true)]
        [DataRow(100, 100, true)]
        [DataRow(99, 100, false)]
        public void ToExpression_RazlicitaVrijednost_VracaOcekivano(int vrijednost, int prag, bool ocekivano)
        {
            // Arrange
            var specifikacija = new GreaterThanOrEqualSpecification<OglasUsluge, int>(o => o.MinBudzet, prag);
            var oglas = new OglasUsluge { MinBudzet = vrijednost };

            // Act
            var rezultat = specifikacija.ToExpression().Compile()(oglas);

            // Assert
            Assert.AreEqual(ocekivano, rezultat);
        }
    }
}
