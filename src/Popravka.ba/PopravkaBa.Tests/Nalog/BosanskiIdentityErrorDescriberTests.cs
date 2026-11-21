using Microsoft.AspNetCore.Identity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PopravkaBa.Domain.Shared;

namespace PopravkaBa.Tests.Nalog
{
    [TestClass]
    [TestCategory("Domain")]
    public class BosanskiIdentityErrorDescriberTests
    {
        private BosanskiIdentityErrorDescriber _opisivac = null!;

        [TestInitialize]
        public void Inicijaliziraj()
        {
            _opisivac = new BosanskiIdentityErrorDescriber();
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void DefaultError_Poziv_VracaBosanskuPoruku()
        {
            // Act
            var greska = _opisivac.DefaultError();

            // Assert
            Assert.AreEqual("DefaultError", greska.Code);
            Assert.AreEqual("Došlo je do nepoznate greške.", greska.Description);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void ConcurrencyFailure_Poziv_VracaBosanskuPoruku()
        {
            // Act
            var greska = _opisivac.ConcurrencyFailure();

            // Assert
            Assert.AreEqual("ConcurrencyFailure", greska.Code);
            Assert.AreEqual("Optimistička greška u istovremenosti, objekat je u međuvremenu izmijenjen.", greska.Description);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void PasswordMismatch_Poziv_VracaBosanskuPoruku()
        {
            // Act
            var greska = _opisivac.PasswordMismatch();

            // Assert
            Assert.AreEqual("PasswordMismatch", greska.Code);
            Assert.AreEqual("Pogrešna lozinka.", greska.Description);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void InvalidToken_Poziv_VracaBosanskuPoruku()
        {
            // Act
            var greska = _opisivac.InvalidToken();

            // Assert
            Assert.AreEqual("InvalidToken", greska.Code);
            Assert.AreEqual("Token nije validan.", greska.Description);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void RecoveryCodeRedemptionFailed_Poziv_VracaBosanskuPoruku()
        {
            // Act
            var greska = _opisivac.RecoveryCodeRedemptionFailed();

            // Assert
            Assert.AreEqual("RecoveryCodeRedemptionFailed", greska.Code);
            Assert.AreEqual("Iskorištavanje koda za oporavak nije uspjelo.", greska.Description);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void LoginAlreadyAssociated_Poziv_VracaBosanskuPoruku()
        {
            // Act
            var greska = _opisivac.LoginAlreadyAssociated();

            // Assert
            Assert.AreEqual("LoginAlreadyAssociated", greska.Code);
            Assert.AreEqual("Korisnik s ovom prijavom već postoji.", greska.Description);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void InvalidUserName_Poziv_VracaBosanskuPoruku()
        {
            // Act
            var greska = _opisivac.InvalidUserName("test");

            // Assert
            Assert.AreEqual("InvalidUserName", greska.Code);
            Assert.AreEqual("Korisničko ime 'test' nije validno, može sadržavati samo slova ili cifre.", greska.Description);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void InvalidEmail_Poziv_VracaBosanskuPoruku()
        {
            // Act
            var greska = _opisivac.InvalidEmail("test");

            // Assert
            Assert.AreEqual("InvalidEmail", greska.Code);
            Assert.AreEqual("Email 'test' nije validan.", greska.Description);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void DuplicateUserName_Poziv_VracaBosanskuPoruku()
        {
            // Act
            var greska = _opisivac.DuplicateUserName("test");

            // Assert
            Assert.AreEqual("DuplicateUserName", greska.Code);
            Assert.AreEqual("Korisničko ime je već zauzeto.", greska.Description);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void DuplicateEmail_Poziv_VracaBosanskuPoruku()
        {
            // Act
            var greska = _opisivac.DuplicateEmail("test");

            // Assert
            Assert.AreEqual("DuplicateEmail", greska.Code);
            Assert.AreEqual("Email adresa je već registrovana.", greska.Description);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void InvalidRoleName_Poziv_VracaBosanskuPoruku()
        {
            // Act
            var greska = _opisivac.InvalidRoleName("test");

            // Assert
            Assert.AreEqual("InvalidRoleName", greska.Code);
            Assert.AreEqual("Naziv uloge 'test' nije validan.", greska.Description);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void DuplicateRoleName_Poziv_VracaBosanskuPoruku()
        {
            // Act
            var greska = _opisivac.DuplicateRoleName("test");

            // Assert
            Assert.AreEqual("DuplicateRoleName", greska.Code);
            Assert.AreEqual("Naziv uloge 'test' je već zauzet.", greska.Description);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void UserAlreadyHasPassword_Poziv_VracaBosanskuPoruku()
        {
            // Act
            var greska = _opisivac.UserAlreadyHasPassword();

            // Assert
            Assert.AreEqual("UserAlreadyHasPassword", greska.Code);
            Assert.AreEqual("Korisnik već ima postavljenu lozinku.", greska.Description);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void UserLockoutNotEnabled_Poziv_VracaBosanskuPoruku()
        {
            // Act
            var greska = _opisivac.UserLockoutNotEnabled();

            // Assert
            Assert.AreEqual("UserLockoutNotEnabled", greska.Code);
            Assert.AreEqual("Zaključavanje naloga nije omogućeno za ovog korisnika.", greska.Description);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void UserAlreadyInRole_Poziv_VracaBosanskuPoruku()
        {
            // Act
            var greska = _opisivac.UserAlreadyInRole("test");

            // Assert
            Assert.AreEqual("UserAlreadyInRole", greska.Code);
            Assert.AreEqual("Korisnik je već u ulozi 'test'.", greska.Description);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void UserNotInRole_Poziv_VracaBosanskuPoruku()
        {
            // Act
            var greska = _opisivac.UserNotInRole("test");

            // Assert
            Assert.AreEqual("UserNotInRole", greska.Code);
            Assert.AreEqual("Korisnik nije u ulozi 'test'.", greska.Description);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void PasswordTooShort_Poziv_VracaBosanskuPoruku()
        {
            // Act
            var greska = _opisivac.PasswordTooShort(8);

            // Assert
            Assert.AreEqual("PasswordTooShort", greska.Code);
            Assert.AreEqual("Lozinka mora imati najmanje 8 znakova.", greska.Description);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void PasswordRequiresUniqueChars_Poziv_VracaBosanskuPoruku()
        {
            // Act
            var greska = _opisivac.PasswordRequiresUniqueChars(8);

            // Assert
            Assert.AreEqual("PasswordRequiresUniqueChars", greska.Code);
            Assert.AreEqual("Lozinka mora sadržavati najmanje 8 različitih znakova.", greska.Description);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void PasswordRequiresNonAlphanumeric_Poziv_VracaBosanskuPoruku()
        {
            // Act
            var greska = _opisivac.PasswordRequiresNonAlphanumeric();

            // Assert
            Assert.AreEqual("PasswordRequiresNonAlphanumeric", greska.Code);
            Assert.AreEqual("Lozinka mora sadržavati barem jedan specijalni znak ili znak interpunkcije.", greska.Description);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void PasswordRequiresDigit_Poziv_VracaBosanskuPoruku()
        {
            // Act
            var greska = _opisivac.PasswordRequiresDigit();

            // Assert
            Assert.AreEqual("PasswordRequiresDigit", greska.Code);
            Assert.AreEqual("Lozinka mora sadržavati barem jednu cifru.", greska.Description);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void PasswordRequiresUpper_Poziv_VracaBosanskuPoruku()
        {
            // Act
            var greska = _opisivac.PasswordRequiresUpper();

            // Assert
            Assert.AreEqual("PasswordRequiresUpper", greska.Code);
            Assert.AreEqual("Lozinka mora sadržavati barem jedno veliko slovo.", greska.Description);
        }

        [TestMethod]
        [Owner("Sajra Malkić")]
        public void PasswordRequiresLower_Poziv_VracaBosanskuPoruku()
        {
            // Act
            var greska = _opisivac.PasswordRequiresLower();

            // Assert
            Assert.AreEqual("PasswordRequiresLower", greska.Code);
            Assert.AreEqual("Lozinka mora sadržavati barem jedno malo slovo.", greska.Description);
        }
    }
}
