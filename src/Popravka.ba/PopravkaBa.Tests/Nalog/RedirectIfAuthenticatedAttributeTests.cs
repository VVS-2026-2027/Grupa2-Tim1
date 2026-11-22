using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using PopravkaBa.Web.Attributes;

namespace PopravkaBa.Tests.Nalog;

// Filter na stranicama za prijavu i registraciju: prijavljenog korisnika vraća na početnu
[TestClass]
[TestCategory("Controller")]
public class RedirectIfAuthenticatedAttributeTests
{
    private static ActionExecutingContext Kontekst(ClaimsPrincipal korisnik)
    {
        var http = new DefaultHttpContext { User = korisnik };
        var akcija = new ActionContext(http, new RouteData(), new ActionDescriptor());
        return new ActionExecutingContext(akcija, new List<IFilterMetadata>(), new Dictionary<string, object?>(), new object());
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public void OnActionExecuting_PrijavljenKorisnik_PreusmjeravaNaPocetnu()
    {
        // Arrange
        var kontekst = Kontekst(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "haso") }, "Test")));

        // Act
        new RedirectIfAuthenticatedAttribute().OnActionExecuting(kontekst);

        // Assert
        var preusmjerenje = Assert.IsInstanceOfType<RedirectToActionResult>(kontekst.Result);
        Assert.AreEqual("Index", preusmjerenje.ActionName);
        Assert.AreEqual("Home", preusmjerenje.ControllerName);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public void OnActionExecuting_NeprijavljenKorisnik_PustaZahtjevDalje()
    {
        // Arrange
        var kontekst = Kontekst(new ClaimsPrincipal(new ClaimsIdentity()));

        // Act
        new RedirectIfAuthenticatedAttribute().OnActionExecuting(kontekst);

        // Assert
        Assert.IsNull(kontekst.Result);
    }
}
