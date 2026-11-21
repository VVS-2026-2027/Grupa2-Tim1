using Microsoft.VisualStudio.TestTools.UnitTesting;
using PopravkaBa.Domain.Models;
using PopravkaBa.Domain.Specifications.Subtype;
using System;

namespace PopravkaBa.Tests.Pretraga.Specifikacije
{
    [TestClass]
    [TestCategory("Domain")]
    public class KljucneRijeciNaslovOglasaSpecificationTests
    {
        [TestMethod]
        [Owner("Sajra Malkić")]
        [DataRow("Popravka krova", "krov", true)]
        [DataRow("POPRAVKA KROVA", "krov", true)]
        [DataRow("Popravka krova", "KROV", true)]
        [DataRow("Popravka krova", "voda", false)]
        public void ToExpression_RazlicitNaslov_VracaOcekivano(string naslov, string kljucnaRijec, bool ocekivano)
        {
            // Arrange
            var specifikacija = new KljucneRijeciNaslovOglasaSpecification<OglasUsluge>(kljucnaRijec);
            var oglas = new OglasUsluge { Naslov = naslov };

            // Act
            var rezultat = specifikacija.ToExpression().Compile()(oglas);

            // Assert
            Assert.AreEqual(ocekivano, rezultat);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void ToExpression_NaslovNijePostavljen_BacaNullReferenceException()
        {
            // Arrange
            var specifikacija = new KljucneRijeciNaslovOglasaSpecification<OglasUsluge>("krov");
            var oglas = new OglasUsluge { Naslov = null! };
            var izraz = specifikacija.ToExpression().Compile();

            // Act & Assert
            Assert.ThrowsExactly<NullReferenceException>(() => izraz(oglas));
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void Konstruktor_NullKljucnaRijec_BacaNullReferenceException()
        {
            // Arrange
            string kljucnaRijec = null!;

            // Act & Assert
            Assert.ThrowsExactly<NullReferenceException>(
                () => new KljucneRijeciNaslovOglasaSpecification<OglasUsluge>(kljucnaRijec));
        }
    }

    [TestClass]
    [TestCategory("Domain")]
    public class KljucneRijeciOpisOglasaSpecificationTests
    {
        [TestMethod]
        [Owner("Sajra Malkić")]
        [DataRow("Potrebna zamjena cijevi", "cijev", true)]
        [DataRow("POTREBNA ZAMJENA CIJEVI", "cijev", true)]
        [DataRow("Potrebna zamjena cijevi", "CIJEV", true)]
        [DataRow("Potrebna zamjena cijevi", "farba", false)]
        public void ToExpression_RazlicitOpis_VracaOcekivano(string opis, string kljucnaRijec, bool ocekivano)
        {
            // Arrange
            var specifikacija = new KljucneRijeciOpisOglasaSpecification<OglasUsluge>(kljucnaRijec);
            var oglas = new OglasUsluge { Opis = opis };

            // Act
            var rezultat = specifikacija.ToExpression().Compile()(oglas);

            // Assert
            Assert.AreEqual(ocekivano, rezultat);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void ToExpression_OpisNijePostavljen_BacaNullReferenceException()
        {
            // Arrange
            var specifikacija = new KljucneRijeciOpisOglasaSpecification<OglasUsluge>("cijev");
            var oglas = new OglasUsluge { Opis = null! };
            var izraz = specifikacija.ToExpression().Compile();

            // Act & Assert
            Assert.ThrowsExactly<NullReferenceException>(() => izraz(oglas));
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void Konstruktor_NullKljucnaRijec_BacaNullReferenceException()
        {
            // Arrange
            string kljucnaRijec = null!;

            // Act & Assert
            Assert.ThrowsExactly<NullReferenceException>(
                () => new KljucneRijeciOpisOglasaSpecification<OglasUsluge>(kljucnaRijec));
        }
    }

    [TestClass]
    [TestCategory("Domain")]
    public class KljucneRijeciOpisIzvrsiocaSpecificationTests
    {
        [TestMethod]
        [Owner("Sajra Malkić")]
        [DataRow("Iskusan vodoinstalater", "vodo", true)]
        [DataRow("ISKUSAN VODOINSTALATER", "vodo", true)]
        [DataRow("Iskusan vodoinstalater", "VODO", true)]
        [DataRow("Iskusan vodoinstalater", "elektro", false)]
        public void ToExpression_RazlicitOpis_VracaOcekivano(string opis, string kljucnaRijec, bool ocekivano)
        {
            // Arrange
            var specifikacija = new KljucneRijeciOpisIzvrsiocaSpecification<Majstor>(kljucnaRijec);
            var majstor = new Majstor { Opis = opis };

            // Act
            var rezultat = specifikacija.ToExpression().Compile()(majstor);

            // Assert
            Assert.AreEqual(ocekivano, rezultat);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void ToExpression_OpisNijePostavljen_VracaFalse()
        {
            // Arrange
            var specifikacija = new KljucneRijeciOpisIzvrsiocaSpecification<Majstor>("vodo");
            var majstor = new Majstor { Opis = null };

            // Act
            var rezultat = specifikacija.ToExpression().Compile()(majstor);

            // Assert
            Assert.IsFalse(rezultat);
        }
    }

    [TestClass]
    [TestCategory("Domain")]
    public class KljucneRijeciDisplayNameIzvrsiocaSpecificationTests
    {
        [TestMethod]
        [Owner("Sajra Malkić")]
        [DataRow("mark", true)]
        [DataRow("MARK", true)]
        [DataRow("petr", true)]
        [DataRow("marko petrovic", true)]
        [DataRow("xyz", false)]
        public void ToExpression_MajstorSaImenom_VracaOcekivano(string kljucnaRijec, bool ocekivano)
        {
            // Arrange
            var specifikacija = new KljucneRijeciDisplayNameIzvrsiocaSpecification<Majstor>(kljucnaRijec);
            var majstor = new Majstor { Ime = "Marko", Prezime = "Petrovic" };

            // Act
            var rezultat = specifikacija.ToExpression().Compile()(majstor);

            // Assert
            Assert.AreEqual(ocekivano, rezultat);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void ToExpression_MajstorBezImenaIPrezimena_VracaFalse()
        {
            // Arrange
            var specifikacija = new KljucneRijeciDisplayNameIzvrsiocaSpecification<Majstor>("marko");
            var majstor = new Majstor();

            // Act
            var rezultat = specifikacija.ToExpression().Compile()(majstor);

            // Assert
            Assert.IsFalse(rezultat);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        [DataRow("gradnja", true)]
        [DataRow("GRADNJA", true)]
        [DataRow("alfa", true)]
        [DataRow("beta", false)]
        public void ToExpression_FirmaSaNazivom_VracaOcekivano(string kljucnaRijec, bool ocekivano)
        {
            // Arrange
            var specifikacija = new KljucneRijeciDisplayNameIzvrsiocaSpecification<Firma>(kljucnaRijec);
            var firma = new Firma { NazivFirme = "Alfa Gradnja" };

            // Act
            var rezultat = specifikacija.ToExpression().Compile()(firma);

            // Assert
            Assert.AreEqual(ocekivano, rezultat);
        }
    }
}
