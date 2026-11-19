using PopravkaBa.Domain.Enums;

namespace PopravkaBa.Domain.Shared
{
    public static class JacinaLozinke
    {
        private const int MinimalnaDuzina = 8;
        private const int MinimalniBodoviZaSrednju = 2;
        private const int BodoviZaJaku = 4;

        public static Jacina Procijeni(string lozinka, string? email)
        {
            if (!string.IsNullOrEmpty(email))
            {
                var dioEmaila = email.Split('@')[0];
                if (dioEmaila.Length >= 5 && lozinka.Contains(dioEmaila, StringComparison.OrdinalIgnoreCase))
                    return Jacina.Slaba;
            }

            var bodovi = IzracunajBodove(lozinka);

            if (bodovi >= BodoviZaJaku) return Jacina.Jaka;
            return bodovi >= MinimalniBodoviZaSrednju ? Jacina.Srednja : Jacina.Slaba;
        }

        private static int IzracunajBodove(string lozinka)
        {
            var bodovi = 0;
            if (lozinka.Length >= MinimalnaDuzina) bodovi++;
            if (lozinka.Any(char.IsDigit)) bodovi++;
            if (lozinka.Any(char.IsUpper)) bodovi++;
            if (lozinka.Any(znak => !char.IsLetterOrDigit(znak))) bodovi++;
            return bodovi;
        }
    }
}