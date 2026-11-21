using Microsoft.VisualStudio.TestTools.UnitTesting;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Models;
using PopravkaBa.Domain.Specifications.Subtype;

namespace PopravkaBa.Tests.Pretraga.Specifikacije
{
    [TestClass]
    public class AktivanSpecificationTests
    {
        [TestMethod]
        [DataRow(Status.Aktivan, true)]
        [DataRow(Status.Neaktivan, false)]
        [DataRow(Status.NaCekanju, false)]
        public void ToExpression_RazlicitStatusOglasa_VracaOcekivano(Status status, bool ocekivano)
        {
            // Arrange
            var specifikacija = new AktivanSpecification<OglasUsluge>();
            var oglas = new OglasUsluge { StatusOglasa = status };

            // Act
            var rezultat = specifikacija.ToExpression().Compile()(oglas);

            // Assert
            Assert.AreEqual(ocekivano, rezultat);
        }
    }
}
