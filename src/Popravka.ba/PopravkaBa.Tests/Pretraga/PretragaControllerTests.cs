using Microsoft.AspNetCore.Mvc;
using PopravkaBa.Application.DTOs;
using PopravkaBa.Application.Services.Interface;
using PopravkaBa.Application.Strategies.Interface;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Models;
using PopravkaBa.Web.Controllers;
using PopravkaBa.Web.Models.ViewModels;

namespace PopravkaBa.Tests.Pretraga;

[TestClass]
[TestCategory("Controller")]
public class PretragaControllerTests
{
    private Mock<IPretragaService> _pretraga = null!;
    private Mock<IMjestoService> _mjesta = null!;
    private Mock<IKategorijaService> _kategorije = null!;
    private Mock<IPretragaStrategy> _strategijaKlijenta = null!;
    private Mock<IPretragaStrategy> _strategijaMajstora = null!;
    private PretragaController _kontroler = null!;

    [TestInitialize]
    public void PripremiTest()
    {
        _pretraga = new Mock<IPretragaService>();
        _mjesta = new Mock<IMjestoService>();
        _kategorije = new Mock<IKategorijaService>();

        _strategijaKlijenta = new Mock<IPretragaStrategy>();
        _strategijaKlijenta.Setup(s => s.DajUlogu()).Returns(KorisnickeUloge.Klijent);

        _strategijaMajstora = new Mock<IPretragaStrategy>();
        _strategijaMajstora.Setup(s => s.DajUlogu()).Returns(KorisnickeUloge.Majstor);

        _kontroler = new PretragaController(
            _pretraga.Object,
            new[] { _strategijaKlijenta.Object, _strategijaMajstora.Object },
            _mjesta.Object,
            _kategorije.Object);
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task Index_BezUloge_PreusmjeravaNaPrijavu()
    {
        // Arrange
        TestHelpers.NeprijavljenKorisnik(_kontroler);
        var filteri = new FilterPretrageDto();

        // Act
        var rezultat = await _kontroler.Index(filteri);

        // Assert
        var preusmjerenje = (RedirectToActionResult)rezultat;
        Assert.AreEqual("Login", preusmjerenje.ActionName);
        Assert.AreEqual("Account", preusmjerenje.ControllerName);
        _pretraga.VerifyNoOtherCalls();
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task Index_UlogaBezStrategije_VracaForbid()
    {
        // Arrange
        TestHelpers.PrijaviKorisnika(_kontroler, "f1", nameof(KorisnickeUloge.Firma));
        var filteri = new FilterPretrageDto();

        // Act
        var rezultat = await _kontroler.Index(filteri);

        // Assert
        Assert.IsInstanceOfType<ForbidResult>(rezultat);
        _pretraga.VerifyNoOtherCalls();
        _mjesta.VerifyNoOtherCalls();
        _kategorije.VerifyNoOtherCalls();
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    [DataRow(KorisnickeUloge.Klijent)]
    [DataRow(KorisnickeUloge.Majstor)]
    public async Task Index_UlogaSaStrategijom_VracaPopunjenModelPretrage(
        KorisnickeUloge uloga)
    {
        // Arrange
        TestHelpers.PrijaviKorisnika(_kontroler, "u1", uloga.ToString());
        var strategija = uloga == KorisnickeUloge.Klijent
            ? _strategijaKlijenta.Object
            : _strategijaMajstora.Object;
        var filteri = new FilterPretrageDto { KljucneRijeci = "popravka" };
        var ocekivaniRezultat = new RezultatPretrageDto { UkupnoStavki = 2 };
        var mjesta = new[] { new Mjesto { MjestoID = 1, Naziv = "Sarajevo" } };
        var kategorije = new[] { new Kategorija { ID = 1, Naziv = "Vodoinstalacije" } };

        _pretraga.Setup(s => s.PretraziAsync(filteri, strategija))
            .ReturnsAsync(ocekivaniRezultat);
        _mjesta.Setup(s => s.DajSvaMjestaAsync()).ReturnsAsync(mjesta);
        _kategorije.Setup(s => s.DajSveKategorije()).ReturnsAsync(kategorije);

        // Act
        var rezultat = await _kontroler.Index(filteri);

        // Assert
        var model = (PretragaViewModel)((ViewResult)rezultat).Model!;
        Assert.AreSame(ocekivaniRezultat, model.Rezultat);
        Assert.AreSame(filteri, model.Filteri);
        CollectionAssert.AreEqual(mjesta, model.Mjesta.ToArray());
        CollectionAssert.AreEqual(kategorije, model.Kategorije.ToArray());
        _pretraga.Verify(s => s.PretraziAsync(filteri, strategija), Times.Once());
    }
}