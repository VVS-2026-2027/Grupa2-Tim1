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

        [TestMethod]
        public void Procijeni_DuzinaICifra_VracaSrednja()
        {
            var rezultat = JacinaLozinke.Procijeni("abcdefg1", Email);

            Assert.AreEqual(Jacina.Srednja, rezultat);
        }

        [TestMethod]
        public void Procijeni_DuzinaIVelikoSlovo_VracaSrednja()
        {
            var rezultat = JacinaLozinke.Procijeni("Abcdefgh", Email);

            Assert.AreEqual(Jacina.Srednja, rezultat);
        }

        [TestMethod]
        public void Procijeni_DuzinaISpecijalniZnak_VracaSrednja()
        {
            var rezultat = JacinaLozinke.Procijeni("abcdefg!", Email);

            Assert.AreEqual(Jacina.Srednja, rezultat);
        }

        [TestMethod]
        public void Procijeni_SviKriterijiIspunjeni_VracaJaka()
        {
            var rezultat = JacinaLozinke.Procijeni("Abcdefg1!", Email);

            Assert.AreEqual(Jacina.Jaka, rezultat);
        }

        [TestMethod]
        public void Procijeni_LozinkaSadrziDioEmaila_VracaSlaba()
        {
            var rezultat = JacinaLozinke.Procijeni("Amina123!x", "amina@gmail.com");

            Assert.AreEqual(Jacina.Slaba, rezultat);
        }

        [TestMethod]
        public void Procijeni_KratakDioEmaila_Ignorira()
        {
            var rezultat = JacinaLozinke.Procijeni("Abcdefg1!", "a@gmail.com");

            Assert.AreEqual(Jacina.Jaka, rezultat);
        }

        [TestMethod]
        public void Procijeni_EmailNull_Ignorira()
        {
            var rezultat = JacinaLozinke.Procijeni("Abcdefg1!", null);

            Assert.AreEqual(Jacina.Jaka, rezultat);
        }

        [TestMethod]
        public void Procijeni_LozinkaNull_BacaArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => JacinaLozinke.Procijeni(null!, Email));
        }

        [TestMethod]
        public void Procijeni_PrazanString_BacaArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(() => JacinaLozinke.Procijeni("", Email));
        }

        [TestMethod]
        public void Procijeni_LozinkaSaRazmacima_RazmaciSeIgnorisu()
        {
            // "abc de f1" bez razmaka je "abcdef1" (7 znakova), samo 1 bod
            var rezultat = JacinaLozinke.Procijeni("abc de f1", Email);

            Assert.AreEqual(Jacina.Slaba, rezultat);
        }

        [TestMethod]
        public void Procijeni_SamoRazmaci_BacaArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(() => JacinaLozinke.Procijeni("   ", Email));
        }

        [TestMethod]
        public void Procijeni_CestaLozinkaPassword_VracaSlaba()
        {
            var rezultat = JacinaLozinke.Procijeni("Password", Email);

            Assert.AreEqual(Jacina.Slaba, rezultat);
        }

        [TestMethod]
        public void Procijeni_CestaLozinka12345678_VracaSlaba()
        {
            var rezultat = JacinaLozinke.Procijeni("12345678", Email);

            Assert.AreEqual(Jacina.Slaba, rezultat);
        }
    }
}