using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using PopravkaBa.Application.DTOs;
using PopravkaBa.Application.Services.Interface;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Models;
using PopravkaBa.Web.Controllers;
using PopravkaBa.Web.Models.ViewModels;
using static PopravkaBa.Tests.Oglasi.OglasControllerTestHelper;
using static PopravkaBa.Tests.Oglasi.OglasiTestHelper;

namespace PopravkaBa.Tests.Oglasi;

[TestClass]
[TestCategory("Controller")]
public class OglasMajstoraControllerTests
{
    private Mock<IOglasMajstoraFacade> _fasada = null!;
    private Mock<IMjestoService> _mjesta = null!;
    private Mock<UserManager<ApplicationUser>> _userManager = null!;
    private Mock<IFileStorage> _storage = null!;
    private OglasMajstoraController _kontroler = null!;
    private Kategorija[] _kategorije = null!;

    [TestInitialize]
    public void Postavi()
    {
        _fasada = new Mock<IOglasMajstoraFacade>();
        _mjesta = new Mock<IMjestoService>();
        _userManager = TestHelpers.MockUserManager();
        _storage = new Mock<IFileStorage>();
        _kategorije = new[] { new Kategorija { ID = 1, Naziv = "Vodoinstalacije" } };

        _fasada.Setup(f => f.DajSveKategorije()).ReturnsAsync(_kategorije);
        _userManager.KorisnikIzClaimova();

        _kontroler = new OglasMajstoraController(_fasada.Object, _mjesta.Object, _userManager.Object,
            NullLogger<OglasMajstoraController>.Instance, _storage.Object);
        TestHelpers.PrijaviKorisnika(_kontroler, VlasnikId, "Majstor");
    }

    private void PostojiOglas(OglasMajstora oglas) =>
        _fasada.Setup(f => f.DajOglasPoId(oglas.OglasID)).ReturnsAsync(oglas);

