using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using PopravkaBa.Domain.Models;

namespace PopravkaBa.Tests.Helpers;

// Gotove postavke za mock UserManager-a, koriste ih Account, Profil i VerifikacijaEmaila testovi.
// Pozivaju se kao da su metode samog mocka, npr. _userManager.VratiUloge(korisnik, "Klijent").
public static class UserManagerTestHelper
{
    // GetUserId(User) vraća ovaj id
    public static void PrijavljenId(this Mock<UserManager<ApplicationUser>> userManager, string id) =>
        userManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(id);

    // GetUserAsync(User) vraća ovog korisnika (null = niko nije prijavljen)
    public static void PrijavljenKorisnik(this Mock<UserManager<ApplicationUser>> userManager, ApplicationUser? korisnik) =>
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(korisnik);

    public static void VratiUloge(this Mock<UserManager<ApplicationUser>> userManager, ApplicationUser korisnik, params string[] uloge) =>
        userManager.Setup(m => m.GetRolesAsync(korisnik)).ReturnsAsync(uloge.ToList());

    public static void PronadjiPoUsername(this Mock<UserManager<ApplicationUser>> userManager, ApplicationUser korisnik) =>
        userManager.Setup(m => m.FindByNameAsync(korisnik.UserName!)).ReturnsAsync(korisnik);

    public static void PronadjiPoId(this Mock<UserManager<ApplicationUser>> userManager, ApplicationUser? korisnik, string id) =>
        userManager.Setup(m => m.FindByIdAsync(id)).ReturnsAsync(korisnik);
}
