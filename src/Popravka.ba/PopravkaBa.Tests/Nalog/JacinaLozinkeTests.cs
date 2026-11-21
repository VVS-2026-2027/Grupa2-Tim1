using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Shared;

namespace PopravkaBa.Tests.Nalog
{
    [TestClass]
    [TestCategory("Domain")]
    [TestCategory("TDD")]
    public class JacinaLozinkeTests
    {
        private const string Email = "korisnik@popravka.ba";

        [TestMethod]
        [Owner("Emina Vatrić")]
        public void Procijeni_KratkaLozinka_VracaSlaba()
        {
            // Arrange
            var lozinka = "abc";

            // Act
            var rezultat = JacinaLozinke.Procijeni(lozinka, Email);

            // Assert
            Assert.AreEqual(Jacina.Slaba, rezultat);
        }

        [TestMethod]
        [Owner("Emina Vatrić")]
        public void Procijeni_DuzinaICifra_VracaSrednja()
        {
            var rezultat = JacinaLozinke.Procijeni("abcdefg1", Email);

            Assert.AreEqual(Jacina.Srednja, rezultat);
        }

        [TestMethod]
        [Owner("Emina Vatrić")]
        public void Procijeni_DuzinaIVelikoSlovo_VracaSrednja()
        {
            var rezultat = JacinaLozinke.Procijeni("Abcdefgh", Email);

            Assert.AreEqual(Jacina.Srednja, rezultat);
        }

        [TestMethod]
        [Owner("Emina Vatrić")]
        public void Procijeni_DuzinaISpecijalniZnak_VracaSrednja()
        {
            var rezultat = JacinaLozinke.Procijeni("abcdefg!", Email);

            Assert.AreEqual(Jacina.Srednja, rezultat);
        }

        [TestMethod]
        [Owner("Emina Vatrić")]
        public void Procijeni_SviKriterijiIspunjeni_VracaJaka()
        {
            var rezultat = JacinaLozinke.Procijeni("Abcdefg1!", Email);

            Assert.AreEqual(Jacina.Jaka, rezultat);
        }

        [TestMethod]
        [Owner("Emina Vatrić")]
        public void Procijeni_LozinkaSadrziDioEmaila_VracaSlaba()
        {
            var rezultat = JacinaLozinke.Procijeni("Amina123!x", "amina@gmail.com");

            Assert.AreEqual(Jacina.Slaba, rezultat);
        }

        [TestMethod]
        [Owner("Emina Vatrić")]
        public void Procijeni_KratakDioEmaila_Ignorira()
        {
            var rezultat = JacinaLozinke.Procijeni("Abcdefg1!", "a@gmail.com");

            Assert.AreEqual(Jacina.Jaka, rezultat);
        }

        [TestMethod]
        [Owner("Emina Vatrić")]
        public void Procijeni_EmailNull_Ignorira()
        {
            var rezultat = JacinaLozinke.Procijeni("Abcdefg1!", null);

            Assert.AreEqual(Jacina.Jaka, rezultat);
        }

