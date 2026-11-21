using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Popravka.ba.Data;
using PopravkaBa.Infrastructure.Migrations;

namespace PopravkaBa.Tests.Baza;

[TestClass]
[TestCategory("Infrastructure")]
public class MigracijeTests
{
    public static IEnumerable<object[]> Migracije =>
        DataDrivenInputParser.CitajXml<string>("TestData/xml/Migracije_spisak.xml");

    [TestMethod]
    [TestCategory("DataDriven")]
    [Owner("Nerina Čengić")]
    [DynamicData(nameof(Migracije))]
    public void Migracija_PodaciIzXml_UpDownIImaTargetModel(string naziv)
    {
        // Arrange
        var tip = typeof(ApplicationDbContext).Assembly.GetTypes().Single(t => t.Name == naziv);
        var migracija = (Migration)Activator.CreateInstance(tip)!;

        // Act
        var upOperacije = migracija.UpOperations;
        var downOperacije = migracija.DownOperations;
        var metoda = tip.GetMethod("BuildTargetModel", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var modelBuilder = new ModelBuilder();
        metoda.Invoke(migracija, new object[] { modelBuilder });

        // Assert
        Assert.IsTrue(upOperacije.Count > 0);
        Assert.IsTrue(downOperacije.Count > 0);
        Assert.IsTrue(modelBuilder.Model.GetEntityTypes().Any());
    }

    [TestMethod]
    [Owner("Nerina Čengić")]
    public void ModelSnapshot_BuildModel_GradiModelSaEntitetima()
    {
        // Arrange
        var tip = typeof(ApplicationDbContext).Assembly
            .GetTypes().Single(t => t.Name == "ApplicationDbContextModelSnapshot");
        var snapshot = Activator.CreateInstance(tip)!;
        var metoda = tip.GetMethod("BuildModel", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var modelBuilder = new ModelBuilder();

        // Act
        metoda.Invoke(snapshot, new object[] { modelBuilder });

        // Assert
        Assert.IsTrue(modelBuilder.Model.GetEntityTypes().Any());
    }
}
