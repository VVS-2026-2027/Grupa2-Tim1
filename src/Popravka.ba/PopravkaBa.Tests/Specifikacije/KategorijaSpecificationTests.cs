using Microsoft.VisualStudio.TestTools.UnitTesting;
using PopravkaBa.Domain.Models;
using PopravkaBa.Domain.Specifications.Subtype;
using System;
using System.Collections.Generic;

namespace PopravkaBa.Tests.Specifikacije
{
    [TestClass]
    public class KategorijaOglasSpecificationTests
    {
        [TestMethod]
        public void ToExpression_OglasImaTrazenuKategoriju_VracaTrue()
        {
            // Arrange
            var specifikacija = new KategorijaOglasSpecification<OglasUsluge>(new List<int> { 1, 3 });
            var oglas = new OglasUsluge
            {
                Kategorije = new List<OglasKategorija> { new OglasKategorija { KategorijaID = 3 } }
            };

            // Act
            var rezultat = specifikacija.ToExpression().Compile()(oglas);

            // Assert
            Assert.IsTrue(rezultat);
        }

        [TestMethod]
        public void ToExpression_OglasNemaTrazenuKategoriju_VracaFalse()
        {
            // Arrange
            var specifikacija = new KategorijaOglasSpecification<OglasUsluge>(new List<int> { 1, 3 });
            var oglas = new OglasUsluge
            {
                Kategorije = new List<OglasKategorija> { new OglasKategorija { KategorijaID = 5 } }
            };

            // Act
            var rezultat = specifikacija.ToExpression().Compile()(oglas);

            // Assert
            Assert.IsFalse(rezultat);
        }

        [TestMethod]
        public void ToExpression_OglasBezKategorija_VracaFalse()
        {
            // Arrange
            var specifikacija = new KategorijaOglasSpecification<OglasUsluge>(new List<int> { 1 });
            var oglas = new OglasUsluge { Kategorije = new List<OglasKategorija>() };

            // Act
            var rezultat = specifikacija.ToExpression().Compile()(oglas);

            // Assert
            Assert.IsFalse(rezultat);
        }

        [TestMethod]
        public void ToExpression_PrazanSpisakTrazenihKategorija_VracaFalse()
        {
            // Arrange
            var specifikacija = new KategorijaOglasSpecification<OglasUsluge>(new List<int>());
            var oglas = new OglasUsluge
            {
                Kategorije = new List<OglasKategorija> { new OglasKategorija { KategorijaID = 1 } }
            };

            // Act
            var rezultat = specifikacija.ToExpression().Compile()(oglas);

            // Assert
            Assert.IsFalse(rezultat);
        }

        [TestMethod]
        public void ToExpression_KategorijeNisuPostavljene_BacaArgumentNullException()
        {
            // Arrange
            var specifikacija = new KategorijaOglasSpecification<OglasUsluge>(new List<int> { 1 });
            var oglas = new OglasUsluge { Kategorije = null };
            var izraz = specifikacija.ToExpression().Compile();

            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => izraz(oglas));
        }
    }

    [TestClass]
    public class KategorijaIzvrsilacSpecificationTests
    {
        [TestMethod]
        public void ToExpression_IzvrsilacImaTrazenuKategoriju_VracaTrue()
        {
            // Arrange
            var specifikacija = new KategorijaIzvrsilacSpecification<Majstor>(new List<int> { 2, 4 });
            var majstor = new Majstor
            {
                Kategorije = new List<IzvrsilacKategorija> { new IzvrsilacKategorija { KategorijaID = 4 } }
            };

            // Act
            var rezultat = specifikacija.ToExpression().Compile()(majstor);

            // Assert
            Assert.IsTrue(rezultat);
        }

        [TestMethod]
        public void ToExpression_IzvrsilacNemaTrazenuKategoriju_VracaFalse()
        {
            // Arrange
            var specifikacija = new KategorijaIzvrsilacSpecification<Majstor>(new List<int> { 2, 4 });
            var majstor = new Majstor
            {
                Kategorije = new List<IzvrsilacKategorija> { new IzvrsilacKategorija { KategorijaID = 7 } }
            };

            // Act
            var rezultat = specifikacija.ToExpression().Compile()(majstor);

            // Assert
            Assert.IsFalse(rezultat);
        }

        [TestMethod]
        public void ToExpression_KategorijeNisuPostavljene_BacaArgumentNullException()
        {
            // Arrange
            var specifikacija = new KategorijaIzvrsilacSpecification<Majstor>(new List<int> { 2 });
            var majstor = new Majstor { Kategorije = null };
            var izraz = specifikacija.ToExpression().Compile();

            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => izraz(majstor));
        }
    }
}
