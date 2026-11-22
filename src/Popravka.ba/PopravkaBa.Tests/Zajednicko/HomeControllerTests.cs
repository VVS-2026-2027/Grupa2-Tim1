using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Popravka.ba.Models;
using PopravkaBa.Application.Services.Interface;
using PopravkaBa.Domain.Models;
using PopravkaBa.Web.Controllers;
using PopravkaBa.Web.Models.ViewModels;
using static PopravkaBa.Tests.Oglasi.OglasiTestHelper;

namespace PopravkaBa.Tests.Zajednicko;

[TestClass]
[TestCategory("Controller")]
public class HomeControllerTests
{
    private Mock<IMjestoService> _mjesta = null!;
    private Mock<IOglasService> _oglasi = null!;
    private Mock<IOglasUslugeService> _oglasiUsluga = null!;
    private Mock<IKategorijaService> _kategorije = null!;
    private Mock<IHostEnvironment> _okruzenje = null!;
    private HomeController _kontroler = null!;

    [TestInitialize]
    public void Postavi()
    {
        _mjesta = new Mock<IMjestoService>();
        _oglasi = new Mock<IOglasService>();
        _oglasiUsluga = new Mock<IOglasUslugeService>();
        _kategorije = new Mock<IKategorijaService>();
        _okruzenje = new Mock<IHostEnvironment>();

        _kategorije.Setup(k => k.DajTopKategorijePoMajstorimaAsync(4)).ReturnsAsync(Array.Empty<(Kategorija, int)>());
        _mjesta.Setup(m => m.DajSvaMjestaAsync()).ReturnsAsync(Array.Empty<Mjesto>());

        _kontroler = new HomeController(NullLogger<HomeController>.Instance, _mjesta.Object, _oglasi.Object,
            _oglasiUsluga.Object, _kategorije.Object, _okruzenje.Object);
        _kontroler.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    private async Task<HomeViewModel> Index() => (HomeViewModel)((ViewResult)await _kontroler.Index()).Model!;

    private static T SaVlasnikom<T>(T oglas, string ime, string prezime) where T : Oglas
    {
        oglas.VlasnikOglasa = new Klijent { Ime = ime, Prezime = prezime, Slika = "vlasnik.png" };
        return oglas;
    }

    private void IzuzetakUZahtjevu(Exception izuzetak) =>
        _kontroler.HttpContext.Features.Set<IExceptionHandlerFeature>(new ExceptionHandlerFeature { Error = izuzetak });

    // ---------- Index ----------

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Index_PodaciIzServisa_PunjiMjestaIBrojZavrsenihUsluga()
    {
        // Arrange
        var mjesta = new[] { new Mjesto { MjestoID = 1, Naziv = "Sarajevo" } };
        _mjesta.Setup(m => m.DajSvaMjestaAsync()).ReturnsAsync(mjesta);
        _oglasiUsluga.Setup(o => o.DajBrojZavrsenihAsync()).ReturnsAsync(42);

        // Act
        var vm = await Index();

        // Assert
        Assert.AreSame(mjesta, vm.Mjesta);
        Assert.AreEqual(42, vm.BrojRealiziranihUsluga);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Index_TopKategorije_MapiraNazivIBrojIzvrsilaca()
    {
        // Arrange
        _kategorije.Setup(k => k.DajTopKategorijePoMajstorimaAsync(4))
            .ReturnsAsync(new[] { (new Kategorija { ID = 3, Naziv = "Vodoinstalacije" }, 12) });

        // Act
        var kategorija = (await Index()).TopKategorije!.Single();

        // Assert
        Assert.AreEqual(3, kategorija.ID);
        Assert.AreEqual("Vodoinstalacije", kategorija.Naziv);
        Assert.AreEqual(12, kategorija.AktivniIzvrsilacCount);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Index_NedavniOglasUsluge_MapiraOglasITip()
    {
        // Arrange
        var oglas = SaVlasnikom(OglasUsluge(5), "Haso", "Hasić");
        oglas.Ponude = new List<PonudaUsluge> { new(), new() };
        _oglasi.Setup(o => o.DajNedavne(4)).ReturnsAsync(new Oglas[] { oglas });

        // Act
        var stavka = (await Index()).NedavniOglasi!.Single();

        // Assert
        Assert.AreEqual(5, stavka.ID);
        Assert.AreEqual(oglas.Naslov, stavka.Naslov);
        Assert.AreEqual("Haso H.", stavka.VlasnikOglasaSkracenoIme);
        Assert.AreEqual(2, stavka.BrojPonuda);
        Assert.AreEqual("OglasUsluge", stavka.TipOglasa);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Index_NedavniOglasRadnogMjesta_TipJeOglasRadnoMjesto()
    {
        // Arrange
        _oglasi.Setup(o => o.DajNedavne(4)).ReturnsAsync(new Oglas[] { SaVlasnikom(OglasRadnoMjesto(5), "Haso", "Hasić") });

        // Act
        var stavka = (await Index()).NedavniOglasi!.Single();

        // Assert
        Assert.AreEqual("OglasRadnoMjesto", stavka.TipOglasa);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    [DataRow("Haso", "Hasić", "Haso H.")]
    [DataRow("Muhamed-Abdulah", "Kovač", "Muhamed-Abdulah...")]
    public async Task Index_ImeVlasnika_SkracujeImeDuzeOd15Znakova(string ime, string prezime, string ocekivano)
    {
        // Arrange
        _oglasi.Setup(o => o.DajNedavne(4)).ReturnsAsync(new Oglas[] { SaVlasnikom(OglasUsluge(1), ime, prezime) });

        // Act
        var stavka = (await Index()).NedavniOglasi!.Single();

        // Assert
        Assert.AreEqual(ocekivano, stavka.VlasnikOglasaSkracenoIme);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Index_NemaNedavnihOglasa_VracaPraznuListu()
    {
        // Arrange
        _oglasi.Setup(o => o.DajNedavne(4)).ReturnsAsync((IEnumerable<Oglas>?)null);

        // Act
        var vm = await Index();

        // Assert
        Assert.AreEqual(0, vm.NedavniOglasi!.Count());
    }

    // ---------- Privacy, Error, StatusCode ----------

    [TestMethod]
    [Owner("Aid Mustafić")]
    public void Privacy_Poziv_VracaPogled()
    {
        // Act
        var rezultat = _kontroler.Privacy();

        // Assert
        Assert.IsInstanceOfType<ViewResult>(rezultat);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public void Error_RazvojnoOkruzenje_PrikazujePorukuIzuzetka()
    {
        // Arrange
        _okruzenje.Setup(o => o.EnvironmentName).Returns(Environments.Development);
        IzuzetakUZahtjevu(new InvalidOperationException("Nešto je puklo"));

        // Act
        var vm = (ErrorViewModel)((ViewResult)_kontroler.Error()).Model!;

        // Assert
        Assert.AreEqual("Nešto je puklo", vm.ExceptionMessage);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public void Error_ProdukcijskoOkruzenje_SkrivaDetaljeIzuzetka()
    {
        // Arrange
        _okruzenje.Setup(o => o.EnvironmentName).Returns(Environments.Production);
        IzuzetakUZahtjevu(new InvalidOperationException("Nešto je puklo"));

        // Act
        var vm = (ErrorViewModel)((ViewResult)_kontroler.Error()).Model!;

        // Assert
        Assert.IsNull(vm.ExceptionMessage);
        Assert.IsNull(vm.StackTrace);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public void Error_BezAktivnosti_RequestIdJeIdZahtjeva()
    {
        // Arrange
        _okruzenje.Setup(o => o.EnvironmentName).Returns(Environments.Production);
        _kontroler.HttpContext.TraceIdentifier = "zahtjev-1";

        // Act
        var vm = (ErrorViewModel)((ViewResult)_kontroler.Error()).Model!;

        // Assert
        Assert.AreEqual("zahtjev-1", vm.RequestId);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    [DataRow(404, "NotFound")]
    [DataRow(403, "AccessDenied")]
    [DataRow(429, "TooManyRequests")]
    [DataRow(500, "InternalServerError")]
    [DataRow(418, "Error")]
    public void StatusCode_KodGreske_PrikazujeOdgovarajuciPogled(int kod, string pogled)
    {
        // Act
        var rezultat = (ViewResult)_kontroler.StatusCode(kod);

        // Assert
        Assert.AreEqual(pogled, rezultat.ViewName);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public void StatusCode_KodGreske_PostavljaStatusOdgovora()
    {
        // Act
        _kontroler.StatusCode(404);

        // Assert
        Assert.AreEqual(404, _kontroler.Response.StatusCode);
    }
}
