using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Models;
using PopravkaBa.Domain.Specifications.Interface;
using PopravkaBa.Domain.Specifications.Subtype;
using System;
using System.Linq.Expressions;

namespace PopravkaBa.Tests.Specifikacije
{
    [TestClass]
    public class AndSpecificationTests
    {
        private Mock<ISpecification<OglasUsluge>> _lijevi = null!;
        private Mock<ISpecification<OglasUsluge>> _desni = null!;

        [TestInitialize]
        public void Inicijaliziraj()
        {
            Expression<Func<OglasUsluge, bool>> uvijekTacno = o => true;
            _lijevi = new Mock<ISpecification<OglasUsluge>>();
            _lijevi.Setup(s => s.ToExpression()).Returns(uvijekTacno);
            _desni = new Mock<ISpecification<OglasUsluge>>();
            _desni.Setup(s => s.ToExpression()).Returns(uvijekTacno);
        }

        [TestMethod]
        [DataRow(Status.Aktivan, 150, true)]
        [DataRow(Status.Neaktivan, 150, false)]
        [DataRow(Status.Aktivan, 50, false)]
        [DataRow(Status.Neaktivan, 50, false)]
        public void ToExpression_KombinacijaDvaUslova_VracaOcekivano(Status status, int minBudzet, bool ocekivano)
        {
            // Arrange
            var lijevi = new AktivanSpecification<OglasUsluge>();
            var desni = new GreaterThanSpecification<OglasUsluge, int>(o => o.MinBudzet, 100);
            var specifikacija = new AndSpecification<OglasUsluge>(lijevi, desni);
            var oglas = new OglasUsluge { StatusOglasa = status, MinBudzet = minBudzet };

            // Act
            var rezultat = specifikacija.ToExpression().Compile()(oglas);

            // Assert
            Assert.AreEqual(ocekivano, rezultat);
        }

        [TestMethod]
        public void ToExpression_MockiraneSpecifikacije_PozivaObjeSpecifikacije()
        {
            // Arrange
            var specifikacija = new AndSpecification<OglasUsluge>(_lijevi.Object, _desni.Object);

            // Act
            var rezultat = specifikacija.ToExpression().Compile()(new OglasUsluge());

            // Assert
            Assert.IsTrue(rezultat);
            _lijevi.Verify(s => s.ToExpression(), Times.Once());
            _desni.Verify(s => s.ToExpression(), Times.Once());
        }
    }
}
