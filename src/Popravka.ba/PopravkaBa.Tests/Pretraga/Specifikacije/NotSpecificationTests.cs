using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Models;
using PopravkaBa.Domain.Specifications.Interface;
using PopravkaBa.Domain.Specifications.Subtype;
using System;
using System.Linq.Expressions;

namespace PopravkaBa.Tests.Pretraga.Specifikacije
{
    [TestClass]
    [TestCategory("Domain")]
    public class NotSpecificationTests
    {
        private Mock<ISpecification<OglasUsluge>> _unutrasnja = null!;

        [TestInitialize]
        public void Inicijaliziraj()
        {
            Expression<Func<OglasUsluge, bool>> uvijekTacno = o => true;
            _unutrasnja = new Mock<ISpecification<OglasUsluge>>();
            _unutrasnja.Setup(s => s.ToExpression()).Returns(uvijekTacno);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        [DataRow(Status.Aktivan, false)]
        [DataRow(Status.Neaktivan, true)]
        public void ToExpression_NegacijaAktivnog_VracaOcekivano(Status status, bool ocekivano)
        {
            // Arrange
            var specifikacija = new NotSpecification<OglasUsluge>(new AktivanSpecification<OglasUsluge>());
            var oglas = new OglasUsluge { StatusOglasa = status };

            // Act
            var rezultat = specifikacija.ToExpression().Compile()(oglas);

            // Assert
            Assert.AreEqual(ocekivano, rezultat);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void ToExpression_MockiranaSpecifikacija_PozivaUnutrasnjuSpecifikaciju()
        {
            // Arrange
            var specifikacija = new NotSpecification<OglasUsluge>(_unutrasnja.Object);

            // Act
            var rezultat = specifikacija.ToExpression().Compile()(new OglasUsluge());

            // Assert
            Assert.IsFalse(rezultat);
            _unutrasnja.Verify(s => s.ToExpression(), Times.Once());
        }
    }
}