        [TestMethod]
        [Owner("Emina Vatrić")]
        public void Procijeni_LozinkaNull_BacaArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => JacinaLozinke.Procijeni(null!, Email));
        }

        [TestMethod]
        [Owner("Emina Vatrić")]
        public void Procijeni_PrazanString_BacaArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(() => JacinaLozinke.Procijeni("", Email));
        }

        [TestMethod]
        [Owner("Emina Vatrić")]
        public void Procijeni_LozinkaSaRazmacima_RazmaciSeIgnorisu()
        {
            // "abc de f1" bez razmaka je "abcdef1" (7 znakova), samo 1 bod
            var rezultat = JacinaLozinke.Procijeni("abc de f1", Email);

            Assert.AreEqual(Jacina.Slaba, rezultat);
        }

        [TestMethod]
        [Owner("Emina Vatrić")]
        public void Procijeni_SamoRazmaci_BacaArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(() => JacinaLozinke.Procijeni("   ", Email));
        }

        [TestMethod]
        [Owner("Emina Vatrić")]
        public void Procijeni_CestaLozinkaPassword_VracaSlaba()
        {
            var rezultat = JacinaLozinke.Procijeni("Password", Email);

            Assert.AreEqual(Jacina.Slaba, rezultat);
        }

        [TestMethod]
        [Owner("Emina Vatrić")]
        public void Procijeni_CestaLozinka12345678_VracaSlaba()
        {
            var rezultat = JacinaLozinke.Procijeni("12345678", Email);

            Assert.AreEqual(Jacina.Slaba, rezultat);
        }

        [TestMethod]
        [Owner("Emina Vatrić")]
        public void Procijeni_LozinkaSadrziCestuRijec_VracaSlaba()
        {
            var rezultat = JacinaLozinke.Procijeni("Password1!", Email);

            Assert.AreEqual(Jacina.Slaba, rezultat);
        }

        [TestMethod]
        [Owner("Amina Bukvić")]
        public void Procijeni_SedamZnakova_NeDobijaBodZaDuzinu()
        {
            // Arrange: cifra, veliko slovo i specijalni znak, ali samo 7 znakova
            var lozinka = "Abcde1!";

            // Act
            var rezultat = JacinaLozinke.Procijeni(lozinka, Email);

            // Assert
            Assert.AreEqual(Jacina.Srednja, rezultat);
        }

        [TestMethod]
        [Owner("Amina Bukvić")]
        public void Procijeni_OsamZnakova_DobijaBodZaDuzinu()
        {
            // Arrange: isti kriteriji kao gore, sa tačno 8 znakova
            var lozinka = "Abcdef1!";

            // Act
            var rezultat = JacinaLozinke.Procijeni(lozinka, Email);

            // Assert
            Assert.AreEqual(Jacina.Jaka, rezultat);
        }

        [TestMethod]
        [Owner("Amina Bukvić")]
        public void Procijeni_DioEmailaOdCetiriZnaka_VracaJaka()
        {
            // Arrange: "mina" ima 4 znaka, a pravilo emaila važi tek od 5
            var lozinka = "Mina123!x";

            // Act
            var rezultat = JacinaLozinke.Procijeni(lozinka, "mina@gmail.com");

            // Assert
            Assert.AreEqual(Jacina.Jaka, rezultat);
        }

        [TestMethod]
        [Owner("Amina Bukvić")]
        public void Procijeni_DioEmailaOdPetZnakova_VracaSlaba()
        {
            // Arrange: "emina" ima tačno 5 znakova
            var lozinka = "Emina123!x";

            // Act
            var rezultat = JacinaLozinke.Procijeni(lozinka, "emina@gmail.com");

            // Assert
            Assert.AreEqual(Jacina.Slaba, rezultat);
        }

        [TestMethod]
        [Owner("Amina Bukvić")]
        public void Procijeni_VelikoSlovoSaDijakritikom_DobijaBodZaVelikoSlovo()
        {
            // Arrange: dužina + veliko slovo Č = 2 boda
            var lozinka = "Čabcdefg";

            // Act
            var rezultat = JacinaLozinke.Procijeni(lozinka, Email);

            // Assert
            Assert.AreEqual(Jacina.Srednja, rezultat);
        }

        [TestMethod]
        [Owner("Amina Bukvić")]
        public void Procijeni_ViseCifara_BodZaCifruSeDajeJednom()
        {
            // Arrange: tri cifre i dalje daju samo 1 bod, pa je ukupno 2 (dužina + cifra)
            var lozinka = "abcdefg123";

            // Act
            var rezultat = JacinaLozinke.Procijeni(lozinka, Email);

            // Assert
            Assert.AreEqual(Jacina.Srednja, rezultat);
        }

        public static IEnumerable<object[]> SlucajeviIzCsv =>
    DataDrivenInputParser.CitajCsv<string, string?, Jacina>("TestData/csv/JacinaLozinke.csv");

        [TestMethod]
        [Owner("Emina Vatrić")]
        [DynamicData(nameof(SlucajeviIzCsv))]
        [TestCategory("DataDriven")]
        public void Procijeni_PodaciIzCsv_VracaOcekivano(string lozinka, string? email, Jacina ocekivano)
        {
            var rezultat = JacinaLozinke.Procijeni(lozinka, email);

            Assert.AreEqual(ocekivano, rezultat);
        }
    }
}