    // ---------- Index ----------

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Index_BezPretrage_PrikazujeSveOglase()
    {
        // Arrange
        var oglasi = new[] { OglasMajstora(1) };
        _fasada.Setup(f => f.DajSveOglase()).ReturnsAsync(oglasi);

        // Act
        var rezultat = await _kontroler.Index(null, null);

        // Assert
        ProvjeriPogledSaModelom(rezultat, oglasi);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    [DataRow("bojler", null)]
    [DataRow(null, 2)]
    public async Task Index_TekstIliLokacija_PrikazujeRezultatePretrage(string? tekst, int? lokacija)
    {
        // Arrange
        var oglasi = new[] { OglasMajstora(2) };
        _fasada.Setup(f => f.PronadjiOglase(tekst!, lokacija)).ReturnsAsync(oglasi);

        // Act
        var rezultat = await _kontroler.Index(tekst, lokacija);

        // Assert
        ProvjeriPogledSaModelom(rezultat, oglasi);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Index_TekstPretrage_VracaTekstUViewBag()
    {
        // Arrange
        _fasada.Setup(f => f.PronadjiOglase("bojler", null)).ReturnsAsync(Array.Empty<OglasMajstora>());

        // Act
        await _kontroler.Index("bojler", null);

        // Assert
        Assert.AreEqual("bojler", _kontroler.ViewData["Search"]);
    }

    // ---------- Detalji ----------

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Detalji_NepostojeciOglas_VracaNotFound()
    {
        // Act
        var rezultat = await _kontroler.Detalji(99);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Detalji_PostojeciOglas_MapiraPodatkeOglasa()
    {
        // Arrange
        var oglas = OglasMajstora(1);
        oglas.Mjesto = new Mjesto { MjestoID = 1, Naziv = "Sarajevo" };
        oglas.Kategorije = new[]
        {
            new OglasKategorija { Kategorija = new Kategorija { Naziv = "Vodoinstalacije" } },
            new OglasKategorija { Kategorija = null! }
        };
        PostojiOglas(oglas);

        // Act
        var vm = (OglasMajstoraDetaljiViewModel)((ViewResult)await _kontroler.Detalji(1)).Model!;

        // Assert
        Assert.AreEqual(oglas.Naslov, vm.Naslov);
        Assert.AreEqual("Sarajevo", vm.Lokacija);
        Assert.AreEqual(oglas.MinCijena, vm.MinCijena);
        CollectionAssert.AreEqual(new[] { "Vodoinstalacije" }, vm.Kategorije);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Detalji_VlasnikPostoji_PrikazujeKorisnickoImeVlasnika()
    {
        // Arrange
        PostojiOglas(OglasMajstora(1));
        _userManager.Setup(m => m.FindByIdAsync(VlasnikId)).ReturnsAsync(new Majstor { UserName = "haso" });

        // Act
        var vm = (OglasMajstoraDetaljiViewModel)((ViewResult)await _kontroler.Detalji(1)).Model!;

        // Assert
        Assert.AreEqual("haso", vm.VlasnikUsername);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Detalji_VlasnikNePostoji_PrikazujeIdVlasnika()
    {
        // Arrange
        PostojiOglas(OglasMajstora(1));

        // Act
        var vm = (OglasMajstoraDetaljiViewModel)((ViewResult)await _kontroler.Detalji(1)).Model!;

        // Assert
        Assert.AreEqual(VlasnikId, vm.VlasnikUsername);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    [DataRow(VlasnikId, "Majstor", true)]
    [DataRow(DrugiKorisnikId, "Administrator", true)]
    [DataRow(DrugiKorisnikId, "Majstor", false)]
    public async Task Detalji_KorisnikIUloga_OdredjujeDaLiJeVlasnik(string korisnikId, string uloga, bool jeVlasnik)
    {
        // Arrange
        PostojiOglas(OglasMajstora(1));
        TestHelpers.PrijaviKorisnika(_kontroler, korisnikId, uloga);

        // Act
        var vm = (OglasMajstoraDetaljiViewModel)((ViewResult)await _kontroler.Detalji(1)).Model!;

        // Assert
        Assert.AreEqual(jeVlasnik, vm.JeVlasnik);
    }

    // ---------- ObjaviOglas ----------

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObjaviOglas_Get_PrikazujeFormuSaKategorijama()
    {
        // Act
        await _kontroler.ObjaviOglas();

        // Assert
        Assert.AreSame(_kategorije, _kontroler.ViewData["Kategorije"]);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObjaviOglas_Post_NevalidanModel_VracaFormuBezObjave()
    {
        // Arrange
        var dto = ObjaviMajstoraDto();
        _kontroler.ModelState.AddModelError("Naslov", "Naslov je obavezan.");

        // Act
        var rezultat = await _kontroler.ObjaviOglas(dto);

        // Assert
        ProvjeriPogledSaModelom(rezultat, dto);
        _fasada.Verify(f => f.ObjaviOglas(It.IsAny<ObjaviOglasMajstoraDto>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObjaviOglas_Post_IspravanModel_ObjavljujeOglasZaPrijavljenogKorisnika()
    {
        // Arrange
        var dto = ObjaviMajstoraDto();

        // Act
        await _kontroler.ObjaviOglas(dto);

        // Assert
        _fasada.Verify(f => f.ObjaviOglas(dto, VlasnikId), Times.Once);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObjaviOglas_Post_IspravanModel_PreusmjeravaNaListuOglasa()
    {
        // Act
        var rezultat = await _kontroler.ObjaviOglas(ObjaviMajstoraDto());

        // Assert
        ProvjeriPreusmjerenjeNaIndex(rezultat);
        Assert.AreEqual("Oglas je uspješno kreiran.", _kontroler.TempData["Success"]);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObjaviOglas_Post_ServisBaciIzuzetak_VracaFormuSaGreskom()
    {
        // Arrange
        var dto = ObjaviMajstoraDto();
        _fasada.Setup(f => f.ObjaviOglas(dto, VlasnikId))
            .ThrowsAsync(new InvalidOperationException("Možete imati maksimalno 3 aktivna oglasa usluge istovremeno."));

        // Act
        var rezultat = await _kontroler.ObjaviOglas(dto);

        // Assert
        ProvjeriPogledSaModelom(rezultat, dto);
        Assert.IsFalse(_kontroler.ModelState.IsValid);
    }

    // ---------- UrediOglas (GET) ----------

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_Get_NepostojeciOglas_VracaNotFound()
    {
        // Act
        var rezultat = await _kontroler.UrediOglas(99);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_Get_TudjiOglas_VracaNaDetaljeSaGreskom()
    {
        // Arrange
        PostojiOglas(OglasMajstora(1, vlasnikId: DrugiKorisnikId));

        // Act
        var rezultat = await _kontroler.UrediOglas(1);

        // Assert
        ProvjeriPreusmjerenjeNaDetalje(rezultat, 1);
        Assert.AreEqual("Nemate dozvolu za uređivanje ovog oglasa.", _kontroler.TempData["Error"]);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_Get_NeaktivanOglas_VracaNaDetaljeSaGreskom()
    {
        // Arrange
        PostojiOglas(OglasMajstora(1, status: Status.Neaktivan));

        // Act
        var rezultat = await _kontroler.UrediOglas(1);

        // Assert
        ProvjeriPreusmjerenjeNaDetalje(rezultat, 1);
        Assert.AreEqual("Samo aktivni oglasi se mogu uređivati.", _kontroler.TempData["Error"]);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_Get_AdminINeaktivanTudjiOglas_PrikazujeFormu()
    {
        // Arrange
        PostojiOglas(OglasMajstora(1, vlasnikId: DrugiKorisnikId, status: Status.Neaktivan));
        TestHelpers.PrijaviKorisnika(_kontroler, "admin", "Administrator");

        // Act
        var rezultat = await _kontroler.UrediOglas(1);

        // Assert
        Assert.IsInstanceOfType<ViewResult>(rezultat);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_Get_VlastitiOglas_PopunjavaFormuPodacimaOglasa()
    {
        // Arrange
        var oglas = OglasMajstora(1);
        oglas.Kategorije = new[] { new OglasKategorija { KategorijaID = 4 } };
        PostojiOglas(oglas);

        // Act
        var dto = (UrediOglasMajstoraDto)((ViewResult)await _kontroler.UrediOglas(1)).Model!;

        // Assert
        Assert.AreEqual(oglas.Naslov, dto.Naslov);
        Assert.AreEqual(oglas.MinCijena, dto.MinCijena);
        CollectionAssert.AreEqual(new[] { 4 }, dto.KategorijeID);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_Get_VlastitiOglas_PunjiListuMjesta()
    {
        // Arrange
        var mjesta = new[] { new Mjesto { MjestoID = 1, Naziv = "Sarajevo" } };
        _mjesta.Setup(m => m.DajSvaMjestaAsync()).ReturnsAsync(mjesta);
        PostojiOglas(OglasMajstora(1));

        // Act
        await _kontroler.UrediOglas(1);

        // Assert
        Assert.AreSame(mjesta, _kontroler.ViewData["Mjesta"]);
    }

    // ---------- UrediOglas (POST) ----------

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_Post_IdSeNePoklapa_VracaBadRequest()
    {
        // Act
        var rezultat = await _kontroler.UrediOglas(2, UrediMajstoraDto(1), null);

        // Assert
        Assert.IsInstanceOfType<BadRequestResult>(rezultat);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_Post_NepostojeciOglas_VracaNotFound()
    {
        // Act
        var rezultat = await _kontroler.UrediOglas(99, UrediMajstoraDto(99), null);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_Post_TudjiOglas_NeUredjujeOglas()
    {
        // Arrange
        PostojiOglas(OglasMajstora(1, vlasnikId: DrugiKorisnikId));

        // Act
        var rezultat = await _kontroler.UrediOglas(1, UrediMajstoraDto(1), null);

        // Assert
        ProvjeriPreusmjerenjeNaDetalje(rezultat, 1);
        _fasada.Verify(f => f.UrediOglas(It.IsAny<UrediOglasMajstoraDto>()), Times.Never);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_Post_NovaSlika_SpremaSlikuIPostavljaUrl()
    {
        // Arrange
        PostojiOglas(OglasMajstora(1));
        var dto = UrediMajstoraDto(1);
        _storage.Setup(s => s.SpremiPublic(It.IsAny<Stream>(), "image/png", It.IsAny<CancellationToken>()))
            .ReturnsAsync(NovaSlikaUrl);

        // Act
        await _kontroler.UrediOglas(1, dto, Fajl());

        // Assert
        Assert.AreEqual(NovaSlikaUrl, dto.Slika);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_Post_SpremanjeSlikeVratiNull_ZadrzavaStaruSliku()
    {
        // Arrange
        PostojiOglas(OglasMajstora(1));
        var dto = UrediMajstoraDto(1);

        // Act
        await _kontroler.UrediOglas(1, dto, Fajl());

        // Assert
        Assert.AreEqual("stara.png", dto.Slika);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_Post_BezSlike_ZadrzavaStaruSliku()
    {
        // Arrange
        PostojiOglas(OglasMajstora(1));
        var dto = UrediMajstoraDto(1);

        // Act
        await _kontroler.UrediOglas(1, dto, null);

        // Assert
        Assert.AreEqual("stara.png", dto.Slika);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_Post_NevalidanModel_VracaFormuBezUredjivanja()
    {
        // Arrange
        PostojiOglas(OglasMajstora(1));
        var dto = UrediMajstoraDto(1);
        _kontroler.ModelState.AddModelError("Naslov", "Naslov je obavezan.");

        // Act
        var rezultat = await _kontroler.UrediOglas(1, dto, null);

        // Assert
        ProvjeriPogledSaModelom(rezultat, dto);
        _fasada.Verify(f => f.UrediOglas(It.IsAny<UrediOglasMajstoraDto>()), Times.Never);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_Post_IspravanModel_UredjujeIPreusmjeravaNaDetalje()
    {
        // Arrange
        PostojiOglas(OglasMajstora(1));
        var dto = UrediMajstoraDto(1);

        // Act
        var rezultat = await _kontroler.UrediOglas(1, dto, null);

        // Assert
        _fasada.Verify(f => f.UrediOglas(dto), Times.Once);
        ProvjeriPreusmjerenjeNaDetalje(rezultat, 1);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_Post_ServisBaciIzuzetak_VracaFormuSaGreskom()
    {
        // Arrange
        PostojiOglas(OglasMajstora(1));
        var dto = UrediMajstoraDto(1);
        _fasada.Setup(f => f.UrediOglas(dto)).ThrowsAsync(new KeyNotFoundException());

        // Act
        var rezultat = await _kontroler.UrediOglas(1, dto, null);

        // Assert
        ProvjeriPogledSaModelom(rezultat, dto);
        Assert.IsFalse(_kontroler.ModelState.IsValid);
    }

    // ---------- ObrisiOglas ----------

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObrisiOglas_Get_NepostojeciOglas_VracaNotFound()
    {
        // Act
        var rezultat = await _kontroler.ObrisiOglas(99);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObrisiOglas_Get_PostojeciOglas_PrikazujePotvrdu()
    {
        // Arrange
        var oglas = OglasMajstora(1);
        PostojiOglas(oglas);

        // Act
        var rezultat = await _kontroler.ObrisiOglas(1);

        // Assert
        ProvjeriPogledSaModelom(rezultat, oglas);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task PotvrdaBrisanjaOglasa_NepostojeciOglas_VracaNotFound()
    {
        // Act
        var rezultat = await _kontroler.PotvrdaBrisanjaOglasa(99);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task PotvrdaBrisanjaOglasa_TudjiOglas_NeBriseOglas()
    {
        // Arrange
        PostojiOglas(OglasMajstora(1, vlasnikId: DrugiKorisnikId));

        // Act
        await _kontroler.PotvrdaBrisanjaOglasa(1);

        // Assert
        _fasada.Verify(f => f.ObrisiOglas(It.IsAny<int>()), Times.Never);
        Assert.AreEqual("Nemate dozvolu za brisanje ovog oglasa.", _kontroler.TempData["Error"]);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    [DataRow(VlasnikId, "Majstor")]
    [DataRow("admin", "Administrator")]
    public async Task PotvrdaBrisanjaOglasa_VlasnikIliAdmin_DeaktiviraOglas(string korisnikId, string uloga)
    {
        // Arrange
        PostojiOglas(OglasMajstora(1));
        TestHelpers.PrijaviKorisnika(_kontroler, korisnikId, uloga);

        // Act
        var rezultat = await _kontroler.PotvrdaBrisanjaOglasa(1);

        // Assert
        _fasada.Verify(f => f.ObrisiOglas(1), Times.Once);
        ProvjeriPreusmjerenjeNaDetalje(rezultat, 1);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task PotvrdaBrisanjaOglasa_OglasNestaoPriBrisanju_VracaNotFound()
    {
        // Arrange
        PostojiOglas(OglasMajstora(1));
        _fasada.Setup(f => f.ObrisiOglas(1)).ThrowsAsync(new KeyNotFoundException());

        // Act
        var rezultat = await _kontroler.PotvrdaBrisanjaOglasa(1);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }
}
