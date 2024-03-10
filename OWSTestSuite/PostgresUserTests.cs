using Microsoft.Extensions.Configuration;
using OWSShared.Options;
using OWSTestSuite.Models;
using static OWSTestSuite.Helpers;

namespace OWSTestSuite;

public class PostgresUserTests : MSSQLUserTests
{
    [SetUp]
    public override async Task Setup()
    {
        StorageOptions = Options.Create(new StorageOptions());
        StorageOptions.Value.OWSDBConnectionString = Configuration.GetConnectionString("Postgres");

        UserRepo = new OWSData.Repositories.Implementations.Postgres.UsersRepository(StorageOptions);
        CharRepo = new OWSData.Repositories.Implementations.Postgres.CharactersRepository(StorageOptions);
        InstanceRepo = new OWSData.Repositories.Implementations.Postgres.InstanceManagementRepository(StorageOptions);
        GlobalRepo = new OWSData.Repositories.Implementations.Postgres.GlobalDataRepository(StorageOptions);
        Connection = new NpgsqlConnection(StorageOptions.Value.OWSDBConnectionString);
        await PostgresSetupAbilities(Connection, CustomerGuid);

        // Ensure we have 2 test users
        foreach (Account account in Accounts)
        {
            await UserRepo.RegisterUser(CustomerGuid, account.User?.Email, account.User?.Password, account.User?.FirstName, account.User?.LastName);
        }

    }

    [TearDown]
    public override async Task BaseTearDown()
    {
        Connection = new NpgsqlConnection(StorageOptions.Value.OWSDBConnectionString);

        foreach (Account account in Accounts)
        {
            PlayerLoginAndCreateSession? playerLoginAndCreateSession = await UserRepo.LoginAndCreateSession(CustomerGuid, account.User?.Email, account.User?.Password);
            await UserRepo.RemoveCharacter(CustomerGuid, (Guid)playerLoginAndCreateSession.UserSessionGuid!, account.Character?.CharacterName);

            await RemoveUser(Connection, CustomerGuid, playerLoginAndCreateSession);
        }

        await RemoveAbilities(Connection, CustomerGuid, LauncherGuid);
        await RemoveCustomData(Connection, CustomerGuid);
    }
}
