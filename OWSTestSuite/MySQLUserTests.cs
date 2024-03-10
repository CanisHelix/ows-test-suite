using Microsoft.Extensions.Configuration;
using OWSTestSuite.Models;
using static OWSTestSuite.Helpers;

namespace OWSTestSuite;

public class MySQLUserTests : MSSQLUserTests
{
    [SetUp]
    public override async Task Setup()
    {
        StorageOptions = Options.Create(new StorageOptions());
        StorageOptions.Value.OWSDBConnectionString = Configuration.GetConnectionString("MySQL");

        UserRepo = new OWSData.Repositories.Implementations.MySQL.UsersRepository(StorageOptions);
        CharRepo = new OWSData.Repositories.Implementations.MySQL.CharactersRepository(StorageOptions);
        InstanceRepo = new OWSData.Repositories.Implementations.MySQL.InstanceManagementRepository(StorageOptions);
        GlobalRepo = new OWSData.Repositories.Implementations.MySQL.GlobalDataRepository(StorageOptions);
        Connection = new MySqlConnection(StorageOptions.Value.OWSDBConnectionString);
        await MySQLSetupAbilities(Connection, CustomerGuid);

        // Ensure we have 2 test users

        foreach (Account account in Accounts)
        {
            await UserRepo.RegisterUser(CustomerGuid, account.User?.Email, account.User?.Password, account.User?.FirstName, account.User?.LastName);
        }

    }

    [TearDown]
    public override async Task BaseTearDown()
    {
        Connection = new MySqlConnection(StorageOptions.Value.OWSDBConnectionString);

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
