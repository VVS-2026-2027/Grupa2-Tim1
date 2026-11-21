using PopravkaBa.Application.Services.Implementation;
using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Models;

namespace PopravkaBa.Tests.Profil;

// Pomoćne metode koje samo pripremaju podatke za testove
internal static class IzvrsilacUslugeServiceTestHelper
{
    internal static Majstor Majstor(string id) => new()
    {
        Id = id,
        UserName = id,
        Email = $"{id}@test.ba",
        Adresa = ""
    };
}
