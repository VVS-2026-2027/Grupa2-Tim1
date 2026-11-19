using Microsoft.VisualStudio.TestTools.UnitTesting;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Models;
using PopravkaBa.Domain.Specifications.Subtype;

namespace PopravkaBa.Tests.Specifikacije
{
    [TestClass]
    public class VrstaZaposlenjaSpecificationTests
    {
        [TestMethod]
        [DataRow(VrstaZaposlenja.PunoRadnoVrijeme, VrstaZaposlenja.PunoRadnoVrijeme, true)]
        [DataRow(VrstaZaposlenja.Praksa, VrstaZaposlenja.Praksa, true)]
        [DataRow(VrstaZaposlenja.PunoRadnoVrijeme, VrstaZaposlenja.Sezonski, false)]
        [DataRow(VrstaZaposlenja.UgovorDjelo, VrstaZaposlenja.PolaRadnogVrijema, false)]
        public void ToExpression_RazlicitaVrstaZaposlenja_VracaOcekivano(
            VrstaZaposlenja vrstaOglasa, VrstaZaposlenja trazenaVrsta, bool ocekivano)
        {
            // Arrange
            var specifikacija = new VrstaZaposlenjaSpecification<OglasRadnoMjesto>(trazenaVrsta);
            var oglas = new OglasRadnoMjesto { VrstaZaposlenja = vrstaOglasa };

            // Act
            var rezultat = specifikacija.ToExpression().Compile()(oglas);

            // Assert
            Assert.AreEqual(ocekivano, rezultat);
        }
    }
}
