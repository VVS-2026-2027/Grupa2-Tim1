using Microsoft.VisualStudio.TestTools.UnitTesting;
using PopravkaBa.Domain.Shared;

namespace PopravkaBa.Tests.Zajednicko
{
    [TestClass]
    [TestCategory("Domain")]
    public class NameFormatterTests
    {
        [TestMethod]
        [Owner("Sajra Malkić")]
        [DataRow("MARKO MARKOVIĆ", "Marko Marković")]
        [DataRow("ana jović", "Ana Jović")]
        [DataRow("  marko  ", "Marko")]
        [DataRow("ĐURĐA", "Đurđa")]
        public void NormalizeName_RazlicitUnos_VracaImeSaVelikimPocetnimSlovima(string ulaz, string ocekivano)
        {
            // Act
            var rezultat = NameFormatter.NormalizeName(ulaz);

            // Assert
            Assert.AreEqual(ocekivano, rezultat);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void NormalizeName_Null_VracaNull()
        {
            // Act
            var rezultat = NameFormatter.NormalizeName(null!);

            // Assert
            Assert.IsNull(rezultat);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void NormalizeName_PrazanString_VracaPrazanString()
        {
            // Act
            var rezultat = NameFormatter.NormalizeName(string.Empty);

            // Assert
            Assert.AreEqual(string.Empty, rezultat);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void NormalizeName_SamoRazmaci_VracaIsteRazmake()
        {
            // Act
            var rezultat = NameFormatter.NormalizeName("   ");

            // Assert
            Assert.AreEqual("   ", rezultat);
        }
    }
}
