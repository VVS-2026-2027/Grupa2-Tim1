using Amazon.S3;
using Amazon.S3.Model;
using PopravkaBa.Infrastructure.Adapters;
using PopravkaBa.Infrastructure.Adapters.Options;

namespace PopravkaBa.Tests.Zajednicko;

[TestClass]
public class S3FileStorageAdapterTests
{
    private Mock<IAmazonS3> _s3 = null!;
    private S3FileStorageAdapter _adapter = null!;

    [TestInitialize]
    public void Postavi()
    {
        _s3 = new Mock<IAmazonS3>();
        _s3.Setup(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutObjectResponse());
        _s3.Setup(s => s.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteObjectResponse());

        var opcije = Microsoft.Extensions.Options.Options.Create(new R2Options
        {
            PublicBucket = "javni-bucket",
            PrivateBucket = "privatni-bucket",
            PublicBaseURL = "https://cdn.popravka.ba"
        });
        _adapter = new S3FileStorageAdapter(_s3.Object, opcije);
    }

    // ---------- Extension (data-driven iz XML-a) ----------

    public static IEnumerable<object[]> SlucajeviEkstenzija =>
        DataDrivenInputParser.CitajXml<string, string>("TestData/xml/S3FileStorageAdapter_ekstenzije.xml");

    [TestMethod]
    [DynamicData(nameof(SlucajeviEkstenzija))]
    public void Extension_PodaciIzXml_VracaOcekivanuEkstenziju(string contentType, string ekstenzija)
    {
        // Act
        var rezultat = S3FileStorageAdapter.Extension(contentType);

        // Assert
        Assert.AreEqual(ekstenzija, rezultat);
    }

    [TestMethod]
    public void Extension_NepoznatContentType_VracaPrazanString()
    {
        // Act
        var rezultat = S3FileStorageAdapter.Extension("text/plain");

        // Assert
        Assert.AreEqual(string.Empty, rezultat);
    }

    // ---------- SpremiPublic ----------

    [TestMethod]
    public async Task SpremiPublic_PngSlika_VracaJavniUrlSaKljucemUSlikeFolderu()
    {
        // Arrange
        using var sadrzaj = new MemoryStream(new byte[] { 1, 2, 3 });

        // Act
        var url = await _adapter.SpremiPublic(sadrzaj, "image/png");

        // Assert
        Assert.IsTrue(url.StartsWith("https://cdn.popravka.ba/slike/"));
        Assert.IsTrue(url.EndsWith(".png"));
    }

    [TestMethod]
    public async Task SpremiPublic_PngSlika_SaljeZahtjevNaJavniBucket()
    {
        // Arrange
        using var sadrzaj = new MemoryStream(new byte[] { 1, 2, 3 });

        // Act
        await _adapter.SpremiPublic(sadrzaj, "image/png");

        // Assert
        _s3.Verify(s => s.PutObjectAsync(It.Is<PutObjectRequest>(r =>
            r.BucketName == "javni-bucket" &&
            r.ContentType == "image/png" &&
            r.Key.StartsWith("slike/") &&
            r.Key.EndsWith(".png") &&
            r.InputStream == sadrzaj), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task SpremiPublic_S3BacaGresku_ProsljedjujeIzuzetak()
    {
        // Arrange
        _s3.Setup(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("Greška storage-a"));
        using var sadrzaj = new MemoryStream(new byte[] { 1 });

        // Act + Assert
        await Assert.ThrowsExactlyAsync<AmazonS3Exception>(() => _adapter.SpremiPublic(sadrzaj, "image/png"));
    }

    // ---------- SpremiPrivate ----------

    [TestMethod]
    public async Task SpremiPrivate_PdfFajl_VracaKljucSaEkstenzijomIzImenaFajla()
    {
        // Arrange
        using var sadrzaj = new MemoryStream(new byte[] { 1, 2, 3 });

        // Act
        var kljuc = await _adapter.SpremiPrivate(sadrzaj, "rjesenje.pdf", "application/pdf");

        // Assert
        Assert.IsTrue(kljuc.StartsWith("prilozi/"));
        Assert.IsTrue(kljuc.EndsWith(".pdf"));
    }

    [TestMethod]
    public async Task SpremiPrivate_PdfFajl_SaljeZahtjevNaPrivatniBucket()
    {
        // Arrange
        using var sadrzaj = new MemoryStream(new byte[] { 1, 2, 3 });

        // Act
        var kljuc = await _adapter.SpremiPrivate(sadrzaj, "rjesenje.pdf", "application/pdf");

        // Assert
        _s3.Verify(s => s.PutObjectAsync(It.Is<PutObjectRequest>(r =>
            r.BucketName == "privatni-bucket" &&
            r.Key == kljuc &&
            r.ContentType == "application/pdf"), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- GetSignedURL ----------

    [TestMethod]
    public void GetSignedURL_PostojeciKljuc_VracaUrlKojiJeNapravioS3Klijent()
    {
        // Arrange
        _s3.Setup(s => s.GetPreSignedURL(It.IsAny<GetPreSignedUrlRequest>())).Returns("https://potpisani.url");

        // Act
        var url = _adapter.GetSignedURL("prilozi/abc.pdf", TimeSpan.FromMinutes(5));

        // Assert
        Assert.AreEqual("https://potpisani.url", url);
    }

    [TestMethod]
    public void GetSignedURL_PostojeciKljuc_TraziHttpsGetZaPrivatniBucket()
    {
        // Arrange
        _s3.Setup(s => s.GetPreSignedURL(It.IsAny<GetPreSignedUrlRequest>())).Returns("https://potpisani.url");

        // Act
        _adapter.GetSignedURL("prilozi/abc.pdf", TimeSpan.FromMinutes(5));

        // Assert
        _s3.Verify(s => s.GetPreSignedURL(It.Is<GetPreSignedUrlRequest>(r =>
            r.BucketName == "privatni-bucket" &&
            r.Key == "prilozi/abc.pdf" &&
            r.Verb == HttpVerb.GET &&
            r.Protocol == Protocol.HTTPS)), Times.Once);
    }

    // ---------- ObrisiPublic / ObrisiPrivate (mock: metode ništa ne vraćaju) ----------

    [TestMethod]
    public async Task ObrisiPublic_JavniUrl_BriseObjekatPoKljucuBezBaznogUrla()
    {
        // Act
        await _adapter.ObrisiPublic("https://cdn.popravka.ba/slike/abc.png");

        // Assert
        _s3.Verify(s => s.DeleteObjectAsync(It.Is<DeleteObjectRequest>(r =>
            r.BucketName == "javni-bucket" &&
            r.Key == "slike/abc.png"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task ObrisiPrivate_PostojeciKljuc_BriseObjekatIzPrivatnogBucketa()
    {
        // Act
        await _adapter.ObrisiPrivate("prilozi/abc.pdf");

        // Assert
        _s3.Verify(s => s.DeleteObjectAsync(It.Is<DeleteObjectRequest>(r =>
            r.BucketName == "privatni-bucket" &&
            r.Key == "prilozi/abc.pdf"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task ObrisiPrivate_S3BacaGresku_ProsljedjujeIzuzetak()
    {
        // Arrange
        _s3.Setup(s => s.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("Greška storage-a"));

        // Act + Assert
        await Assert.ThrowsExactlyAsync<AmazonS3Exception>(() => _adapter.ObrisiPrivate("prilozi/abc.pdf"));
    }
}
