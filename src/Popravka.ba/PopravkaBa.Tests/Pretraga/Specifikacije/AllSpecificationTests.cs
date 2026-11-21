using Microsoft.VisualStudio.TestTools.UnitTesting;
using PopravkaBa.Domain.Models;
using PopravkaBa.Domain.Specifications.Subtype;

namespace PopravkaBa.Tests.Pretraga.Specifikacije
{
    [TestClass]
    [TestCategory("Domain")]
    public class AllSpecificationTests
    {
        [TestMethod]
        [Owner("Sajra Malkić")]
        public void ToExpression_BiloKojiOglas_VracaTrue()
        {
            // Arrange
            var specifikacija = new AllSpecification<OglasUsluge>();
            var oglas = new OglasUsluge();

            // Act
            var rezultat = specifikacija.ToExpression().Compile()(oglas);

            // Assert
            Assert.IsTrue(rezultat);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void OrderBy_ZadanoPonasanje_VracaNull()
        {
            // Arrange
            var specifikacija = new AllSpecification<OglasUsluge>();

            // Act
            var rezultat = specifikacija.OrderBy;

            // Assert
            Assert.IsNull(rezultat);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void OrderByDescending_ZadanoPonasanje_VracaNull()
        {
            // Arrange
            var specifikacija = new AllSpecification<OglasUsluge>();

            // Act
            var rezultat = specifikacija.OrderByDescending;

            // Assert
            Assert.IsNull(rezultat);
        }
    }
}
