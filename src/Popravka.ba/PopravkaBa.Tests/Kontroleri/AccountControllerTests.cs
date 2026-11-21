using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;
using Microsoft.Extensions.Logging;
using PopravkaBa.Application.DTOs;
using PopravkaBa.Application.Services.Interface;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Models;
using PopravkaBa.Web.Controllers;
using PopravkaBa.Web.Models.Enums;
using PopravkaBa.Web.Models.ViewModels;
using static PopravkaBa.Tests.Account.AccountControllerTestHelper;

namespace PopravkaBa.Tests.Account;

[TestClass]
public class AccountControllerTests
{
    private Mock<UserManager<ApplicationUser>> _userManager = null!;
    private Mock<SignInManager<ApplicationUser>> _signInManager = null!;
    private Mock<IKategorijaService> _kategorijaService = null!;
    private Mock<IMjestoService> _mjestoService = null!;
    private Mock<IVerifikacijaEmailaService> _verifikacijaService = null!;
    private Mock<IEmailSender> _emailSender = null!;
    private Mock<IFileStorage> _storage = null!;
    private AccountController _kontroler = null!;

    [TestInitialize]
    public void Postavi()
    {
        _userManager = TestHelpers.MockUserManager();
        _signInManager = new Mock<SignInManager<ApplicationUser>>(
            _userManager.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
            null!, null!, null!, null!);
        _kategorijaService = new Mock<IKategorijaService>();
        _mjestoService = new Mock<IMjestoService>();
        _verifikacijaService = new Mock<IVerifikacijaEmailaService>();
        _emailSender = new Mock<IEmailSender>();
        _storage = new Mock<IFileStorage>();

        _mjestoService.Setup(m => m.DajSvaMjestaAsync()).ReturnsAsync(new List<Mjesto> { new() { MjestoID = 1, Naziv = "Sarajevo" } });
        _kategorijaService.Setup(k => k.DajSveKategorije()).ReturnsAsync(new List<Kategorija> { new() { ID = 1, Naziv = "Vodoinstalater" } });
        _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        _storage.Setup(s => s.SpremiPublic(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://cdn.popravka.ba/slike/logo.png");

        _kontroler = new AccountController(
            _userManager.Object,
            _signInManager.Object,
            _kategorijaService.Object,
            _mjestoService.Object,
            _verifikacijaService.Object,
            _emailSender.Object,
            Mock.Of<IWebHostEnvironment>(),
            Mock.Of<ILogger<AccountController>>(),
            _storage.Object);
        TestHelpers.NeprijavljenKorisnik(_kontroler);
    }

    // ---------- Pomoćne metode (priprema podataka) ----------


    // ---------- Login (GET) ----------

    [TestMethod]
    public async Task Login_Get_VracaViewRegistracijaSaTabomPrijava()
    {
        // Act
        var rezultat = await _kontroler.Login("/oglasi");

        // Assert
        var view = Assert.IsInstanceOfType<ViewResult>(rezultat);
        var model = Assert.IsInstanceOfType<RegistracijaViewModel>(view.Model);
        Assert.AreEqual("Registracija", view.ViewName);
        Assert.AreEqual(AuthTab.Prijava, model.ActiveTab);
        Assert.AreEqual("/oglasi", view.ViewData["ReturnUrl"]);
    }

    // ---------- Login (POST) ----------

    [TestMethod]
    public async Task Login_NevalidanModel_VracaViewSaReturnUrlom()
    {
        // Arrange
        _kontroler.ModelState.AddModelError("Login.Lozinka", "Lozinka je obavezna.");

        // Act
        var rezultat = await _kontroler.Login(PrijavaVm(), "/oglasi");

        // Assert
        var view = Assert.IsInstanceOfType<ViewResult>(rezultat);
        Assert.AreEqual("Registracija", view.ViewName);
        Assert.AreEqual("/oglasi", view.ViewData["ReturnUrl"]);
    }

    public static IEnumerable<object[]> UnosiZaPrijavu =>
        DataDrivenInputParser.CitajXml<string, int, int>("TestData/xml/AccountController_prijavaUnos.xml");

    [TestMethod]
    [DynamicData(nameof(UnosiZaPrijavu))]
    public async Task Login_UnosIzXml_KorisnikaTraziPoEmailuIliKorisnickomImenu(string unos, int poEmailu, int poImenu)
    {
        // Arrange (korisnik ne postoji, pa se prijava prekida poslije pretrage)

        // Act
        await _kontroler.Login(PrijavaVm(unos));

        // Assert
        _userManager.Verify(m => m.FindByEmailAsync(unos), Times.Exactly(poEmailu));
        _userManager.Verify(m => m.FindByNameAsync(unos), Times.Exactly(poImenu));
    }

    [TestMethod]
    public async Task Login_NepostojeciKorisnik_VracaViewSaGreskomIBezPokusajaPrijave()
    {
        // Arrange
        _userManager.Setup(m => m.FindByNameAsync("haso")).ReturnsAsync((ApplicationUser?)null);

        // Act
        var rezultat = await _kontroler.Login(PrijavaVm("haso"));

        // Assert
        Assert.IsInstanceOfType<ViewResult>(rezultat);
        Assert.AreEqual(1, _kontroler.ModelState.ErrorCount);
        _signInManager.Verify(s => s.PasswordSignInAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public async Task Login_PogresnaLozinka_VracaViewSaPorukomOPogresnimPodacima()
    {
        // Arrange
        _userManager.Setup(m => m.FindByNameAsync("haso")).ReturnsAsync(new Klijent { UserName = "haso" });
        _signInManager.Setup(s => s.PasswordSignInAsync("haso", "Lozinka1", false, true))
            .ReturnsAsync(SignInResult.Failed);

        // Act
        var rezultat = await _kontroler.Login(PrijavaVm("haso", "Lozinka1"));

        // Assert
        Assert.IsInstanceOfType<ViewResult>(rezultat);
        Assert.AreEqual("Pogrešni pristupni podaci", _kontroler.ModelState[string.Empty]!.Errors[0].ErrorMessage);
    }

    [TestMethod]
    public async Task Login_UspjesnaPrijavaSaLokalnimReturnUrlom_PreusmjeravaNaReturnUrl()
    {
        // Arrange
        _userManager.Setup(m => m.FindByNameAsync("haso")).ReturnsAsync(new Klijent { UserName = "haso" });
        _signInManager.Setup(s => s.PasswordSignInAsync("haso", "Lozinka1", false, true))
            .ReturnsAsync(SignInResult.Success);
        var url = new Mock<IUrlHelper>();
        url.Setup(u => u.IsLocalUrl("/oglasi")).Returns(true);
        _kontroler.Url = url.Object;

        // Act
        var rezultat = await _kontroler.Login(PrijavaVm("haso", "Lozinka1"), "/oglasi");

        // Assert
        var redirect = Assert.IsInstanceOfType<RedirectResult>(rezultat);
        Assert.AreEqual("/oglasi", redirect.Url);
    }

    [TestMethod]
    public async Task Login_UspjesnaPrijavaSaVanjskimReturnUrlom_PreusmjeravaNaPocetnu()
    {
        // Arrange
        _userManager.Setup(m => m.FindByNameAsync("haso")).ReturnsAsync(new Klijent { UserName = "haso" });
        _signInManager.Setup(s => s.PasswordSignInAsync("haso", "Lozinka1", false, true))
            .ReturnsAsync(SignInResult.Success);

        // Act
        var rezultat = await _kontroler.Login(PrijavaVm("haso", "Lozinka1"), "https://zlonamjerni-sajt.com");

        // Assert
        var redirect = Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
        Assert.AreEqual("Index", redirect.ActionName);
        Assert.AreEqual("Home", redirect.ControllerName);
    }

    [TestMethod]
    public async Task Login_UspjesnaPrijavaBezReturnUrla_PreusmjeravaNaPocetnu()
    {
        // Arrange
        _userManager.Setup(m => m.FindByEmailAsync("haso@popravka.ba")).ReturnsAsync(new Klijent { UserName = "haso" });
        _signInManager.Setup(s => s.PasswordSignInAsync("haso", "Lozinka1", false, true))
            .ReturnsAsync(SignInResult.Success);

        // Act
        var rezultat = await _kontroler.Login(PrijavaVm("haso@popravka.ba", "Lozinka1"));

        // Assert
        var redirect = Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
        Assert.AreEqual("Home", redirect.ControllerName);
    }

    // ---------- Logout ----------

    [TestMethod]
    public async Task Logout_PrijavljenKorisnik_OdjavljujeKorisnika()
    {
        // Arrange
        TestHelpers.PrijaviKorisnika(_kontroler, "k1", "Klijent");

        // Act
        await _kontroler.Logout();

        // Assert
        _signInManager.Verify(s => s.SignOutAsync(), Times.Once);
    }

    [TestMethod]
    public async Task Logout_PrijavljenKorisnik_PreusmjeravaNaPocetnu()
    {
        // Arrange
        TestHelpers.PrijaviKorisnika(_kontroler, "k1", "Klijent");

        // Act
        var rezultat = await _kontroler.Logout();

        // Assert
        var redirect = Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
        Assert.AreEqual("Home", redirect.ControllerName);
    }

    // ---------- Registracija (GET) ----------

    [TestMethod]
    public async Task Registracija_Get_VracaViewSaTabomRegistracijaIPodacima()
    {
        // Act
        var rezultat = await _kontroler.Registracija();

        // Assert
        var view = Assert.IsInstanceOfType<ViewResult>(rezultat);
        var model = Assert.IsInstanceOfType<RegistracijaViewModel>(view.Model);
        Assert.AreEqual(AuthTab.Registracija, model.ActiveTab);
        Assert.AreEqual(1, model.Mjesta.Count());
        Assert.AreEqual(1, model.Kategorije.Count());
    }

    // ---------- RegistracijaKlijenta ----------

    [TestMethod]
    public async Task RegistracijaKlijenta_NevalidanModel_VracaViewBezLozinkeUModelu()
    {
        // Arrange
        _kontroler.ModelState.AddModelError("Email", "Unesite validnu email adresu.");
        var dto = KlijentDto();

        // Act
        var rezultat = await _kontroler.RegistracijaKlijenta(dto);

        // Assert
        var view = Assert.IsInstanceOfType<ViewResult>(rezultat);
        var model = Assert.IsInstanceOfType<RegistracijaViewModel>(view.Model);
        Assert.AreEqual("Registracija", view.ViewName);
        Assert.IsNull(model.KlijentDTO!.Lozinka);
        Assert.IsNull(model.KlijentDTO.PotvrdaLozinke);
    }

    [TestMethod]
    public async Task RegistracijaKlijenta_KreiranjeNeuspjesno_VracaViewSaGreskamaIdentityja()
    {
        // Arrange
        _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(Greske("Korisničko ime je zauzeto.", "Lozinka je preslaba."));

        // Act
        var rezultat = await _kontroler.RegistracijaKlijenta(KlijentDto());

        // Assert
        Assert.IsInstanceOfType<ViewResult>(rezultat);
        Assert.AreEqual(2, _kontroler.ModelState.ErrorCount);
        _signInManager.Verify(s => s.SignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task RegistracijaKlijenta_IspravniPodaci_DodjeljujeUlogu()
    {
        // Act
        await _kontroler.RegistracijaKlijenta(KlijentDto());

        // Assert
        _userManager.Verify(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Klijent"), Times.Once);
    }

    [TestMethod]
    public async Task RegistracijaKlijenta_IspravniPodaci_DodajeMjestaKorisnikuIPrijavljujeGa()
    {
        // Arrange
        var dto = KlijentDto();

        // Act
        await _kontroler.RegistracijaKlijenta(dto);

        // Assert
        _mjestoService.Verify(m => m.DodajMjestaKorisniku(It.IsAny<string>(), dto.MjestaID), Times.Once);
        _signInManager.Verify(s => s.SignInAsync(It.IsAny<ApplicationUser>(), false, It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task RegistracijaKlijenta_TokenGenerisan_SaljeVerifikacijskiEmailIPreusmjeravaNaPoruku()
    {
        // Arrange
        _verifikacijaService.Setup(v => v.GenerisiTokenAsync(It.IsAny<ApplicationUser>(), TipTokena.PotvrdaEmaila))
            .ReturnsAsync("sirovi-token");

        // Act
        var rezultat = await _kontroler.RegistracijaKlijenta(KlijentDto());

        // Assert
        _emailSender.Verify(e => e.PosaljiEmailAsync("haso@popravka.ba", "Potvrdite email — Popravka.ba", It.IsAny<string>()), Times.Once);
        var redirect = Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
        Assert.AreEqual("VerifikacijaPoslana", redirect.ActionName);
    }

    [TestMethod]
    public async Task RegistracijaKlijenta_TokenNijeGenerisan_NeSaljeEmail()
    {
        // Arrange
        _verifikacijaService.Setup(v => v.GenerisiTokenAsync(It.IsAny<ApplicationUser>(), TipTokena.PotvrdaEmaila))
            .ReturnsAsync((string?)null);

        // Act
        await _kontroler.RegistracijaKlijenta(KlijentDto());

        // Assert
        _emailSender.Verify(e => e.PosaljiEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // ---------- RegistracijaMajstora ----------

    [TestMethod]
    public async Task RegistracijaMajstora_NevalidanModel_VracaViewBezLozinkeUModelu()
    {
        // Arrange
        _kontroler.ModelState.AddModelError("Email", "Unesite validnu email adresu.");

        // Act
        var rezultat = await _kontroler.RegistracijaMajstora(MajstorDto());

        // Assert
        var view = Assert.IsInstanceOfType<ViewResult>(rezultat);
        var model = Assert.IsInstanceOfType<RegistracijaViewModel>(view.Model);
        Assert.IsNull(model.MajstorDTO!.Lozinka);
    }

    [TestMethod]
    public async Task RegistracijaMajstora_KreiranjeNeuspjesno_VracaViewSaGreskamaIdentityja()
    {
        // Arrange
        _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(Greske("Korisničko ime je zauzeto."));

        // Act
        var rezultat = await _kontroler.RegistracijaMajstora(MajstorDto());

        // Assert
        Assert.IsInstanceOfType<ViewResult>(rezultat);
        Assert.AreEqual(1, _kontroler.ModelState.ErrorCount);
    }

    [TestMethod]
    public async Task RegistracijaMajstora_IspravniPodaci_DodjeljujeUloguKategorijeIMjesta()
    {
        // Arrange
        var dto = MajstorDto();

        // Act
        await _kontroler.RegistracijaMajstora(dto);

        // Assert
        _userManager.Verify(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Majstor"), Times.Once);
        _kategorijaService.Verify(k => k.DodajKategorijeIzvrsiocu(It.IsAny<string>(), dto.KategorijeID), Times.Once);
        _mjestoService.Verify(m => m.DodajMjestaKorisniku(It.IsAny<string>(), dto.MjestaID), Times.Once);
    }

    [TestMethod]
    public async Task RegistracijaMajstora_TokenGenerisan_SaljeVerifikacijskiEmail()
    {
        // Arrange
        _verifikacijaService.Setup(v => v.GenerisiTokenAsync(It.IsAny<ApplicationUser>(), TipTokena.PotvrdaEmaila))
            .ReturnsAsync("sirovi-token");

        // Act
        var rezultat = await _kontroler.RegistracijaMajstora(MajstorDto());

        // Assert
        _emailSender.Verify(e => e.PosaljiEmailAsync("mujo@popravka.ba", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        var redirect = Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
        Assert.AreEqual("VerifikacijaPoslana", redirect.ActionName);
    }

    [TestMethod]
    public async Task RegistracijaMajstora_TokenNijeGenerisan_NeSaljeEmail()
    {
        // Arrange
        _verifikacijaService.Setup(v => v.GenerisiTokenAsync(It.IsAny<ApplicationUser>(), TipTokena.PotvrdaEmaila))
            .ReturnsAsync((string?)null);

        // Act
        await _kontroler.RegistracijaMajstora(MajstorDto());

        // Assert
        _emailSender.Verify(e => e.PosaljiEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // ---------- RegistracijaFirme ----------

    [TestMethod]
    public async Task RegistracijaFirme_NevalidanModel_VracaViewBezLozinkeUModelu()
    {
        // Arrange
        _kontroler.ModelState.AddModelError("Email", "Unesite validnu email adresu.");

        // Act
        var rezultat = await _kontroler.RegistracijaFirme(FirmaDto(), null);

        // Assert
        var view = Assert.IsInstanceOfType<ViewResult>(rezultat);
        var model = Assert.IsInstanceOfType<RegistracijaViewModel>(view.Model);
        Assert.IsNull(model.FirmaDTO!.Lozinka);
    }

    public static IEnumerable<object[]> Logotipi =>
        DataDrivenInputParser.CitajXml<string, long, bool>("TestData/xml/AccountController_logotipi.xml");

    [TestMethod]
    [DynamicData(nameof(Logotipi))]
    public async Task RegistracijaFirme_LogotipIzXml_PrihvataSamoDozvoljenFormatIVelicinu(
        string imeFajla, long velicina, bool prihvacen)
    {
        // Arrange
        var logo = Logo(imeFajla, velicina);

        // Act
        var rezultat = await _kontroler.RegistracijaFirme(FirmaDto(), logo.Object);

        // Assert
        Assert.AreEqual(prihvacen, rezultat is RedirectToActionResult);
    }

    [TestMethod]
    public async Task RegistracijaFirme_NedozvoljenFormatLogotipa_NeKreiraKorisnika()
    {
        // Arrange
        var logo = Logo("logo.gif", 1024);

        // Act
        await _kontroler.RegistracijaFirme(FirmaDto(), logo.Object);

        // Assert
        _userManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        Assert.AreEqual(1, _kontroler.ModelState.ErrorCount);
    }

    [TestMethod]
    public async Task RegistracijaFirme_ValidanLogotip_SpremaLogotipNaStorage()
    {
        // Arrange
        var logo = Logo("logo.png", 1024);

        // Act
        await _kontroler.RegistracijaFirme(FirmaDto(), logo.Object);

        // Assert
        _storage.Verify(s => s.SpremiPublic(It.IsAny<Stream>(), "image/png", It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task RegistracijaFirme_BezLogotipa_NeSpremaNistaNaStorage()
    {
        // Act
        var rezultat = await _kontroler.RegistracijaFirme(FirmaDto(), null);

        // Assert
        _storage.Verify(s => s.SpremiPublic(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
    }

    [TestMethod]
    public async Task RegistracijaFirme_PrazanLogotip_PreskaceValidacijuISpremanje()
    {
        // Arrange
        var logo = Logo("logo.gif", 0);

        // Act
        var rezultat = await _kontroler.RegistracijaFirme(FirmaDto(), logo.Object);

        // Assert
        _storage.Verify(s => s.SpremiPublic(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
    }

    [TestMethod]
    public async Task RegistracijaFirme_KreiranjeNeuspjesno_VracaViewSaGreskamaIdentityja()
    {
        // Arrange
        _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(Greske("Korisničko ime je zauzeto."));

        // Act
        var rezultat = await _kontroler.RegistracijaFirme(FirmaDto(), null);

        // Assert
        Assert.IsInstanceOfType<ViewResult>(rezultat);
        Assert.AreEqual(1, _kontroler.ModelState.ErrorCount);
    }

    [TestMethod]
    public async Task RegistracijaFirme_IspravniPodaci_DodjeljujeUloguKategorijeIMjesta()
    {
        // Arrange
        var dto = FirmaDto();

        // Act
        await _kontroler.RegistracijaFirme(dto, null);

        // Assert
        _userManager.Verify(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Firma"), Times.Once);
        _kategorijaService.Verify(k => k.DodajKategorijeIzvrsiocu(It.IsAny<string>(), dto.KategorijeID), Times.Once);
        _mjestoService.Verify(m => m.DodajMjestaKorisniku(It.IsAny<string>(), dto.MjestaID), Times.Once);
    }

    [TestMethod]
    public async Task RegistracijaFirme_TokenGenerisan_SaljeVerifikacijskiEmail()
    {
        // Arrange
        _verifikacijaService.Setup(v => v.GenerisiTokenAsync(It.IsAny<ApplicationUser>(), TipTokena.PotvrdaEmaila))
            .ReturnsAsync("sirovi-token");

        // Act
        await _kontroler.RegistracijaFirme(FirmaDto(), null);

        // Assert
        _emailSender.Verify(e => e.PosaljiEmailAsync("firma@popravka.ba", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task RegistracijaFirme_TokenNijeGenerisan_NeSaljeEmail()
    {
        // Arrange
        _verifikacijaService.Setup(v => v.GenerisiTokenAsync(It.IsAny<ApplicationUser>(), TipTokena.PotvrdaEmaila))
            .ReturnsAsync((string?)null);

        // Act
        await _kontroler.RegistracijaFirme(FirmaDto(), null);

        // Assert
        _emailSender.Verify(e => e.PosaljiEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // ---------- ZaboravljenaLozinka ----------

    [TestMethod]
    public async Task ZaboravljenaLozinka_Get_VracaView()
    {
        // Act
        var rezultat = await _kontroler.ZaboravljenaLozinka();

        // Assert
        Assert.IsInstanceOfType<ViewResult>(rezultat);
    }

    public static IEnumerable<object[]> NeispravniEmailovi =>
        DataDrivenInputParser.CitajXml<string>("TestData/xml/AccountController_neispravniEmailovi.xml");

    [TestMethod]
    [DynamicData(nameof(NeispravniEmailovi))]
    public async Task ZaboravljenaLozinka_NeispravanEmailIzXml_VracaViewSaGreskomBezTrazenjaKorisnika(string? email)
    {
        // Act
        var rezultat = await _kontroler.ZaboravljenaLozinka(email!);

        // Assert
        var view = Assert.IsInstanceOfType<ViewResult>(rezultat);
        Assert.IsNull(view.ViewName);
        Assert.AreEqual(1, _kontroler.ModelState.ErrorCount);
        _userManager.Verify(m => m.FindByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task ZaboravljenaLozinka_NepostojeciKorisnik_VracaPotvrduBezGenerisanjaTokena()
    {
        // Arrange
        _userManager.Setup(m => m.FindByEmailAsync("niko@popravka.ba")).ReturnsAsync((ApplicationUser?)null);

        // Act
        var rezultat = await _kontroler.ZaboravljenaLozinka("niko@popravka.ba");

        // Assert
        var view = Assert.IsInstanceOfType<ViewResult>(rezultat);
        Assert.AreEqual("ZaboravljenaLozinkaPotvrda", view.ViewName);
        _verifikacijaService.Verify(v => v.GenerisiTokenAsync(It.IsAny<ApplicationUser>(), It.IsAny<TipTokena>()), Times.Never);
    }

    [TestMethod]
    public async Task ZaboravljenaLozinka_PostojeciKorisnik_SaljeEmailZaResetLozinke()
    {
        // Arrange
        var korisnik = new Klijent { Id = "k1", Email = "haso@popravka.ba" };
        _userManager.Setup(m => m.FindByEmailAsync("haso@popravka.ba")).ReturnsAsync(korisnik);
        _verifikacijaService.Setup(v => v.GenerisiTokenAsync(korisnik, TipTokena.ResetLozinke)).ReturnsAsync("sirovi-token");

        // Act
        var rezultat = await _kontroler.ZaboravljenaLozinka("haso@popravka.ba");

        // Assert
        _emailSender.Verify(e => e.PosaljiEmailAsync(
            "haso@popravka.ba", "[Popravka.ba] Zahtjev za reset lozinke", It.IsAny<string>()), Times.Once);
        var view = Assert.IsInstanceOfType<ViewResult>(rezultat);
        Assert.AreEqual("ZaboravljenaLozinkaPotvrda", view.ViewName);
    }

    [TestMethod]
    public async Task ZaboravljenaLozinka_PostojeciKorisnik_PamtiEmailUTempData()
    {
        // Arrange
        var korisnik = new Klijent { Id = "k1", Email = "haso@popravka.ba" };
        _userManager.Setup(m => m.FindByEmailAsync("haso@popravka.ba")).ReturnsAsync(korisnik);
        _verifikacijaService.Setup(v => v.GenerisiTokenAsync(korisnik, TipTokena.ResetLozinke)).ReturnsAsync("sirovi-token");

        // Act
        await _kontroler.ZaboravljenaLozinka("haso@popravka.ba");

        // Assert
        Assert.AreEqual("haso@popravka.ba", _kontroler.TempData["ResetEmail"]);
    }

    [TestMethod]
    public async Task ZaboravljenaLozinka_TokenNijeGenerisan_NeSaljeEmail()
    {
        // Arrange
        var korisnik = new Klijent { Id = "k1", Email = "haso@popravka.ba" };
        _userManager.Setup(m => m.FindByEmailAsync("haso@popravka.ba")).ReturnsAsync(korisnik);
        _verifikacijaService.Setup(v => v.GenerisiTokenAsync(korisnik, TipTokena.ResetLozinke)).ReturnsAsync((string?)null);

        // Act
        await _kontroler.ZaboravljenaLozinka("haso@popravka.ba");

        // Assert
        _emailSender.Verify(e => e.PosaljiEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // ---------- ResetLozinke (GET) ----------

    [TestMethod]
    public async Task ResetLozinke_Get_PrazanToken_VracaNotFound()
    {
        // Act
        var rezultat = await _kontroler.ResetLozinke((string?)"");

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    public async Task ResetLozinke_Get_NullToken_VracaNotFound()
    {
        // Act
        var rezultat = await _kontroler.ResetLozinke((string?)null);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    public static IEnumerable<object[]> StatusiResetTokena =>
        DataDrivenInputParser.CitajXml<Status, bool>("TestData/xml/AccountController_statusResetTokena.xml");

    [TestMethod]
    [DynamicData(nameof(StatusiResetTokena))]
    public async Task ResetLozinke_Get_StatusTokenaIzXml_OtvaraFormuSamoZaAktivanToken(Status status, bool preusmjerenje)
    {
        // Arrange
        _verifikacijaService.Setup(v => v.ValidirajResetTokenAsync("tok")).ReturnsAsync(Validacija(status));

        // Act
        var rezultat = await _kontroler.ResetLozinke("tok");

        // Assert
        Assert.AreEqual(preusmjerenje, rezultat is RedirectToActionResult);
    }

    [TestMethod]
    public async Task ResetLozinke_Get_NevalidanToken_PreusmjeravaNaStranicuSaKodom403()
    {
        // Arrange
        _verifikacijaService.Setup(v => v.ValidirajResetTokenAsync("tok")).ReturnsAsync(Validacija(Status.Odbijeno));

        // Act
        var rezultat = await _kontroler.ResetLozinke("tok");

        // Assert
        var redirect = Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
        Assert.AreEqual("StatusCode", redirect.ActionName);
        Assert.AreEqual("Home", redirect.ControllerName);
        Assert.AreEqual(403, redirect.RouteValues!["code"]);
    }

    [TestMethod]
    public async Task ResetLozinke_Get_AktivanToken_VracaViewSaTokenomUModelu()
    {
        // Arrange
        _verifikacijaService.Setup(v => v.ValidirajResetTokenAsync("tok")).ReturnsAsync(Validacija(Status.Aktivan));

        // Act
        var rezultat = await _kontroler.ResetLozinke("tok");

        // Assert
        var view = Assert.IsInstanceOfType<ViewResult>(rezultat);
        var model = Assert.IsInstanceOfType<ResetLozinkeViewModel>(view.Model);
        Assert.AreEqual("tok", model.Token);
    }

    // ---------- ResetLozinke (POST) ----------

    [TestMethod]
    public async Task ResetLozinke_Post_NevalidanModel_VracaIstiViewModel()
    {
        // Arrange
        var vm = ResetVm();
        _kontroler.ModelState.AddModelError("PotvrdaLozinke", "Lozinke se ne podudaraju.");

        // Act
        var rezultat = await _kontroler.ResetLozinke(vm);

        // Assert
        var view = Assert.IsInstanceOfType<ViewResult>(rezultat);
        Assert.AreSame(vm, view.Model);
    }

    [TestMethod]
    public async Task ResetLozinke_Post_TokenNijeAktivan_VracaErrorView()
    {
        // Arrange
        _verifikacijaService.Setup(v => v.ValidirajResetTokenAsync("tok")).ReturnsAsync(Validacija(Status.Neaktivan));

        // Act
        var rezultat = await _kontroler.ResetLozinke(ResetVm());

        // Assert
        var view = Assert.IsInstanceOfType<ViewResult>(rezultat);
        Assert.AreEqual("Error", view.ViewName);
    }

    [TestMethod]
    public async Task ResetLozinke_Post_ResetNeuspjesan_VracaViewSaGreskamaIToken_NijeOznacenKoristenim()
    {
        // Arrange
        var token = new VerifikacijskiToken { ID = 1 };
        var korisnik = new Klijent { Id = "k1" };
        _verifikacijaService.Setup(v => v.ValidirajResetTokenAsync("tok")).ReturnsAsync(Validacija(Status.Aktivan, token, korisnik));
        _userManager.Setup(m => m.GeneratePasswordResetTokenAsync(korisnik)).ReturnsAsync("identity-token");
        _userManager.Setup(m => m.ResetPasswordAsync(korisnik, "identity-token", "NovaLozinka1"))
            .ReturnsAsync(Greske("Lozinka je preslaba."));

        // Act
        var rezultat = await _kontroler.ResetLozinke(ResetVm());

        // Assert
        Assert.IsInstanceOfType<ViewResult>(rezultat);
        Assert.AreEqual(1, _kontroler.ModelState.ErrorCount);
        _verifikacijaService.Verify(v => v.OznaciKoristenimAsync(It.IsAny<VerifikacijskiToken>()), Times.Never);
    }

    [TestMethod]
    public async Task ResetLozinke_Post_ResetUspjesan_OznacavaTokenKoristenimIVracaPotvrdu()
    {
        // Arrange
        var token = new VerifikacijskiToken { ID = 1 };
        var korisnik = new Klijent { Id = "k1" };
        _verifikacijaService.Setup(v => v.ValidirajResetTokenAsync("tok")).ReturnsAsync(Validacija(Status.Aktivan, token, korisnik));
        _userManager.Setup(m => m.GeneratePasswordResetTokenAsync(korisnik)).ReturnsAsync("identity-token");
        _userManager.Setup(m => m.ResetPasswordAsync(korisnik, "identity-token", "NovaLozinka1"))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var rezultat = await _kontroler.ResetLozinke(ResetVm());

        // Assert
        _verifikacijaService.Verify(v => v.OznaciKoristenimAsync(token), Times.Once);
        var view = Assert.IsInstanceOfType<ViewResult>(rezultat);
        Assert.AreEqual("ResetLozinkePotvrda", view.ViewName);
    }

    // ---------- PotvrdiEmail ----------

    [TestMethod]
    public async Task PotvrdiEmail_BezTokena_VracaOdbijenoBezPozivaServisa()
    {
        // Act
        var rezultat = await _kontroler.PotvrdiEmail(null);

        // Assert
        var view = Assert.IsInstanceOfType<ViewResult>(rezultat);
        Assert.AreEqual(Status.Odbijeno, view.Model);
        _verifikacijaService.Verify(v => v.PotvrdiAsync(It.IsAny<string>(), It.IsAny<TipTokena>()), Times.Never);
    }

    [TestMethod]
    public async Task PotvrdiEmail_ValidanToken_VracaStatusIzServisa()
    {
        // Arrange
        _verifikacijaService.Setup(v => v.PotvrdiAsync("tok", TipTokena.PotvrdaEmaila)).ReturnsAsync(Status.Aktivan);

        // Act
        var rezultat = await _kontroler.PotvrdiEmail("tok");

        // Assert
        var view = Assert.IsInstanceOfType<ViewResult>(rezultat);
        Assert.AreEqual(Status.Aktivan, view.Model);
    }

    // ---------- VerifikacijaPoslana ----------

    [TestMethod]
    public async Task VerifikacijaPoslana_NemaPrijavljenogKorisnika_PreusmjeravaNaLogin()
    {
        // Arrange
        _userManager.PrijavljenKorisnik(null);

        // Act
        var rezultat = await _kontroler.VerifikacijaPoslana();

        // Assert
        var redirect = Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
        Assert.AreEqual("Login", redirect.ActionName);
    }

    [TestMethod]
    public async Task VerifikacijaPoslana_VecAktivanKorisnik_PreusmjeravaNaPocetnu()
    {
        // Arrange
        _userManager.PrijavljenKorisnik(AktivanKorisnik());

        // Act
        var rezultat = await _kontroler.VerifikacijaPoslana();

        // Assert
        var redirect = Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
        Assert.AreEqual("Home", redirect.ControllerName);
    }

    [TestMethod]
    public async Task VerifikacijaPoslana_NeaktivanKorisnik_VracaViewSaEmailomUViewData()
    {
        // Arrange
        _userManager.PrijavljenKorisnik(new Klijent { Id = "k1", Email = "haso@popravka.ba" });

        // Act
        var rezultat = await _kontroler.VerifikacijaPoslana();

        // Assert
        var view = Assert.IsInstanceOfType<ViewResult>(rezultat);
        Assert.AreEqual("haso@popravka.ba", view.ViewData["Email"]);
    }

    // ---------- PosaljiVerifikaciju ----------

    [TestMethod]
    public async Task PosaljiVerifikaciju_NemaPrijavljenogKorisnika_PreusmjeravaNaLogin()
    {
        // Arrange
        _userManager.PrijavljenKorisnik(null);

        // Act
        var rezultat = await _kontroler.PosaljiVerifikaciju();

        // Assert
        var redirect = Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
        Assert.AreEqual("Login", redirect.ActionName);
    }

    [TestMethod]
    public async Task PosaljiVerifikaciju_VecAktivanKorisnik_PreusmjeravaNaPocetnuBezSlanja()
    {
        // Arrange
        _userManager.PrijavljenKorisnik(AktivanKorisnik());

        // Act
        var rezultat = await _kontroler.PosaljiVerifikaciju();

        // Assert
        var redirect = Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
        Assert.AreEqual("Home", redirect.ControllerName);
        Assert.AreEqual("Vaš email je već potvrđen.", _kontroler.TempData["Success"]);
        _verifikacijaService.Verify(v => v.GenerisiTokenAsync(It.IsAny<ApplicationUser>(), It.IsAny<TipTokena>()), Times.Never);
    }

    [TestMethod]
    public async Task PosaljiVerifikaciju_PreRanoNoviZahtjev_PrikazujeGreskuBezSlanjaEmaila()
    {
        // Arrange
        _userManager.PrijavljenKorisnik(new Klijent { Id = "k1", Email = "haso@popravka.ba" });
        _verifikacijaService.Setup(v => v.GenerisiTokenAsync(It.IsAny<ApplicationUser>(), TipTokena.PotvrdaEmaila))
            .ReturnsAsync((string?)null);

        // Act
        var rezultat = await _kontroler.PosaljiVerifikaciju();

        // Assert
        var redirect = Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
        Assert.AreEqual("VerifikacijaPoslana", redirect.ActionName);
        Assert.AreEqual("Pričekajte par minuta prije slanja novog zahtjeva.", _kontroler.TempData["Greska"]);
        _emailSender.Verify(e => e.PosaljiEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task PosaljiVerifikaciju_TokenGenerisan_SaljeEmailKorisniku()
    {
        // Arrange
        _userManager.PrijavljenKorisnik(new Klijent { Id = "k1", Email = "haso@popravka.ba" });
        _verifikacijaService.Setup(v => v.GenerisiTokenAsync(It.IsAny<ApplicationUser>(), TipTokena.PotvrdaEmaila))
            .ReturnsAsync("sirovi-token");

        // Act
        var rezultat = await _kontroler.PosaljiVerifikaciju();

        // Assert
        _emailSender.Verify(e => e.PosaljiEmailAsync("haso@popravka.ba", "Potvrdite email — Popravka.ba", It.IsAny<string>()), Times.Once);
        Assert.AreEqual("Novi link za verifikaciju je upravo poslat na Vaš email.", _kontroler.TempData["Success"]);
        Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
    }
}