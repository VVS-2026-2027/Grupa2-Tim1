using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PopravkaBa.Web.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PopravkaBa.Tests.Zajednicko;

// Pomoćne metode koje samo pripremaju podatke za testove
internal static class BosanskiValidationMetadataProviderTestHelper
{
    internal static ValidationMetadataProviderContext NapraviKontekstZaSvojstvo(object validator)
    {
        var identitet = ModelMetadataIdentity.ForProperty(typeof(List<int>), "KategorijeID", typeof(object));
        var atributi = ModelAttributes.GetAttributesForType(typeof(List<int>));
        var kontekst = new ValidationMetadataProviderContext(identitet, atributi);
        kontekst.ValidationMetadata.ValidatorMetadata.Add(validator);
        return kontekst;
    }
}
