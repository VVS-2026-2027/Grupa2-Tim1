using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PopravkaBa.Domain.Models;
using PopravkaBa.Domain.Specifications.Interface;
using PopravkaBa.Domain.Specifications.Subtype;
using System;
using System.Linq.Expressions;

namespace PopravkaBa.Tests.Pretraga.Specifikacije
{
    [TestClass]
    [TestCategory("Domain")]
    public class SortiranjeSpecificationTests
    {
        private Mock<ISpecification<OglasUsluge>> _unutrasnja = null!;
        private Expression<Func<OglasUsluge, object>> _sortiranje = null!;

        [TestInitialize]
        public void Inicijaliziraj()
        {
            _unutrasnja = new Mock<ISpecification<OglasUsluge>>();
            _sortiranje = o => o.MinBudzet;
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void ToExpression_OmotanaSpecifikacija_VracaIzrazUnutrasnjeSpecifikacije()
        {
            // Arrange
            Expression<Func<OglasUsluge, bool>> izraz = o => o.MinBudzet > 0;
            _unutrasnja.Setup(s => s.ToExpression()).Returns(izraz);
            var specifikacija = new SortiranjeSpecification<OglasUsluge>(_unutrasnja.Object, _sortiranje);

            // Act
            var rezultat = specifikacija.ToExpression();

            // Assert
            Assert.AreSame(izraz, rezultat);
            _unutrasnja.Verify(s => s.ToExpression(), Times.Once());
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void OrderBy_ZadanoAscending_VracaIzrazSortiranja()
        {
            // Arrange
            var specifikacija = new SortiranjeSpecification<OglasUsluge>(_unutrasnja.Object, _sortiranje);

            // Act
            var rezultat = specifikacija.OrderBy;

            // Assert
            Assert.AreSame(_sortiranje, rezultat);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void OrderByDescending_ZadanoAscending_VracaNull()
        {
            // Arrange
            var specifikacija = new SortiranjeSpecification<OglasUsluge>(_unutrasnja.Object, _sortiranje);

            // Act
            var rezultat = specifikacija.OrderByDescending;

            // Assert
            Assert.IsNull(rezultat);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void OrderBy_Descending_VracaNull()
        {
            // Arrange
            var specifikacija = new SortiranjeSpecification<OglasUsluge>(_unutrasnja.Object, _sortiranje, true);

            // Act
            var rezultat = specifikacija.OrderBy;

            // Assert
            Assert.IsNull(rezultat);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void OrderByDescending_Descending_VracaIzrazSortiranja()
        {
            // Arrange
            var specifikacija = new SortiranjeSpecification<OglasUsluge>(_unutrasnja.Object, _sortiranje, true);

            // Act
            var rezultat = specifikacija.OrderByDescending;

            // Assert
            Assert.AreSame(_sortiranje, rezultat);
        }
    }
}
