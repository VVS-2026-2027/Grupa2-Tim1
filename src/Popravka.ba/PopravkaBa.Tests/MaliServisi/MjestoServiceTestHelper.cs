using PopravkaBa.Application.Services;
using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Models;

namespace PopravkaBa.Tests.MaliServisi;

// Pomoćne metode koje samo pripremaju podatke za testove
internal static class MjestoServiceTestHelper
{
    internal static Mjesto Mjesto(int id, string naziv) => new() { MjestoID = id, Naziv = naziv };
}
