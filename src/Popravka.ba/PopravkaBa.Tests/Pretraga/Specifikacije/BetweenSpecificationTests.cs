using Microsoft.VisualStudio.TestTools.UnitTesting;
using PopravkaBa.Domain.Models;
using PopravkaBa.Domain.Specifications.Subtype;

namespace PopravkaBa.Tests.Pretraga.Specifikacije
{
    [TestClass]
    [TestCategory("Domain")]
    public class BetweenSpecificationTests
    {
        [TestMethod]
        [Owner("Sajra Malkić")]
        [DataRow(15, 10, 20, true)]
        [DataRow(10, 10, 20, true)]
        [DataRow(20, 10, 20, true)]
        [DataRow(9, 10, 20, false)]
        [DataRow(21, 10, 20, false)]
        [DataRow(7, 10, 5, false)]
        public void ToExpression_RazlicitaVrijednost_VracaOcekivano(int vrijednost, int donja, int gornja, bool ocekivano)
        {
            // Arrange
            var specifikacija = new BetweenSpecification<OglasUsluge, int>(o => o.MinBudzet, donja, gornja);
            var oglas = new OglasUsluge { MinBudzet = vrijednost };

            // Act
            var rezultat = specifikacija.ToExpression().Compile()(oglas);

            // Assert
            Assert.AreEqual(ocekivano, rezultat);
        }
    }
}
