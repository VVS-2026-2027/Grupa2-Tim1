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
public class OglasUslugeControllerTests
{
    private Mock<IOglasUslugeFacade> _fasada = null!;
    private Mock<IMjestoService> _mjesta = null!;
    private Mock<UserManager<ApplicationUser>> _userManager = null!;
    private Mock<IPonudaUslugeService> _ponude = null!;
    private Mock<IRecenzijaService> _recenzije = null!;
    private Mock<IFileStorage> _storage = null!;
    private OglasUslugeController _kontroler = null!;
    private Kategorija[] _kategorije = null!;

    [TestInitialize]
    public void Postavi()
    {
        _fasada = new Mock<IOglasUslugeFacade>();
        _mjesta = new Mock<IMjestoService>();
        _userManager = TestHelpers.MockUserManager();
        _ponude = new Mock<IPonudaUslugeService>();
        _recenzije = new Mock<IRecenzijaService>();
        _storage = new Mock<IFileStorage>();
        _kategorije = new[] { new Kategorija { ID = 1, Naziv = "Soboslikarstvo" } };

        _fasada.Setup(f => f.DajSveKategorije()).ReturnsAsync(_kategorije);
        _userManager.KorisnikIzClaimova();

        _kontroler = new OglasUslugeController(_fasada.Object, _mjesta.Object, _userManager.Object, _ponude.Object,
            _recenzije.Object, NullLogger<OglasUslugeController>.Instance, _storage.Object);
        TestHelpers.PrijaviKorisnika(_kontroler, VlasnikId, "Klijent");
    }

    private void PostojiOglas(OglasUsluge oglas) =>
        _fasada.Setup(f => f.DajOglasPoId(oglas.OglasID)).ReturnsAsync(oglas);

    private async Task<OglasUslugeDetaljiViewModel> Detalji(int id) =>
        (OglasUslugeDetaljiViewModel)((ViewResult)await _kontroler.Detalji(id)).Model!;

