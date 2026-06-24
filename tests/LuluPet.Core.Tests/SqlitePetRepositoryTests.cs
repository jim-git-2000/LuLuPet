using LuluPet.Core.Storage;
using Xunit;

namespace LuluPet.Core.Tests;

public sealed class SqlitePetRepositoryTests
{
    [Fact]
    public void Initialize_CreatesDatabaseAndDefaultRows()
    {
        var databasePath = CreateDatabasePath();
        var repository = new SqlitePetRepository(databasePath);

        repository.Initialize();

        Assert.True(File.Exists(databasePath));
        var profile = repository.LoadProfile();
        var status = repository.LoadStatus();
        Assert.Equal("Lulu", profile.Name);
        Assert.Equal(0, profile.InteractionCount);
        Assert.Equal("Idle", status.State);
    }

    [Fact]
    public void SaveStatus_CanBeLoadedByNewRepository()
    {
        var databasePath = CreateDatabasePath();
        var repository = new SqlitePetRepository(databasePath);
        repository.Initialize();

        repository.SaveStatus(new PetStatus(
            "Walk",
            321,
            654,
            DateTimeOffset.Parse("2026-06-23T00:00:00Z")));

        var nextRepository = new SqlitePetRepository(databasePath);
        nextRepository.Initialize();
        var status = nextRepository.LoadStatus();

        Assert.Equal("Walk", status.State);
        Assert.Equal(321, status.WindowLeft);
        Assert.Equal(654, status.WindowTop);
    }

    [Fact]
    public void RecordInteraction_WritesLogAndIncrementsProfileCount()
    {
        var databasePath = CreateDatabasePath();
        var repository = new SqlitePetRepository(databasePath);
        repository.Initialize();

        var id = repository.RecordInteraction(new PetInteraction(
            "click",
            "hello",
            "Happy",
            111,
            222,
            DateTimeOffset.Parse("2026-06-23T00:00:00Z")));

        var profile = repository.LoadProfile();
        var interactions = repository.LoadRecentInteractions(1);

        Assert.True(id > 0);
        Assert.Equal(1, profile.InteractionCount);
        var interaction = Assert.Single(interactions);
        Assert.Equal("click", interaction.InteractionType);
        Assert.Equal("hello", interaction.Message);
        Assert.Equal("Happy", interaction.State);
        Assert.Equal(111, interaction.WindowLeft);
        Assert.Equal(222, interaction.WindowTop);
    }

    [Fact]
    public void CompanionDayStats_CanBeAddedAndLoaded()
    {
        var databasePath = CreateDatabasePath();
        var repository = new SqlitePetRepository(databasePath);
        repository.Initialize();
        var localDate = new DateOnly(2026, 6, 24);

        repository.AddCompanionSeconds(localDate, 120, DateTimeOffset.Parse("2026-06-24T01:00:00Z"));
        repository.AddCompanionSeconds(localDate, 30, DateTimeOffset.Parse("2026-06-24T01:01:00Z"));

        var stats = repository.LoadCompanionDayStats(localDate);

        Assert.Equal(localDate, stats.LocalDate);
        Assert.Equal(150, stats.TotalSeconds);
    }

    [Fact]
    public void CompanionDayStats_SeparatesDifferentDates()
    {
        var databasePath = CreateDatabasePath();
        var repository = new SqlitePetRepository(databasePath);
        repository.Initialize();
        var firstDate = new DateOnly(2026, 6, 24);
        var secondDate = new DateOnly(2026, 6, 25);

        repository.AddCompanionSeconds(firstDate, 90, DateTimeOffset.Parse("2026-06-24T01:00:00Z"));
        repository.AddCompanionSeconds(secondDate, 45, DateTimeOffset.Parse("2026-06-25T01:00:00Z"));

        Assert.Equal(90, repository.LoadCompanionDayStats(firstDate).TotalSeconds);
        Assert.Equal(45, repository.LoadCompanionDayStats(secondDate).TotalSeconds);
    }

    private static string CreateDatabasePath()
    {
        return Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "lulupet.db");
    }
}
