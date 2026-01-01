namespace sp26se058_3dprintshop_be.Application.FunctionalTests;

public static class TestDatabaseFactory
{
    public static async Task<ITestDatabase> CreateAsync()
    {
        var database = new TestcontainersTestDatabase();

        await database.InitialiseAsync();

        return database;
    }
}
