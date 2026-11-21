using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PopravkaBa.Application.DTOs;
using PopravkaBa.Application.Services.Interface;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Web.Controllers;
using PopravkaBa.Web.Models.ViewModels;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PopravkaBa.Tests.Kontroleri
{
    [TestClass]
    public class StatsControllerTests
    {
        private Mock<IStatistikaService> _servis = null!;
        private StatsController _kontroler = null!;
        private StatistikaDTO _rezultatServisa = null!;

        [TestInitialize]
        public void Inicijaliziraj()
        {
            _rezultatServisa = new StatistikaDTO();
            _servis = new Mock<IStatistikaService>();
            _servis
                .Setup(s => s.DohvatiRangListu(It.IsAny<StatistikaFilterDTO>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_rezultatServisa);
            _kontroler = new StatsController(_servis.Object);
        }

        [TestMethod]
        public async Task Index_ValidanZahtjev_VracaViewSaRezultatomServisa()
        {
            // Act
            var rezultat = await _kontroler.Index(null, null, null, null, CancellationToken.None);

            // Assert
            var view = rezultat as ViewResult;
            Assert.IsNotNull(view);
            var model = view.Model as StatistikaViewModel;
            Assert.IsNotNull(model);
            Assert.AreSame(_rezultatServisa, model.Rezultat);
        }

        [TestMethod]
        public async Task Index_ValidanZahtjev_PozivaServisTacnoJednom()
        {
            // Act
            await _kontroler.Index(null, null, null, null, CancellationToken.None);

            // Assert
            _servis.Verify(
                s => s.DohvatiRangListu(It.IsAny<StatistikaFilterDTO>(), It.IsAny<CancellationToken>()),
                Times.Once());
        }

        [TestMethod]
        [DataRow("majstor", KoloneStatistike.Majstor)]
        [DataRow("kategorija", KoloneStatistike.Kategorija)]
        [DataRow("lokacija", KoloneStatistike.Lokacija)]
        [DataRow("poslovi", KoloneStatistike.Poslovi)]
        [DataRow("ocjena", KoloneStatistike.Ocjena)]
        [DataRow("MAJSTOR", KoloneStatistike.Majstor)]
        [DataRow("Lokacija", KoloneStatistike.Lokacija)]
        [DataRow("nepoznato", KoloneStatistike.Ocjena)]
        [DataRow("", KoloneStatistike.Ocjena)]
        public async Task Index_RazlicitSort_ServisuProslijedjujeOdgovarajucuKolonu(
            string sort, KoloneStatistike ocekivanaKolona)
        {
            // Act
            await _kontroler.Index(null, null, sort, null, CancellationToken.None);

            // Assert
            _servis.Verify(
                s => s.DohvatiRangListu(
                    It.Is<StatistikaFilterDTO>(f => f.AktivnaKolona == ocekivanaKolona),
                    It.IsAny<CancellationToken>()),
                Times.Once());
        }

        [TestMethod]
        public async Task Index_SortNijeZadan_ServisuProslijedjujeKolonuOcjena()
        {
            // Act
            await _kontroler.Index(null, null, null, null, CancellationToken.None);

            // Assert
            _servis.Verify(
                s => s.DohvatiRangListu(
                    It.Is<StatistikaFilterDTO>(f => f.AktivnaKolona == KoloneStatistike.Ocjena),
                    It.IsAny<CancellationToken>()),
                Times.Once());
        }

        [TestMethod]
        [DataRow("asc", Sortiranje.Ascending)]
        [DataRow("ASC", Sortiranje.Ascending)]
        [DataRow("desc", Sortiranje.Descending)]
        [DataRow("nepoznato", Sortiranje.Descending)]
        [DataRow("", Sortiranje.Descending)]
        public async Task Index_RazlicitSmjer_ServisuProslijedjujeOdgovarajuciSmjer(
            string smjer, Sortiranje ocekivaniSmjer)
        {
            // Act
            await _kontroler.Index(null, null, null, smjer, CancellationToken.None);

            // Assert
            _servis.Verify(
                s => s.DohvatiRangListu(
                    It.Is<StatistikaFilterDTO>(f => f.Smjer == ocekivaniSmjer),
                    It.IsAny<CancellationToken>()),
                Times.Once());
        }

        [TestMethod]
        public async Task Index_SmjerNijeZadan_ServisuProslijedjujeOpadajuciSmjer()
        {
            // Act
            await _kontroler.Index(null, null, null, null, CancellationToken.None);

            // Assert
            _servis.Verify(
                s => s.DohvatiRangListu(
                    It.Is<StatistikaFilterDTO>(f => f.Smjer == Sortiranje.Descending),
                    It.IsAny<CancellationToken>()),
                Times.Once());
        }

        [TestMethod]
        public async Task Index_ZadanaKategorijaIMjesto_ServisuProslijedjujeIdeve()
        {
            // Act
            await _kontroler.Index(5, 7, null, null, CancellationToken.None);

            // Assert
            _servis.Verify(
                s => s.DohvatiRangListu(
                    It.Is<StatistikaFilterDTO>(f => f.KategorijaId == 5 && f.MjestoId == 7),
                    It.IsAny<CancellationToken>()),
                Times.Once());
        }

        [TestMethod]
        public async Task Index_NezadanaKategorijaIMjesto_FilterImaNullVrijednosti()
        {
            // Act
            await _kontroler.Index(null, null, null, null, CancellationToken.None);

            // Assert
            _servis.Verify(
                s => s.DohvatiRangListu(
                    It.Is<StatistikaFilterDTO>(f => f.KategorijaId == null && f.MjestoId == null),
                    It.IsAny<CancellationToken>()),
                Times.Once());
        }

        [TestMethod]
        public async Task Index_ZadanToken_ProslijedjujeSeServisu()
        {
            // Arrange
            using var izvor = new CancellationTokenSource();

            // Act
            await _kontroler.Index(null, null, null, null, izvor.Token);

            // Assert
            _servis.Verify(
                s => s.DohvatiRangListu(It.IsAny<StatistikaFilterDTO>(), izvor.Token),
                Times.Once());
        }

        [TestMethod]
        public async Task Index_ServisBacaIzuzetak_PropagiraIzuzetak()
        {
            // Arrange
            _servis
                .Setup(s => s.DohvatiRangListu(It.IsAny<StatistikaFilterDTO>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Greska servisa"));

            // Act
            var izuzetak = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => _kontroler.Index(null, null, null, null, CancellationToken.None));

            // Assert
            Assert.AreEqual("Greska servisa", izuzetak.Message);
        }
    }
}
