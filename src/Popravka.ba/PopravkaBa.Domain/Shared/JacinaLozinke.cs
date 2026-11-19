using PopravkaBa.Domain.Enums;

namespace PopravkaBa.Domain.Shared
{
    public static class JacinaLozinke
    {
        public static Jacina Procijeni(string lozinka, string? email)
        {
            if (lozinka.Contains(email.Split('@')[0], StringComparison.OrdinalIgnoreCase)) return Jacina.Slaba;
            var bodovi = 0;
            if (lozinka.Length >= 8) bodovi++;
            if (lozinka.Any(char.IsDigit)) bodovi++;
            if (lozinka.Any(char.IsUpper)) bodovi++;
            if (lozinka.Any(znak => !char.IsLetterOrDigit(znak))) bodovi++;

            if (bodovi == 4) return Jacina.Jaka;

            return bodovi >= 2 ? Jacina.Srednja : Jacina.Slaba;
        }
    }
}