using PopravkaBa.Application.Services;
using PopravkaBa.Domain.Interfaces.Repositories;
using PopravkaBa.Domain.Models;

namespace PopravkaBa.Tests.MaliServisi;

// Pomoćne metode koje samo pripremaju podatke za testove
internal static class UvjetOglasaServiceTestHelper
{
    internal static UvjetOglasa Uvjet(int id, int oglasId, string tekst) =>
        new() { ID = id, OglasID = oglasId, TekstUvjeta = tekst };
}