    private static PonudaUsluge Ponuda(int id, string izvrsilacId, int? cijena = 100,
        Status status = Status.NaCekanju, IzvrsilacUsluge? izvrsilac = null) => new()
    {
        ID = id, OglasUslugeID = 1, IzvrsilacID = izvrsilacId, Izvrsilac = izvrsilac!, Cijena = cijena,
        StatusPonude = status, Poruka = "Mogu sutra",
        DatumPocetkaUsluge = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    // Oglas sa kategorijom 1, da kontroler računa prosječnu cijenu kategorije
    private static OglasUsluge OglasSaKategorijom(params PonudaUsluge[] ponude)
    {
        var oglas = OglasUsluge(1);
        oglas.Kategorije = new[] { new OglasKategorija { KategorijaID = 1, Kategorija = new Kategorija { Naziv = "Soboslikarstvo" } } };
        oglas.Ponude = ponude.ToList();
        return oglas;
    }

    // ---------- Index ----------

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Index_BezPretrage_PrikazujeSveOglase()
    {
        // Arrange
        var oglasi = new[] { OglasUsluge(1) };
        _fasada.Setup(f => f.DajSveOglase()).ReturnsAsync(oglasi);

        // Act
        var rezultat = await _kontroler.Index(null, null);

        // Assert
        ProvjeriPogledSaModelom(rezultat, oglasi);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Index_TekstPretrage_PrikazujeRezultatePretrage()
    {
        // Arrange
        var oglasi = new[] { OglasUsluge(2) };
        _fasada.Setup(f => f.PronadjiOglase("krečenje", null)).ReturnsAsync(oglasi);

        // Act
        var rezultat = await _kontroler.Index("krečenje", null);

        // Assert
        ProvjeriPogledSaModelom(rezultat, oglasi);
        Assert.AreEqual("krečenje", _kontroler.ViewData["Search"]);
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
        var oglas = OglasSaKategorijom();
        PostojiOglas(oglas);

        // Act
        var vm = await Detalji(1);

        // Assert
        Assert.AreEqual(oglas.Naslov, vm.Naslov);
        Assert.AreEqual(oglas.MinBudzet, vm.MinBudzet);
        Assert.AreEqual(oglas.MaxBudzet, vm.MaxBudzet);
        CollectionAssert.AreEqual(new[] { "Soboslikarstvo" }, vm.Kategorije);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Detalji_OglasSaPonudom_MapiraPodatkeIzvrsioca()
    {
        // Arrange
        var izvrsilac = new Majstor { Id = "m1", Ime = "Mujo", Prezime = "Mujić", ProsjecnaOcjena = 4.2m, BrojRecenzija = 6 };
        PostojiOglas(OglasSaKategorijom(Ponuda(5, "m1", izvrsilac: izvrsilac)));
        _userManager.Setup(m => m.FindByIdAsync("m1")).ReturnsAsync(new Majstor { UserName = "mujo" });

        // Act
        var ponuda = (await Detalji(1)).Ponude.Single();

        // Assert
        Assert.AreEqual(5, ponuda.PonudaId);
        Assert.AreEqual("mujo", ponuda.IzvrsilacUsername);
        Assert.AreEqual("Mujo Mujić", ponuda.IzvrsilacIme);
        Assert.AreEqual(4.2m, ponuda.ProsjecnaOcjena);
        Assert.AreEqual("Mogu sutra", ponuda.Poruka);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Detalji_PonudaBezIzvrsioca_KoristiZamjenskeVrijednosti()
    {
        // Arrange
        PostojiOglas(OglasSaKategorijom(Ponuda(5, "m1")));

        // Act
        var ponuda = (await Detalji(1)).Ponude.Single();

        // Assert
        Assert.AreEqual("m1", ponuda.IzvrsilacUsername);
        Assert.AreEqual("—", ponuda.IzvrsilacIme);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Detalji_PoznatProsjekKategorije_RacunaRazlikuCijeneOdProsjeka()
    {
        // Arrange
        PostojiOglas(OglasSaKategorijom(Ponuda(5, "m1", cijena: 130)));
        _ponude.Setup(p => p.DajProsjekCijenePoKategorijama(It.Is<IEnumerable<int>>(k => k.Single() == 1)))
            .ReturnsAsync(100m);

        // Act
        var vm = await Detalji(1);

        // Assert
        Assert.AreEqual(100m, vm.ProsjecnaCijenaPonude);
        Assert.AreEqual(30m, vm.Ponude.Single().RazlikaOdProsjeka);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Detalji_NemaProsjekaKategorije_RazlikaJeNull()
    {
        // Arrange
        PostojiOglas(OglasSaKategorijom(Ponuda(5, "m1", cijena: 130)));

        // Act
        var vm = await Detalji(1);

        // Assert
        Assert.IsNull(vm.Ponude.Single().RazlikaOdProsjeka);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Detalji_OglasBezKategorija_NeTraziProsjekCijene()
    {
        // Arrange
        PostojiOglas(OglasUsluge(1));

        // Act
        var vm = await Detalji(1);

        // Assert
        Assert.IsNull(vm.ProsjecnaCijenaPonude);
        _ponude.Verify(p => p.DajProsjekCijenePoKategorijama(It.IsAny<IEnumerable<int>>()), Times.Never);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    [DataRow("m1", "Majstor", true)]
    [DataRow("f1", "Firma", true)]
    [DataRow(DrugiKorisnikId, "Klijent", false)]
    public async Task Detalji_UlogaKorisnika_OdredjujeMozeLiPoslatiPonudu(string korisnikId, string uloga, bool moze)
    {
        // Arrange
        PostojiOglas(OglasUsluge(1));
        TestHelpers.PrijaviKorisnika(_kontroler, korisnikId, uloga);

        // Act
        var vm = await Detalji(1);

        // Assert
        Assert.AreEqual(moze, vm.MozeApplicirati);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Detalji_IzvrsilacPoslaoPonudu_OznacavaDaJeVecPoslao()
    {
        // Arrange
        PostojiOglas(OglasSaKategorijom(Ponuda(5, "m1")));
        TestHelpers.PrijaviKorisnika(_kontroler, "m1", "Majstor");

        // Act
        var vm = await Detalji(1);

        // Assert
        Assert.IsTrue(vm.VecApplicirao);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    [DataRow(true, false)]
    [DataRow(false, true)]
    public async Task Detalji_ZavrsenPosaoVlasnika_OdredjujeDaLiJeIzvrsilacVecOcijenjen(bool mozeOstaviti, bool vecOcijenjen)
    {
        // Arrange
        var oglas = OglasSaKategorijom(Ponuda(5, "m1", status: Status.Isporuceno));
        oglas.StatusOglasa = Status.Isporuceno;
        PostojiOglas(oglas);
        _recenzije.Setup(r => r.MozeOstavitiRecenziju(VlasnikId, "m1")).ReturnsAsync(mozeOstaviti);

        // Act
        var vm = await Detalji(1);

        // Assert
        Assert.AreEqual(vecOcijenjen, vm.VecOcijenjen);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Detalji_ZavrsenOglasBezPrihvacenePonude_NeProvjeravaRecenziju()
    {
        // Arrange
        var oglas = OglasSaKategorijom(Ponuda(5, "m1", status: Status.Odbijeno));
        oglas.StatusOglasa = Status.Isporuceno;
        PostojiOglas(oglas);

        // Act
        await Detalji(1);

        // Assert
        _recenzije.Verify(r => r.MozeOstavitiRecenziju(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Detalji_AktivanOglas_NeProvjeravaRecenziju()
    {
        // Arrange
        PostojiOglas(OglasSaKategorijom(Ponuda(5, "m1", status: Status.Prihvaceno)));

        // Act
        await Detalji(1);

        // Assert
        _recenzije.Verify(r => r.MozeOstavitiRecenziju(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
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
        var dto = ObjaviUslugeDto();
        _kontroler.ModelState.AddModelError("Naslov", "Naslov je obavezan.");

        // Act
        var rezultat = await _kontroler.ObjaviOglas(dto);

        // Assert
        ProvjeriPogledSaModelom(rezultat, dto);
        _fasada.Verify(f => f.ObjaviOglas(It.IsAny<ObjaviOglasUslugeDto>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObjaviOglas_Post_IspravanModel_ObjavljujeIPreusmjeravaNaListu()
    {
        // Arrange
        var dto = ObjaviUslugeDto();

        // Act
        var rezultat = await _kontroler.ObjaviOglas(dto);

        // Assert
        _fasada.Verify(f => f.ObjaviOglas(dto, VlasnikId), Times.Once);
        ProvjeriPreusmjerenjeNaIndex(rezultat);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObjaviOglas_Post_ServisBaciIzuzetak_VracaFormuSaGreskom()
    {
        // Arrange
        var dto = ObjaviUslugeDto();
        _fasada.Setup(f => f.ObjaviOglas(dto, VlasnikId)).ThrowsAsync(new Exception("Baza nije dostupna."));

        // Act
        var rezultat = await _kontroler.ObjaviOglas(dto);

        // Assert
        ProvjeriPogledSaModelom(rezultat, dto);
        Assert.IsFalse(_kontroler.ModelState.IsValid);
    }

    // ---------- UrediOglas ----------

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
        PostojiOglas(OglasUsluge(1, vlasnikId: DrugiKorisnikId));

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
        PostojiOglas(OglasUsluge(1, status: Status.Neaktivan));

        // Act
        var rezultat = await _kontroler.UrediOglas(1);

        // Assert
        ProvjeriPreusmjerenjeNaDetalje(rezultat, 1);
        Assert.AreEqual("Samo aktivni oglasi se mogu uređivati.", _kontroler.TempData["Error"]);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_Get_VlastitiOglas_PopunjavaFormuPodacimaOglasa()
    {
        // Arrange
        var oglas = OglasSaKategorijom();
        PostojiOglas(oglas);

        // Act
        var dto = (UrediOglasUslugeDto)((ViewResult)await _kontroler.UrediOglas(1)).Model!;

        // Assert
        Assert.AreEqual(oglas.Naslov, dto.Naslov);
        Assert.AreEqual(oglas.MaxBudzet, dto.MaxBudzet);
        CollectionAssert.AreEqual(new[] { 1 }, dto.KategorijeID);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_Post_IdSeNePoklapa_VracaBadRequest()
    {
        // Act
        var rezultat = await _kontroler.UrediOglas(2, UrediUslugeDto(1), null);

        // Assert
        Assert.IsInstanceOfType<BadRequestResult>(rezultat);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_Post_NepostojeciOglas_VracaNotFound()
    {
        // Act
        var rezultat = await _kontroler.UrediOglas(99, UrediUslugeDto(99), null);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_Post_TudjiOglas_NeUredjujeOglas()
    {
        // Arrange
        PostojiOglas(OglasUsluge(1, vlasnikId: DrugiKorisnikId));

        // Act
        var rezultat = await _kontroler.UrediOglas(1, UrediUslugeDto(1), null);

        // Assert
        ProvjeriPreusmjerenjeNaDetalje(rezultat, 1);
        _fasada.Verify(f => f.UrediOglas(It.IsAny<UrediOglasUslugeDto>()), Times.Never);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_Post_NovaSlika_SpremaSlikuIPostavljaUrl()
    {
        // Arrange
        PostojiOglas(OglasUsluge(1));
        var dto = UrediUslugeDto(1);
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
        PostojiOglas(OglasUsluge(1));
        var dto = UrediUslugeDto(1);

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
        PostojiOglas(OglasUsluge(1));
        var dto = UrediUslugeDto(1);

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
        PostojiOglas(OglasUsluge(1));
        var dto = UrediUslugeDto(1);
        _kontroler.ModelState.AddModelError("Naslov", "Naslov je obavezan.");

        // Act
        var rezultat = await _kontroler.UrediOglas(1, dto, null);

        // Assert
        ProvjeriPogledSaModelom(rezultat, dto);
        _fasada.Verify(f => f.UrediOglas(It.IsAny<UrediOglasUslugeDto>()), Times.Never);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_Post_IspravanModel_UredjujeIPreusmjeravaNaDetalje()
    {
        // Arrange
        PostojiOglas(OglasUsluge(1));
        var dto = UrediUslugeDto(1);

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
        PostojiOglas(OglasUsluge(1));
        var dto = UrediUslugeDto(1);
        _fasada.Setup(f => f.UrediOglas(dto)).ThrowsAsync(new Exception("Baza nije dostupna."));

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
        var oglas = OglasUsluge(1);
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
        PostojiOglas(OglasUsluge(1, vlasnikId: DrugiKorisnikId));

        // Act
        await _kontroler.PotvrdaBrisanjaOglasa(1);

        // Assert
        _fasada.Verify(f => f.ObrisiOglas(It.IsAny<int>()), Times.Never);
        Assert.AreEqual("Nemate dozvolu za brisanje ovog oglasa.", _kontroler.TempData["Error"]);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task PotvrdaBrisanjaOglasa_VlastitiOglas_DeaktiviraOglas()
    {
        // Arrange
        PostojiOglas(OglasUsluge(1));

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
        PostojiOglas(OglasUsluge(1));
        _fasada.Setup(f => f.ObrisiOglas(1)).ThrowsAsync(new KeyNotFoundException());

        // Act
        var rezultat = await _kontroler.PotvrdaBrisanjaOglasa(1);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    // ---------- ZavrsiOglas ----------

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ZavrsiOglas_UspjesnoZavrsen_OznacavaPosaoZavrsenimZaKlijenta()
    {
        // Act
        var rezultat = await _kontroler.ZavrsiOglas(1);

        // Assert
        _ponude.Verify(p => p.ZavrsiPosao(1, VlasnikId), Times.Once);
        ProvjeriPreusmjerenjeNaDetalje(rezultat, 1);
        Assert.IsNotNull(_kontroler.TempData["Success"]);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ZavrsiOglas_OglasNePostoji_VracaNotFound()
    {
        // Arrange
        _ponude.Setup(p => p.ZavrsiPosao(1, VlasnikId)).ThrowsAsync(new KeyNotFoundException());

        // Act
        var rezultat = await _kontroler.ZavrsiOglas(1);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ZavrsiOglas_KlijentNijeVlasnik_PostavljaGreskuVlasnistva()
    {
        // Arrange
        _ponude.Setup(p => p.ZavrsiPosao(1, VlasnikId)).ThrowsAsync(new UnauthorizedAccessException());

        // Act
        await _kontroler.ZavrsiOglas(1);

        // Assert
        Assert.AreEqual("Samo vlasnik oglasa može označiti posao završenim.", _kontroler.TempData["Error"]);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ZavrsiOglas_NemaPrihvacenePonude_PrikazujePorukuServisa()
    {
        // Arrange
        _ponude.Setup(p => p.ZavrsiPosao(1, VlasnikId)).ThrowsAsync(new InvalidOperationException("Oglas nema prihvaćenu ponudu."));

        // Act
        await _kontroler.ZavrsiOglas(1);

        // Assert
        Assert.AreEqual("Oglas nema prihvaćenu ponudu.", _kontroler.TempData["Error"]);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ZavrsiOglas_NeocekivanaGreska_PrikazujeOpcuPorukuINaDetalje()
    {
        // Arrange
        _ponude.Setup(p => p.ZavrsiPosao(1, VlasnikId)).ThrowsAsync(new Exception("Baza nije dostupna."));

        // Act
        var rezultat = await _kontroler.ZavrsiOglas(1);

        // Assert
        ProvjeriPreusmjerenjeNaDetalje(rezultat, 1);
        Assert.AreEqual("Došlo je do greške pri označavanju posla završenim.", _kontroler.TempData["Error"]);
    }
}
