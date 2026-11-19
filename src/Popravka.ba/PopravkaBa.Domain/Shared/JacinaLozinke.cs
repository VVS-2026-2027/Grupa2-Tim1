using PopravkaBa.Domain.Enums;

namespace PopravkaBa.Domain.Shared
{
    public static class JacinaLozinke
    {
        public static Jacina Procijeni(string lozinka, string? email)
        {
            var bodovi = 0;
            if (lozinka.Length >= 8) bodovi++;
            if (lozinka.Any(char.IsDigit)) bodovi++;

            return bodovi >= 2 ? Jacina.Srednja : Jacina.Slaba;
        }
    }
}