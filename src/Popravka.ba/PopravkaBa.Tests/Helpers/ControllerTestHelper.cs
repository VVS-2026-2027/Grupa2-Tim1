using Microsoft.AspNetCore.Mvc;

namespace PopravkaBa.Tests.Helpers;

// Pomoćne metode za čitanje stanja kontrolera nakon akcije
public static class ControllerTestHelper
{
    // Sve poruke grešaka koje je akcija upisala u ModelState
    public static List<string> PorukeGresaka(this Controller kontroler) =>
        kontroler.ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
}
