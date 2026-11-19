using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Popravka.ba.Data;
using PopravkaBa.Domain.Models;
using PopravkaBa.Infrastructure.Seeders;

namespace PopravkaBa.Tests.Baza;

[TestClass]
public class DbSeederTests
{
    private ApplicationDbContext _db = null!;
    private Mock<UserManager<ApplicationUser>> _userManager = null!;
    private Mock<RoleManager<IdentityRole>> _roleManager = null!;
    private DbSeeder _seeder = null!;
    private readonly Dictionary<string, ApplicationUser> _korisnici = new();

    [TestInitialize]
    public void Postavi()
    {
        _db = TestHelpers.NovaBaza();
        _korisnici.Clear();
        _userManager = TestHelpers.MockUserManager();
        _roleManager = new Mock<RoleManager<IdentityRole>>(
            Mock.Of<IRoleStore<IdentityRole>>(), null!, null!, null!, null!);

        _userManager
            .Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((string email) => _korisnici.TryGetValue(email, out var korisnik) ? korisnik : null);
        _userManager
            .Setup(m => m.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((string ime) => _korisnici.Values.FirstOrDefault(k => k.UserName == ime));
        _userManager
            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser korisnik, string _) =>
            {
                korisnik.Id = Guid.NewGuid().ToString();
                _korisnici[korisnik.Email!] = korisnik;
                _db.Users.Add(korisnik);
                return IdentityResult.Success;
            });
        _userManager
            .Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _roleManager.Setup(r => r.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _roleManager.Setup(r => r.CreateAsync(It.IsAny<IdentityRole>())).ReturnsAsync(IdentityResult.Success);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SeedData:AdminEmail"] = "admin@test.ba",
                ["SeedData:AdminUsername"] = "admin",
                ["SeedData:AdminPassword"] = "Admin#1234"
            })
            .Build();

        _seeder = new DbSeeder(_db, _userManager.Object, _roleManager.Object, config);
    }

    [TestCleanup]
    public void Pocisti() => _db.Dispose();

    [TestMethod]
    public async Task SeedAsync_PraznaBaza_PopunjavaUlogeKategorijeIMjesta()
    {
        // Act
        await _seeder.SeedAsync();

        // Assert
        Assert.IsTrue(await _db.Kategorije.AnyAsync());
        Assert.IsTrue(await _db.Mjesta.AnyAsync());
        _roleManager.Verify(r => r.CreateAsync(It.IsAny<IdentityRole>()), Times.Exactly(4));
    }

    [TestMethod]
    public async Task SeedAsync_PraznaBaza_KreiraKorisnikeIOglase()
    {
        // Act
        await _seeder.SeedAsync();

        // Assert
        Assert.IsTrue(await _db.OglasiUsluga.AnyAsync());
        Assert.IsTrue(await _db.OglasiMajstora.AnyAsync());
        Assert.IsTrue(await _db.OglasiRadnogMjesta.AnyAsync());
        _userManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.AtLeastOnce);
        _userManager.Verify(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.AtLeastOnce);
    }

    [TestMethod]
    public async Task SeedAsync_PozivDvaPut_NeDupliraPodatke()
    {
        // Arrange
        await _seeder.SeedAsync();
        var brojKategorija = await _db.Kategorije.CountAsync();
        var brojMjesta = await _db.Mjesta.CountAsync();
        var brojOglasa = await _db.OglasiUsluga.CountAsync();

        // Act
        await _seeder.SeedAsync();

        // Assert
        Assert.AreEqual(brojKategorija, await _db.Kategorije.CountAsync());
        Assert.AreEqual(brojMjesta, await _db.Mjesta.CountAsync());
        Assert.AreEqual(brojOglasa, await _db.OglasiUsluga.CountAsync());
    }

    [TestMethod]
    public async Task SeedAsync_AdminCreateFails_BacaException()
    {
        // Arrange
        _userManager
            .Setup(m => m.CreateAsync(It.Is<ApplicationUser>(u => u is Administrator), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Admin fail" }));

        // Act
        var izuzetak = await Assert.ThrowsExactlyAsync<Exception>(() => _seeder.SeedAsync());

        // Assert
        StringAssert.Contains(izuzetak.Message, "Admin seeding failed");
    }

    [TestMethod]
    public async Task SeedAsync_MajstoriIFirmeSeNeKreiraju_NeDodajeNjihoveOglase()
    {
        // Arrange
        _userManager
            .Setup(m => m.CreateAsync(
                It.Is<ApplicationUser>(u => u is Majstor || u is Firma), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "fail" }));

        // Act
        await _seeder.SeedAsync();

        // Assert
        Assert.AreEqual(0, await _db.Users.OfType<Majstor>().CountAsync());
        Assert.AreEqual(0, await _db.Users.OfType<Firma>().CountAsync());
        Assert.IsTrue(await _db.OglasiUsluga.AnyAsync());
    }

    [TestMethod]
    public async Task SeedAsync_KlijentiSeNeKreiraju_PreskaceNjihoveLokacijeIOglase()
    {
        // Arrange
        _korisnici["nermin.alih@email.ba"] = new Klijent
        {
            Id = "postojeci-klijent",
            UserName = "nerminalih",
            Email = "nermin.alih@email.ba"
        };
        _userManager
            .Setup(m => m.CreateAsync(It.Is<ApplicationUser>(u => u is Klijent), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "fail" }));

        // Act
        await _seeder.SeedAsync();

        // Assert
        Assert.IsTrue(await _db.OglasiUsluga.AnyAsync());
        Assert.IsTrue(await _db.KorisnikMjesto.AnyAsync(km => km.KorisnikID == "postojeci-klijent"));
    }
}
