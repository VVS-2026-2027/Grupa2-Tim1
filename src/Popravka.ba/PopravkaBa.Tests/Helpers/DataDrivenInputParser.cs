using System.Globalization;
using System.Xml.Linq;

namespace PopravkaBa.Tests.Helpers;

public static class DataDrivenInputParser
{
    public static IEnumerable<object[]> CitajCsv<T1>(string putanja) => Csv(putanja, typeof(T1));
    public static IEnumerable<object[]> CitajCsv<T1, T2>(string putanja) => Csv(putanja, typeof(T1), typeof(T2));
    public static IEnumerable<object[]> CitajCsv<T1, T2, T3>(string putanja) => Csv(putanja, typeof(T1), typeof(T2), typeof(T3));
    public static IEnumerable<object[]> CitajCsv<T1, T2, T3, T4>(string putanja) => Csv(putanja, typeof(T1), typeof(T2), typeof(T3), typeof(T4));
    public static IEnumerable<object[]> CitajCsv<T1, T2, T3, T4, T5>(string putanja) => Csv(putanja, typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5));

    public static IEnumerable<object[]> CitajXml<T1>(string putanja) => Xml(putanja, typeof(T1));
    public static IEnumerable<object[]> CitajXml<T1, T2>(string putanja) => Xml(putanja, typeof(T1), typeof(T2));
    public static IEnumerable<object[]> CitajXml<T1, T2, T3>(string putanja) => Xml(putanja, typeof(T1), typeof(T2), typeof(T3));
    public static IEnumerable<object[]> CitajXml<T1, T2, T3, T4>(string putanja) => Xml(putanja, typeof(T1), typeof(T2), typeof(T3), typeof(T4));
    public static IEnumerable<object[]> CitajXml<T1, T2, T3, T4, T5>(string putanja) => Xml(putanja, typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5));

    // Prvi red CSV-a su nazivi kolona. Vrijednosti ne smiju sadržavati zarez.
    private static IEnumerable<object[]> Csv(string putanja, params Type[] tipovi) =>
        File.ReadAllLines(Puna(putanja))
            .Skip(1)
            .Where(red => !string.IsNullOrWhiteSpace(red))
            .Select(red => Pretvori(red.Split(','), tipovi))
            .ToList();

    // <slucajevi><slucaj><a>..</a><b>..</b></slucaj>...</slucajevi>, kolone redom
    private static IEnumerable<object[]> Xml(string putanja, params Type[] tipovi) =>
        XDocument.Load(Puna(putanja)).Root!
            .Elements()
            .Select(slucaj => Pretvori(slucaj.Elements().Select(e => e.Value).ToArray(), tipovi))
            .ToList();

    private static string Puna(string putanja) => Path.Combine(AppContext.BaseDirectory, putanja);

    private static object[] Pretvori(string[] vrijednosti, Type[] tipovi)
    {
        if (vrijednosti.Length != tipovi.Length)
            throw new InvalidDataException($"Red ima {vrijednosti.Length} kolona, a test očekuje {tipovi.Length}.");
        return vrijednosti.Select((v, i) => Pretvori(v.Trim(), tipovi[i])).ToArray();
    }

    // Prazna ćelija = null (za string i nullable tipove)
    private static object Pretvori(string v, Type tip)
    {
        var osnovni = Nullable.GetUnderlyingType(tip) ?? tip;
        if (v.Length == 0 && (!tip.IsValueType || osnovni != tip)) return null!;
        if (osnovni.IsEnum) return Enum.Parse(osnovni, v, ignoreCase: true);
        if (osnovni == typeof(DateTime)) return DateTime.Parse(v, CultureInfo.InvariantCulture);
        return Convert.ChangeType(v, osnovni, CultureInfo.InvariantCulture);
    }
}