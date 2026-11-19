using Microsoft.VisualStudio.TestTools.UnitTesting;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Shared;
using PopravkaBa.Tests.Helpers;

namespace PopravkaBa.Tests.Zajednicko
{
    [TestClass]
    public class JacinaLozinkeTests
    {
        private const string Email = "korisnik@popravka.ba";

        [TestMethod]
        public void Procijeni_KratkaLozinka_VracaSlaba()
        {
            // Arrange
            var lozinka = "abc";

            // Act
            var rezultat = JacinaLozinke.Procijeni(lozinka, Email);

            // Assert
            Assert.AreEqual(Jacina.Slaba, rezultat);
        }
    }
}