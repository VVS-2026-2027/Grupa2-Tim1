using Microsoft.AspNetCore.Identity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PopravkaBa.Domain.Models;
using PopravkaBa.Tests.Helpers;
using System.Linq;
using System.Threading.Tasks;

namespace PopravkaBa.Tests.Zajednicko
{
    [TestClass]
    public class CaseInsensitivePasswordValidatorTests
    {
        private UserManager<ApplicationUser> _upraviteljKorisnika = null!;
        private CaseInsensitivePasswordValidator<ApplicationUser> _validator = null!;

        [TestInitialize]
        public void Inicijaliziraj()
        {
            _upraviteljKorisnika = TestHelpers.MockUserManager().Object;
            _validator = new CaseInsensitivePasswordValidator<ApplicationUser>();
        }

        [TestMethod]
        [DataRow("abc123")]
        [DataRow("LOZINKA")]
        [DataRow("12345a")]
        [DataRow("!!!č!!!")]
        public async Task ValidateAsync_LozinkaSaSlovom_VracaUspjeh(string lozinka)
        {
            // Act
            var rezultat = await _validator.ValidateAsync(_upraviteljKorisnika, new ApplicationUser(), lozinka);

            // Assert
            Assert.IsTrue(rezultat.Succeeded);
        }

        [TestMethod]
        [DataRow("12345678")]
        [DataRow("!#$%&")]
        [DataRow("")]
        public async Task ValidateAsync_LozinkaBezSlova_VracaGreskuPasswordRequiresLetter(string lozinka)
        {
            // Act
            var rezultat = await _validator.ValidateAsync(_upraviteljKorisnika, new ApplicationUser(), lozinka);

            // Assert
            Assert.IsFalse(rezultat.Succeeded);
            Assert.AreEqual("PasswordRequiresLetter", rezultat.Errors.First().Code);
        }

        [TestMethod]
        public async Task ValidateAsync_NullLozinka_VracaGresku()
        {
            // Act
            var rezultat = await _validator.ValidateAsync(_upraviteljKorisnika, new ApplicationUser(), null);

            // Assert
            Assert.IsFalse(rezultat.Succeeded);
        }

        [TestMethod]
        public async Task ValidateAsync_LozinkaBezSlova_VracaBosanskiOpisGreske()
        {
            // Act
            var rezultat = await _validator.ValidateAsync(_upraviteljKorisnika, new ApplicationUser(), "123");

            // Assert
            Assert.AreEqual("Lozinka mora sadržavati barem jedno slovo.", rezultat.Errors.First().Description);
        }
    }
}
