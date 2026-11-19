using PopravkaBa.Domain.Enums;

namespace PopravkaBa.Domain.Shared
{
    public static class JacinaLozinke
    {
        private const int MinimalnaDuzina = 8;
        private const int MinimalniBodoviZaSrednju = 2;
        private const int BodoviZaJaku = 4;
        private const int MinimalnaDuzinaDijelaEmaila = 5;

        public static Jacina Procijeni(string lozinka, string? email)
        {
            ArgumentException.ThrowIfNullOrEmpty(lozinka);

            var bezRazmaka = new string(lozinka.Where(znak => !char.IsWhiteSpace(znak)).ToArray());

            if (SadrziDioEmaila(bezRazmaka, email))
                return Jacina.Slaba;

            var bodovi = IzracunajBodove(bezRazmaka);

            if (bodovi >= BodoviZaJaku) return Jacina.Jaka;
            return bodovi >= MinimalniBodoviZaSrednju ? Jacina.Srednja : Jacina.Slaba;
        }

        private static bool SadrziDioEmaila(string lozinka, string? email)
        {
            if (string.IsNullOrEmpty(email)) return false;

            var dioEmaila = email.Split('@')[0];
            return dioEmaila.Length >= MinimalnaDuzinaDijelaEmaila
                && lozinka.Contains(dioEmaila, StringComparison.OrdinalIgnoreCase);
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