using System.Globalization;
using System.Text.RegularExpressions;
using PopravkaBa.Domain.Enums;

namespace PopravkaBa.Domain.Shared
{
    public static class JacinaLozinke
    {
        private const int MinimalnaDuzina = 8;
        private const int MinimalniBodoviZaSrednju = 2;
        private const int BodoviZaJaku = 4;
        private const int MinimalnaDuzinaDijelaEmaila = 5;

        private static readonly string[] CesteLozinke =
        {
            "lozinka", "password", "qwerty", "12345678", "sifra", "admin123", "abc12345"
        };

        // Datum sa razdvajačem: 15.03.1998, 15-03-98, 15/03/1998 ili 1998-03-15
        private static readonly Regex DatumSaRazdvajacem =
            new(@"\d{1,2}[./-]\d{1,2}[./-]\d{2}(?:\d{2})?|\d{4}[./-]\d{1,2}[./-]\d{1,2}", RegexOptions.Compiled);

        // Svaki niz od 8 cifara, i kad se preklapaju (u "x150319981" su to 15031998 i 50319981)
        private static readonly Regex OsamCifara = new(@"(?=(\d{8}))", RegexOptions.Compiled);

        private static readonly string[] FormatiSaRazdvajacem =
        {
            "d.M.yyyy", "d-M-yyyy", "d/M/yyyy",
            "d.M.yy", "d-M-yy", "d/M/yy",
            "yyyy.M.d", "yyyy-M-d", "yyyy/M/d"
        };

        private static readonly string[] FormatiBezRazdvajaca = { "ddMMyyyy", "yyyyMMdd" };

        public static Jacina Procijeni(string lozinka, string? email)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(lozinka);
            var bezRazmaka = UkloniRazmake(lozinka);

            if (SadrziDioEmaila(bezRazmaka, email) || JeCestaLozinka(bezRazmaka) || SadrziDatum(bezRazmaka))
                return Jacina.Slaba;

            var bodovi = IzracunajBodove(bezRazmaka);

            if (bodovi >= BodoviZaJaku) return Jacina.Jaka;
            return bodovi >= MinimalniBodoviZaSrednju ? Jacina.Srednja : Jacina.Slaba;
        }

        private static string UkloniRazmake(string lozinka) =>
            new(lozinka.Where(znak => !char.IsWhiteSpace(znak)).ToArray());

        private static bool SadrziDioEmaila(string lozinka, string? email)
        {
            if (string.IsNullOrEmpty(email)) return false;

            var dioEmaila = email.Split('@')[0];
            return dioEmaila.Length >= MinimalnaDuzinaDijelaEmaila
                && lozinka.Contains(dioEmaila, StringComparison.OrdinalIgnoreCase);
        }

        private static bool JeCestaLozinka(string lozinka) =>
            CesteLozinke.Any(cesta => lozinka.Contains(cesta, StringComparison.OrdinalIgnoreCase));

        private static bool SadrziDatum(string lozinka) =>
            DatumSaRazdvajacem.Matches(lozinka).Any(kandidat => JeDatum(kandidat.Value, FormatiSaRazdvajacem))
            || OsamCifara.Matches(lozinka).Any(kandidat => JeDatum(kandidat.Groups[1].Value, FormatiBezRazdvajaca));

        private static bool JeDatum(string tekst, string[] formati) =>
            DateTime.TryParseExact(tekst, formati, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

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