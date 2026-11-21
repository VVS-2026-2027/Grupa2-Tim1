using Microsoft.VisualStudio.TestTools.UnitTesting;
using PopravkaBa.Domain.Models;
using PopravkaBa.Domain.Specifications.Subtype;
using System;
using System.Collections.Generic;

namespace PopravkaBa.Tests.Pretraga.Specifikacije
{
    [TestClass]
    [TestCategory("Domain")]
    public class MjestoOglasSpecificationTests
    {
        [TestMethod]
        [Owner("Sajra Malkić")]
        [DataRow(3, true)]
        [DataRow(1, true)]
        [DataRow(2, false)]
        public void ToExpression_RazlicitoMjestoOglasa_VracaOcekivano(int mjestoId, bool ocekivano)
        {
            // Arrange
            var specifikacija = new MjestoOglasSpecification<OglasUsluge>(new List<int> { 1, 3, 5 });
            var oglas = new OglasUsluge { MjestoID = mjestoId };

            // Act
            var rezultat = specifikacija.ToExpression().Compile()(oglas);

            // Assert
            Assert.AreEqual(ocekivano, rezultat);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void ToExpression_PrazanSpisakMjesta_VracaFalse()
        {
            // Arrange
            var specifikacija = new MjestoOglasSpecification<OglasUsluge>(new List<int>());
            var oglas = new OglasUsluge { MjestoID = 1 };

            // Act
            var rezultat = specifikacija.ToExpression().Compile()(oglas);

            // Assert
            Assert.IsFalse(rezultat);
        }
    }

    [TestClass]
    [TestCategory("Domain")]
    public class MjestoIzvrsilacSpecificationTests
    {
        [TestMethod]
        [Owner("Sajra Malkić")]
        public void ToExpression_IzvrsilacImaTrazenoMjesto_VracaTrue()
        {
            // Arrange
            var specifikacija = new MjestoIzvrsilacSpecification<Majstor>(new List<int> { 1, 3 });
            var majstor = new Majstor
            {
                Mjesta = new List<KorisnikMjesto> { new KorisnikMjesto { MjestoID = 3 } }
            };

            // Act
            var rezultat = specifikacija.ToExpression().Compile()(majstor);

            // Assert
            Assert.IsTrue(rezultat);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void ToExpression_IzvrsilacNemaTrazenoMjesto_VracaFalse()
        {
            // Arrange
            var specifikacija = new MjestoIzvrsilacSpecification<Majstor>(new List<int> { 1, 3 });
            var majstor = new Majstor
            {
                Mjesta = new List<KorisnikMjesto> { new KorisnikMjesto { MjestoID = 9 } }
            };

            // Act
            var rezultat = specifikacija.ToExpression().Compile()(majstor);

            // Assert
            Assert.IsFalse(rezultat);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void ToExpression_MjestaNisuPostavljena_BacaArgumentNullException()
        {
            // Arrange
            var specifikacija = new MjestoIzvrsilacSpecification<Majstor>(new List<int> { 1 });
            var majstor = new Majstor { Mjesta = null };
            var izraz = specifikacija.ToExpression().Compile();

            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => izraz(majstor));
        }
    }
}
