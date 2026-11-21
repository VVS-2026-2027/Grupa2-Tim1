using Microsoft.VisualStudio.TestTools.UnitTesting;
using PopravkaBa.Domain.Models;
using PopravkaBa.Domain.Specifications.Subtype;

namespace PopravkaBa.Tests.Pretraga.Specifikacije
{
    [TestClass]
    [TestCategory("Domain")]
    public class LessThanSpecificationTests
    {
        [TestMethod]
        [Owner("Sajra Malkić")]
        [DataRow(99, 100, true)]
        [DataRow(100, 100, false)]
        [DataRow(101, 100, false)]
        public void ToExpression_RazlicitaVrijednost_VracaOcekivano(int vrijednost, int prag, bool ocekivano)
        {
            // Arrange
            var specifikacija = new LessThanSpecification<OglasUsluge, int>(o => o.MinBudzet, prag);
            var oglas = new OglasUsluge { MinBudzet = vrijednost };

            // Act
            var rezultat = specifikacija.ToExpression().Compile()(oglas);

            // Assert
            Assert.AreEqual(ocekivano, rezultat);
        }
    }

    [TestClass]
    [TestCategory("Domain")]
    public class LessThanOrEqualSpecificationTests
    {
        [TestMethod]
        [Owner("Sajra Malkić")]
        [DataRow(99, 100, true)]
        [DataRow(100, 100, true)]
        [DataRow(101, 100, false)]
        public void ToExpression_RazlicitaVrijednost_VracaOcekivano(int vrijednost, int prag, bool ocekivano)
        {
            // Arrange
            var specifikacija = new LessThanOrEqualSpecification<OglasUsluge, int>(o => o.MinBudzet, prag);
            var oglas = new OglasUsluge { MinBudzet = vrijednost };

            // Act
            var rezultat = specifikacija.ToExpression().Compile()(oglas);

            // Assert
            Assert.AreEqual(ocekivano, rezultat);
        }
    }
}
