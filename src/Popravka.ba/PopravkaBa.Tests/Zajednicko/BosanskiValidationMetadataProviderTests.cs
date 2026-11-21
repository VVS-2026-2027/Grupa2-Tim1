using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PopravkaBa.Web.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using static PopravkaBa.Tests.Zajednicko.BosanskiValidationMetadataProviderTestHelper;

namespace PopravkaBa.Tests.Zajednicko
{
    [TestClass]
    public class BosanskiValidationMetadataProviderTests
    {
        private BosanskiValidationMetadataProvider _provider = null!;

        [TestInitialize]
        public void Inicijaliziraj()
        {
            _provider = new BosanskiValidationMetadataProvider();
        }

        [TestMethod]
        public void CreateValidationMetadata_RequiredBezPoruke_PostavljaBosanskuPoruku()
        {
            // Arrange
            var required = new RequiredAttribute();
            var kontekst = NapraviKontekstZaSvojstvo(required);

            // Act
            _provider.CreateValidationMetadata(kontekst);

            // Assert
            Assert.AreEqual("Polje KategorijeID je obavezno.", required.ErrorMessage);
        }

        [TestMethod]
        public void CreateValidationMetadata_RequiredSaPorukom_NePrepisujePoruku()
        {
            // Arrange
            var required = new RequiredAttribute { ErrorMessage = "Moja poruka" };
            var kontekst = NapraviKontekstZaSvojstvo(required);

            // Act
            _provider.CreateValidationMetadata(kontekst);

            // Assert
            Assert.AreEqual("Moja poruka", required.ErrorMessage);
        }

        [TestMethod]
        public void CreateValidationMetadata_RequiredSaResursom_NePostavljaPoruku()
        {
            // Arrange
            var required = new RequiredAttribute { ErrorMessageResourceName = "NekiResurs" };
            var kontekst = NapraviKontekstZaSvojstvo(required);

            // Act
            _provider.CreateValidationMetadata(kontekst);

            // Assert
            Assert.IsNull(required.ErrorMessage);
        }

        [TestMethod]
        public void CreateValidationMetadata_DrugiAtribut_NeMijenjaAtribut()
        {
            // Arrange
            var duzina = new StringLengthAttribute(10);
            var kontekst = NapraviKontekstZaSvojstvo(duzina);

            // Act
            _provider.CreateValidationMetadata(kontekst);

            // Assert
            Assert.IsNull(duzina.ErrorMessage);
        }

        [TestMethod]
        public void CreateValidationMetadata_KljucBezImena_KoristiPodrazumijevaniNaziv()
        {
            // Arrange
            var required = new RequiredAttribute();
            var identitet = ModelMetadataIdentity.ForType(typeof(List<int>));
            var atributi = ModelAttributes.GetAttributesForType(typeof(List<int>));
            var kontekst = new ValidationMetadataProviderContext(identitet, atributi);
            kontekst.ValidationMetadata.ValidatorMetadata.Add(required);

            // Act
            _provider.CreateValidationMetadata(kontekst);

            // Assert
            Assert.AreEqual("Polje Polje je obavezno.", required.ErrorMessage);
        }

        [TestMethod]
        public void CreateValidationMetadata_BezValidatora_NeBacaIzuzetak()
        {
            // Arrange
            var identitet = ModelMetadataIdentity.ForType(typeof(List<int>));
            var atributi = ModelAttributes.GetAttributesForType(typeof(List<int>));
            var kontekst = new ValidationMetadataProviderContext(identitet, atributi);

            // Act
            _provider.CreateValidationMetadata(kontekst);

            // Assert
            Assert.AreEqual(0, kontekst.ValidationMetadata.ValidatorMetadata.Count);
        }
    }
}
