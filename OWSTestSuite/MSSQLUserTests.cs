using Microsoft.Extensions.Configuration;
using OWSTestSuite.Models;
using static OWSTestSuite.Helpers;
using User = OWSTestSuite.Models.User;

namespace OWSTestSuite;

public class MSSQLUserTests : BaseTest
{
    protected IUsersRepository UserRepo = null!;
    protected ICharactersRepository CharRepo = null!;
    protected IInstanceManagementRepository InstanceRepo = null!;
    protected IGlobalDataRepository GlobalRepo = null!;
    protected IDbConnection Connection = null!;
    protected IOptions<StorageOptions> StorageOptions = null!;

    [SetUp]
    public virtual async Task Setup()
    {
        StorageOptions = Options.Create(new StorageOptions());
        StorageOptions.Value.OWSDBConnectionString = Configuration.GetConnectionString("MSSQL");

        UserRepo = new OWSData.Repositories.Implementations.MSSQL.UsersRepository(StorageOptions);
        CharRepo = new OWSData.Repositories.Implementations.MSSQL.CharactersRepository(StorageOptions);
        InstanceRepo = new OWSData.Repositories.Implementations.MSSQL.InstanceManagementRepository(StorageOptions);
        GlobalRepo = new OWSData.Repositories.Implementations.MSSQL.GlobalDataRepository(StorageOptions);
        Connection = new SqlConnection(StorageOptions.Value.OWSDBConnectionString);
        await MSSQLSetupAbilities(Connection, CustomerGuid);

        // Ensure we have 2 test users

        foreach (Account account in Accounts)
        {
            await UserRepo.RegisterUser(CustomerGuid, account.User?.Email, account.User?.Password, account.User?.FirstName, account.User?.LastName);
        }

    }

    [TearDown]
    public virtual async Task BaseTearDown()
    {
        Connection = new SqlConnection(StorageOptions.Value.OWSDBConnectionString);

        foreach (Account account in Accounts)
        {
            PlayerLoginAndCreateSession playerLoginAndCreateSession = await UserRepo.LoginAndCreateSession(CustomerGuid, account.User?.Email, account.User?.Password);
            await UserRepo.RemoveCharacter(CustomerGuid, (Guid)playerLoginAndCreateSession.UserSessionGuid!, account.Character?.CharacterName);

            await RemoveUser(Connection, CustomerGuid, playerLoginAndCreateSession);
        }

        await RemoveAbilities(Connection, CustomerGuid, LauncherGuid);
        await RemoveCustomData(Connection, CustomerGuid);
    }

    [Test]
    public async Task FailLogin()
    {
        PlayerLoginAndCreateSession playerLoginAndCreateSession = await UserRepo.LoginAndCreateSession(CustomerGuid, Accounts[0].User?.Email, BadPassword);
        Console.WriteLine(PropertyList(playerLoginAndCreateSession));
        Assert.That(playerLoginAndCreateSession.Authenticated, Is.False);
    }

    [Test]
    public async Task SuccessLogin()
    {
        PlayerLoginAndCreateSession playerLoginAndCreateSession = await UserRepo.LoginAndCreateSession(CustomerGuid, Accounts[0].User?.Email, Accounts[0].User?.Password);
        Assert.That(playerLoginAndCreateSession.Authenticated);
    }

    [Test]
    public async Task CreateCharacter()
    {
        // Login First
        PlayerLoginAndCreateSession playerLoginAndCreateSession = await UserRepo.LoginAndCreateSession(CustomerGuid, Accounts[0].User?.Email, Accounts[0].User?.Password);
        Assert.That(playerLoginAndCreateSession.Authenticated);

        CreateCharacter createCharacter = await UserRepo.CreateCharacter(CustomerGuid, (Guid)playerLoginAndCreateSession.UserSessionGuid!, Accounts[0].Character?.CharacterName, Accounts[0].Character?.CharacterClass);
        Assert.Multiple(() =>
        {
            Assert.That(createCharacter.ClassName, Is.EqualTo(Accounts[0].Character?.CharacterClass));
            Assert.That(createCharacter.CharacterName, Is.EqualTo(Accounts[0].Character?.CharacterName));
        });
    }

    [Test]
    public async Task AddRemoveAbilityToCharacter()
    {
        // Login First
        PlayerLoginAndCreateSession playerLoginAndCreateSession = await UserRepo.LoginAndCreateSession(CustomerGuid, Accounts[0].User?.Email, Accounts[0].User?.Password);
        Assert.That(playerLoginAndCreateSession.Authenticated);

        CreateCharacter createCharacter = await UserRepo.CreateCharacter(CustomerGuid, (Guid)playerLoginAndCreateSession.UserSessionGuid!, Accounts[0].Character?.CharacterName, Accounts[0].Character?.CharacterClass);
        Assert.Multiple(() =>
        {
            Assert.That(createCharacter.ClassName, Is.EqualTo(Accounts[0].Character?.CharacterClass));
            Assert.That(createCharacter.CharacterName, Is.EqualTo(Accounts[0].Character?.CharacterName));
        });
        IEnumerable<GetCharacterAbilities> characterAbilities = await CharRepo.GetCharacterAbilities(CustomerGuid, Accounts[0].Character?.CharacterName);
        Assert.That(!characterAbilities.Any());

        var abilities = await CharRepo.GetAbilities(CustomerGuid);
        Assert.That(abilities.First().AbilityName, Is.EqualTo("Test"));

        await CharRepo.AddAbilityToCharacter(CustomerGuid, "Test", Accounts[0].Character?.CharacterName, 1, "");
        await CharRepo.AddAbilityToCharacter(CustomerGuid, "Test", Accounts[0].Character?.CharacterName, 1, "");

        characterAbilities = await CharRepo.GetCharacterAbilities(CustomerGuid, Accounts[0].Character?.CharacterName);
        Assert.That(characterAbilities.Any());

        await CharRepo.UpdateAbilityOnCharacter(CustomerGuid, "Test", Accounts[0].Character?.CharacterName, 10, "");
        characterAbilities = await CharRepo.GetCharacterAbilities(CustomerGuid, Accounts[0].Character?.CharacterName);
        Assert.Multiple(() =>
        {
            Assert.That(characterAbilities.First().AbilityLevel, Is.EqualTo(10));
            Assert.That(characterAbilities.Count(), Is.EqualTo(1));
        });
        await CharRepo.RemoveAbilityFromCharacter(CustomerGuid, "Test", Accounts[0].Character?.CharacterName);
        characterAbilities = await CharRepo.GetCharacterAbilities(CustomerGuid, Accounts[0].Character?.CharacterName);
        Assert.That(!characterAbilities.Any());
    }

    [Test]
    public async Task AddCharacterUsingDefaultCharacterValues()
    {
        // Login First
        PlayerLoginAndCreateSession playerLoginAndCreateSession = await UserRepo.LoginAndCreateSession(CustomerGuid, Accounts[0].User?.Email, Accounts[0].User?.Password);
        Assert.That(playerLoginAndCreateSession.Authenticated);

        GetUserSession userSession = await UserRepo.GetUserSession(CustomerGuid, (Guid)playerLoginAndCreateSession.UserSessionGuid!);
        SuccessAndErrorMessage output = await UserRepo.CreateCharacterUsingDefaultCharacterValues(CustomerGuid, (Guid)userSession.UserGuid!, Accounts[0].Character?.CharacterName, "Default");

        IEnumerable<CustomCharacterData> customCharacterData = await CharRepo.GetCustomCharacterData(CustomerGuid, Accounts[0].Character?.CharacterName);
        var customCharacterDataList = customCharacterData.ToList();
        Assert.Multiple(() =>
        {
            Assert.That(customCharacterDataList.First().CustomFieldName, Is.EqualTo("BaseCharacterStats"));
            Assert.That(customCharacterDataList.First().FieldValue, Is.EqualTo("{\"Strength\": 10, \"Agility\": 10, \"Constitution\": 10 }"));
        });

    }

    [Test]
    public async Task AddRemoveCustomDataToCharacter()
    {
        // Login First
        PlayerLoginAndCreateSession playerLoginAndCreateSession = await UserRepo.LoginAndCreateSession(CustomerGuid, Accounts[0].User?.Email, Accounts[0].User?.Password);
        Assert.That(playerLoginAndCreateSession.Authenticated);

        CreateCharacter createCharacter = await UserRepo.CreateCharacter(CustomerGuid, (Guid)playerLoginAndCreateSession.UserSessionGuid!, Accounts[0].Character?.CharacterName, Accounts[0].Character?.CharacterClass);
        Assert.Multiple(() =>
        {
            Assert.That(createCharacter.ClassName, Is.EqualTo(Accounts[0].Character?.CharacterClass));
            Assert.That(createCharacter.CharacterName, Is.EqualTo(Accounts[0].Character?.CharacterName));
        });
        IEnumerable<CustomCharacterData> customCharacterData = await CharRepo.GetCustomCharacterData(CustomerGuid, Accounts[0].Character?.CharacterName);
        Assert.That(!customCharacterData.Any());

        var addOrUpdateCustomCharacterData = new AddOrUpdateCustomCharacterData
        {
            CharacterName = Accounts[0].Character?.CharacterName,
            CustomFieldName = "CustomField1",
            FieldValue = "0"
        };

        await CharRepo.AddOrUpdateCustomCharacterData(CustomerGuid, addOrUpdateCustomCharacterData);

        customCharacterData = await CharRepo.GetCustomCharacterData(CustomerGuid, Accounts[0].Character?.CharacterName);
        var customCharacterDataList = customCharacterData.ToList();
        Assert.Multiple(() =>
        {
            Assert.That(customCharacterDataList.First().CustomFieldName, Is.EqualTo(addOrUpdateCustomCharacterData.CustomFieldName));
            Assert.That(customCharacterDataList.First().FieldValue, Is.EqualTo(addOrUpdateCustomCharacterData.FieldValue));
        });
        addOrUpdateCustomCharacterData.FieldValue = "1";

        await CharRepo.AddOrUpdateCustomCharacterData(CustomerGuid, addOrUpdateCustomCharacterData);

        customCharacterData = await CharRepo.GetCustomCharacterData(CustomerGuid, Accounts[0].Character?.CharacterName);
        customCharacterDataList = customCharacterData.ToList();
        Assert.Multiple(() =>
        {
            Assert.That(customCharacterDataList.First().CustomFieldName, Is.EqualTo(addOrUpdateCustomCharacterData.CustomFieldName));
            Assert.That(customCharacterDataList.First().FieldValue, Is.EqualTo(addOrUpdateCustomCharacterData.FieldValue));
        });
    }

    [Test]
    public async Task UpdateCharacterStats()
    {
        // Login First
        PlayerLoginAndCreateSession playerLoginAndCreateSession = await UserRepo.LoginAndCreateSession(CustomerGuid, Accounts[0].User?.Email, Accounts[0].User?.Password);
        Assert.That(playerLoginAndCreateSession.Authenticated);

        CreateCharacter createCharacter = await UserRepo.CreateCharacter(CustomerGuid, (Guid)playerLoginAndCreateSession.UserSessionGuid!, Accounts[0].Character?.CharacterName, Accounts[0].Character?.CharacterClass);
        Assert.Multiple(() =>
        {
            Assert.That(createCharacter.ClassName, Is.EqualTo(Accounts[0].Character?.CharacterClass));
            Assert.That(createCharacter.CharacterName, Is.EqualTo(Accounts[0].Character?.CharacterName));
        });

        GetCharByCharName character = await CharRepo.GetCharByCharName(CustomerGuid, Accounts[0].Character?.CharacterName);
        Assert.Multiple(() =>
        {
            Assert.That(character.CharName, Is.EqualTo(Accounts[0].Character?.CharacterName));
            Assert.That(character.ClassName, Is.EqualTo(Accounts[0].Character?.CharacterClass));
            Assert.That(character.CharacterLevel, Is.EqualTo(1));
            Assert.That(character.X, Is.EqualTo(0));
            Assert.That(character.Y, Is.EqualTo(0));
            Assert.That(character.Z, Is.EqualTo(250));
        });

        UpdateCharacterStats updateCharacterStats = new UpdateCharacterStats();
        updateCharacterStats.CustomerGUID = CustomerGuid.ToString();
        updateCharacterStats.CharName = character.CharName;
        updateCharacterStats.CharacterLevel = character.CharacterLevel;
        updateCharacterStats.X = (float)-2500.50;
        updateCharacterStats.Y = (float)35000;
        updateCharacterStats.Z = (float)character.Z + (float)250;
        await CharRepo.UpdateCharacterStats(updateCharacterStats);

        character = await CharRepo.GetCharByCharName(CustomerGuid, Accounts[0].Character?.CharacterName);
        Assert.Multiple(() =>
        {
            Assert.That(character.CharName, Is.EqualTo(Accounts[0].Character?.CharacterName));
            Assert.That(character.ClassName, Is.EqualTo(Accounts[0].Character?.CharacterClass));
            Assert.That(character.CharacterLevel, Is.EqualTo(1));
            Assert.That(character.X, Is.EqualTo(-2500.50));
            Assert.That(character.Y, Is.EqualTo(35000));
            Assert.That(character.Z, Is.EqualTo(500));
        });
    }

    [Test]
    public async Task UpdateCharacterPosition()
    {
        // Login First
        PlayerLoginAndCreateSession playerLoginAndCreateSession = await UserRepo.LoginAndCreateSession(CustomerGuid, Accounts[0].User?.Email, Accounts[0].User?.Password);
        Assert.That(playerLoginAndCreateSession.Authenticated);

        CreateCharacter createCharacter = await UserRepo.CreateCharacter(CustomerGuid, (Guid)playerLoginAndCreateSession.UserSessionGuid!, Accounts[0].Character?.CharacterName, Accounts[0].Character?.CharacterClass);
        Assert.Multiple(() =>
        {
            Assert.That(createCharacter.ClassName, Is.EqualTo(Accounts[0].Character?.CharacterClass));
            Assert.That(createCharacter.CharacterName, Is.EqualTo(Accounts[0].Character?.CharacterName));
        });

        GetCharByCharName character = await CharRepo.GetCharByCharName(CustomerGuid, Accounts[0].Character?.CharacterName);
        Assert.Multiple(() =>
        {
            Assert.That(character.CharName, Is.EqualTo(Accounts[0].Character?.CharacterName));
            Assert.That(character.ClassName, Is.EqualTo(Accounts[0].Character?.CharacterClass));
            Assert.That(character.CharacterLevel, Is.EqualTo(1));
            Assert.That(character.X, Is.EqualTo(0));
            Assert.That(character.Y, Is.EqualTo(0));
            Assert.That(character.Z, Is.EqualTo(250));
        });

        await CharRepo.UpdatePosition(CustomerGuid, Accounts[0].Character?.CharacterName, "", (float)-2500.50, 35000, 500, 0, 0, 0);

        character = await CharRepo.GetCharByCharName(CustomerGuid, Accounts[0].Character?.CharacterName);
        Assert.Multiple(() =>
        {
            Assert.That(character.CharName, Is.EqualTo(Accounts[0].Character?.CharacterName));
            Assert.That(character.ClassName, Is.EqualTo(Accounts[0].Character?.CharacterClass));
            Assert.That(character.CharacterLevel, Is.EqualTo(1));
            Assert.That(character.X, Is.EqualTo(-2500.50));
            Assert.That(character.Y, Is.EqualTo(35000));
            Assert.That(character.Z, Is.EqualTo(501));
        });

        // TODO: Assert User LastAccess update
    }

    [Test]
    public async Task AddRemoveGlobalData()
    {
        var globalData = new GlobalData()
        {
            CustomerGuid = CustomerGuid,
            GlobalDataKey = "GlobalField1",
            GlobalDataValue = "1"
        };

        await GlobalRepo.AddOrUpdateGlobalData(globalData);

        GlobalData? newGlobalData = await GlobalRepo.GetGlobalDataByGlobalDataKey(CustomerGuid, "GlobalField1");

        Assert.That(newGlobalData.GlobalDataKey, Is.EqualTo(globalData.GlobalDataKey));
        Assert.That(newGlobalData.GlobalDataValue, Is.EqualTo(globalData.GlobalDataValue));

        globalData = new GlobalData()
        {
            CustomerGuid = CustomerGuid,
            GlobalDataKey = "GlobalField1",
            GlobalDataValue = "2"
        };

        await GlobalRepo.AddOrUpdateGlobalData(globalData);

        newGlobalData = await GlobalRepo.GetGlobalDataByGlobalDataKey(CustomerGuid, "GlobalField1");

        Assert.That(newGlobalData.GlobalDataKey, Is.EqualTo(globalData.GlobalDataKey));
        Assert.That(newGlobalData.GlobalDataValue, Is.EqualTo(globalData.GlobalDataValue));
    }

    [Test]
    public async Task GetCharacterExtended()
    {
        // Create WorldServer
        SuccessAndErrorMessage output = await InstanceRepo.RegisterLauncher(CustomerGuid, LauncherGuid.ToString(), "127.0.0.1", 1, "127.0.0.1", 7077);
        Assert.That(output.Success);

        var worldServerId = await InstanceRepo.StartWorldServer(CustomerGuid, LauncherGuid.ToString());
        Assert.That(worldServerId, Is.GreaterThan(0));

        // Login First
        PlayerLoginAndCreateSession playerLoginAndCreateSession = await UserRepo.LoginAndCreateSession(CustomerGuid, Accounts[0].User?.Email, Accounts[0].User?.Password);
        Assert.That(playerLoginAndCreateSession.Authenticated);

        // Create Character
        CreateCharacter createCharacter = await UserRepo.CreateCharacter(CustomerGuid, (Guid)playerLoginAndCreateSession.UserSessionGuid!, Accounts[0].Character?.CharacterName, Accounts[0].Character?.CharacterClass);
        Assert.Multiple(() =>
        {
            Assert.That(createCharacter.ClassName, Is.EqualTo(Accounts[0].Character?.CharacterClass));
            Assert.That(createCharacter.CharacterName, Is.EqualTo(Accounts[0].Character?.CharacterName));
        });
        JoinMapByCharName mapInstance = await CharRepo.JoinMapByCharName(CustomerGuid, Accounts[0].Character?.CharacterName, "ThirdPersonExampleMap", 0);
        Assert.That(mapInstance.MapInstanceID, Is.GreaterThan(0));
        await CharRepo.AddCharacterToMapInstanceByCharName(CustomerGuid, Accounts[0].Character?.CharacterName, mapInstance.MapInstanceID);

        GetCharByCharName character = await CharRepo.GetCharByCharName(CustomerGuid, Accounts[0].Character?.CharacterName);
        Assert.Multiple(() =>
        {
            Assert.That(character.CharName, Is.EqualTo(Accounts[0].Character?.CharacterName));
            Assert.That(character.ClassName, Is.EqualTo(Accounts[0].Character?.CharacterClass));
            Assert.That(character.Port, Is.GreaterThan(0));
            Assert.That(character.ServerIp, Is.Not.EqualTo(string.Empty));
            Assert.That(character.MapInstanceID, Is.GreaterThan(0));
        });
    }

    [Test]
    public async Task PlayerLogout()
    {
        // Create WorldServer
        SuccessAndErrorMessage output = await InstanceRepo.RegisterLauncher(CustomerGuid, LauncherGuid.ToString(), "127.0.0.1", 1, "127.0.0.1", 7077);
        Assert.That(output.Success);

        var worldServerId = await InstanceRepo.StartWorldServer(CustomerGuid, LauncherGuid.ToString());
        Assert.That(worldServerId, Is.GreaterThan(0));

        // Login First
        PlayerLoginAndCreateSession playerLoginAndCreateSession = await UserRepo.LoginAndCreateSession(CustomerGuid, Accounts[0].User?.Email, Accounts[0].User?.Password);
        Assert.That(playerLoginAndCreateSession.Authenticated);

        // Create Character
        CreateCharacter createCharacter = await UserRepo.CreateCharacter(CustomerGuid, (Guid)playerLoginAndCreateSession.UserSessionGuid!, Accounts[0].Character?.CharacterName, Accounts[0].Character?.CharacterClass);
        Assert.Multiple(() =>
        {
            Assert.That(createCharacter.ClassName, Is.EqualTo(Accounts[0].Character?.CharacterClass));
            Assert.That(createCharacter.CharacterName, Is.EqualTo(Accounts[0].Character?.CharacterName));
        });
        JoinMapByCharName mapInstance = await CharRepo.JoinMapByCharName(CustomerGuid, Accounts[0].Character?.CharacterName, "ThirdPersonExampleMap", 0);
        Assert.That(mapInstance.MapInstanceID, Is.GreaterThan(0));
        await CharRepo.AddCharacterToMapInstanceByCharName(CustomerGuid, Accounts[0].Character?.CharacterName, mapInstance.MapInstanceID);

        GetCharByCharName character = await CharRepo.GetCharByCharName(CustomerGuid, Accounts[0].Character?.CharacterName);
        Assert.Multiple(() =>
        {
            Assert.That(character.CharName, Is.EqualTo(Accounts[0].Character?.CharacterName));
            Assert.That(character.ClassName, Is.EqualTo(Accounts[0].Character?.CharacterClass));
            Assert.That(character.Port, Is.GreaterThan(0));
            Assert.That(character.ServerIp, Is.Not.EqualTo(string.Empty));
            Assert.That(character.MapInstanceID, Is.GreaterThan(0));
        });

        await CharRepo.PlayerLogout(CustomerGuid, Accounts[0].Character?.CharacterName);

        character = await CharRepo.GetCharByCharName(CustomerGuid, Accounts[0].Character?.CharacterName);
        Assert.Multiple(() =>
        {
            Assert.That(character.CharName, Is.EqualTo(Accounts[0].Character?.CharacterName));
            Assert.That(character.ClassName, Is.EqualTo(Accounts[0].Character?.CharacterClass));
            Assert.That(character.Port, Is.EqualTo(0));
            Assert.That(character.ServerIp, Is.EqualTo(string.Empty));
            Assert.That(character.MapInstanceID, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task UpdateNumberOfPlayers()
    {
        // Create WorldServer
        SuccessAndErrorMessage output = await InstanceRepo.RegisterLauncher(CustomerGuid, LauncherGuid.ToString(), "127.0.0.1", 1, "127.0.0.1", 7077);
        Assert.That(output.Success);

        var worldServerId = await InstanceRepo.StartWorldServer(CustomerGuid, LauncherGuid.ToString());
        Assert.That(worldServerId, Is.GreaterThan(0));

        // Login First
        PlayerLoginAndCreateSession playerLoginAndCreateSession = await UserRepo.LoginAndCreateSession(CustomerGuid, Accounts[0].User?.Email, Accounts[0].User?.Password);
        Assert.That(playerLoginAndCreateSession.Authenticated);

        // Create Character
        CreateCharacter createCharacter = await UserRepo.CreateCharacter(CustomerGuid, (Guid)playerLoginAndCreateSession.UserSessionGuid!, Accounts[0].Character?.CharacterName, Accounts[0].Character?.CharacterClass);
        Assert.Multiple(() =>
        {
            Assert.That(createCharacter.ClassName, Is.EqualTo(Accounts[0].Character?.CharacterClass));
            Assert.That(createCharacter.CharacterName, Is.EqualTo(Accounts[0].Character?.CharacterName));
        });
        MapInstances mapInstance = await CharRepo.CheckMapInstanceStatus(CustomerGuid, 0);
        Assert.That(mapInstance.Status, Is.EqualTo(0));

        JoinMapByCharName mapInstance1 = await CharRepo.JoinMapByCharName(CustomerGuid, Accounts[0].Character?.CharacterName, "ThirdPersonExampleMap", 0);
        Assert.That(mapInstance1.MapInstanceID, Is.GreaterThan(0));
        await CharRepo.AddCharacterToMapInstanceByCharName(CustomerGuid, Accounts[0].Character?.CharacterName, mapInstance1.MapInstanceID);

        mapInstance = await CharRepo.CheckMapInstanceStatus(CustomerGuid, mapInstance1.MapInstanceID);
        Assert.That(mapInstance.Status, Is.EqualTo(1));

        await InstanceRepo.SetZoneInstanceStatus(CustomerGuid, mapInstance1.MapInstanceID, 2);

        mapInstance = await CharRepo.CheckMapInstanceStatus(CustomerGuid, mapInstance1.MapInstanceID);
        Assert.That(mapInstance.Status, Is.EqualTo(2));

        await InstanceRepo.UpdateNumberOfPlayers(CustomerGuid, mapInstance1.MapInstanceID, 5);
        Assert.That(output.Success);

        IEnumerable<GetZoneInstancesForWorldServer> instances = await InstanceRepo.GetZoneInstancesForWorldServer(CustomerGuid, worldServerId);
        foreach (GetZoneInstancesForWorldServer instance in instances)
        {
            if (instance.MapInstanceID == mapInstance1.MapInstanceID)
            {
                Assert.That(instance.NumberOfReportedPlayers, Is.EqualTo(5));
            }
        }
    }

    [Test]
    public async Task TwoPlayersSameMap()
    {
        // Create WorldServer
        SuccessAndErrorMessage output = await InstanceRepo.RegisterLauncher(CustomerGuid, LauncherGuid.ToString(), "127.0.0.1", 1, "127.0.0.1", 7077);
        Assert.That(output.Success);

        var worldServerId = await InstanceRepo.StartWorldServer(CustomerGuid, LauncherGuid.ToString());
        Assert.That(worldServerId, Is.GreaterThan(0));

        // 1st Player Login First
        PlayerLoginAndCreateSession playerLoginAndCreateSession1 = await UserRepo.LoginAndCreateSession(CustomerGuid, Accounts[0].User?.Email, Accounts[0].User?.Password);
        Assert.That(playerLoginAndCreateSession1.Authenticated);

        // 1st Player Create Character
        CreateCharacter createCharacter1 = await UserRepo.CreateCharacter(CustomerGuid, (Guid)playerLoginAndCreateSession1.UserSessionGuid!, Accounts[0].Character?.CharacterName, Accounts[0].Character?.CharacterClass);
        Assert.Multiple(() =>
        {
            Assert.That(createCharacter1.ClassName, Is.EqualTo(Accounts[0].Character?.CharacterClass));
            Assert.That(createCharacter1.CharacterName, Is.EqualTo(Accounts[0].Character?.CharacterName));
        });
        JoinMapByCharName mapInstance1 = await CharRepo.JoinMapByCharName(CustomerGuid, Accounts[0].Character?.CharacterName, "ThirdPersonExampleMap", 0);
        Assert.That(mapInstance1.MapInstanceID, Is.GreaterThan(0));

        output = await InstanceRepo.SetZoneInstanceStatus(CustomerGuid, mapInstance1.MapInstanceID, 2);
        Assert.That(output.Success);

        await CharRepo.AddCharacterToMapInstanceByCharName(CustomerGuid, Accounts[0].Character?.CharacterName, mapInstance1.MapInstanceID);

        // 2nd Player Login First
        PlayerLoginAndCreateSession playerLoginAndCreateSession2 = await UserRepo.LoginAndCreateSession(CustomerGuid, Accounts[1].User?.Email, Accounts[1].User?.Password);
        Assert.That(playerLoginAndCreateSession2.Authenticated);

        // 2nd Player Create Character
        CreateCharacter createCharacter2 = await UserRepo.CreateCharacter(CustomerGuid, (Guid)playerLoginAndCreateSession2.UserSessionGuid!, Accounts[1].Character?.CharacterName, Accounts[1].Character?.CharacterClass);
        Assert.Multiple(() =>
        {
            Assert.That(createCharacter2.ClassName, Is.EqualTo(Accounts[1].Character?.CharacterClass));
            Assert.That(createCharacter2.CharacterName, Is.EqualTo(Accounts[1].Character?.CharacterName));
        });
        JoinMapByCharName mapInstance2 = await CharRepo.JoinMapByCharName(CustomerGuid, Accounts[1].Character?.CharacterName, "ThirdPersonExampleMap", 0);
        Assert.That(mapInstance2.MapInstanceID, Is.EqualTo(mapInstance1.MapInstanceID));
        await CharRepo.AddCharacterToMapInstanceByCharName(CustomerGuid, Accounts[0].Character?.CharacterName, mapInstance2.MapInstanceID);
    }

    [Test]
    public async Task TwoPlayersSameMapByConnect()
    {
        // Create WorldServer
        SuccessAndErrorMessage output = await InstanceRepo.RegisterLauncher(CustomerGuid, LauncherGuid.ToString(), "127.0.0.1", 1, "127.0.0.1", 7077);
        Assert.That(output.Success);

        var worldServerId = await InstanceRepo.StartWorldServer(CustomerGuid, LauncherGuid.ToString());
        Assert.That(worldServerId, Is.GreaterThan(0));

        // 1st Player Login First
        PlayerLoginAndCreateSession playerLoginAndCreateSession1 = await UserRepo.LoginAndCreateSession(CustomerGuid, Accounts[0].User?.Email, Accounts[0].User?.Password);
        Assert.That(playerLoginAndCreateSession1.Authenticated);

        // 1st Player Create Character
        CreateCharacter createCharacter1 = await UserRepo.CreateCharacter(CustomerGuid, (Guid)playerLoginAndCreateSession1.UserSessionGuid!, Accounts[0].Character?.CharacterName, Accounts[0].Character?.CharacterClass);
        Assert.Multiple(() =>
        {
            Assert.That(createCharacter1.ClassName, Is.EqualTo(Accounts[0].Character?.CharacterClass));
            Assert.That(createCharacter1.CharacterName, Is.EqualTo(Accounts[0].Character?.CharacterName));
        });
        JoinMapByCharName mapInstance1 = await CharRepo.JoinMapByCharName(CustomerGuid, Accounts[0].Character?.CharacterName, "ThirdPersonExampleMap", 0);
        Assert.That(mapInstance1.MapInstanceID, Is.GreaterThan(0));

        output = await InstanceRepo.SetZoneInstanceStatus(CustomerGuid, mapInstance1.MapInstanceID, 2);
        Assert.That(output.Success);

        // 2nd Player Login First
        PlayerLoginAndCreateSession playerLoginAndCreateSession2 = await UserRepo.LoginAndCreateSession(CustomerGuid, Accounts[1].User?.Email, Accounts[1].User?.Password);
        Assert.That(playerLoginAndCreateSession2.Authenticated);

        // 2nd Player Create Character
        CreateCharacter createCharacter2 = await UserRepo.CreateCharacter(CustomerGuid, (Guid)playerLoginAndCreateSession2.UserSessionGuid!, Accounts[1].Character?.CharacterName, Accounts[1].Character?.CharacterClass);
        Assert.Multiple(() =>
        {
            Assert.That(createCharacter2.ClassName, Is.EqualTo(Accounts[1].Character?.CharacterClass));
            Assert.That(createCharacter2.CharacterName, Is.EqualTo(Accounts[1].Character?.CharacterName));
        });
        await CharRepo.AddCharacterToMapInstanceByCharName(CustomerGuid, Accounts[1].Character?.CharacterName, mapInstance1.MapInstanceID);
        GetCharByCharName character2 = await CharRepo.GetCharByCharName(CustomerGuid, Accounts[1].Character?.CharacterName);
        Assert.That(character2.MapInstanceID, Is.EqualTo(mapInstance1.MapInstanceID));
    }

    [Test]
    public async Task SpinUpInstance()
    {
        // Create WorldServer
        SuccessAndErrorMessage output = await InstanceRepo.RegisterLauncher(CustomerGuid, LauncherGuid.ToString(), "127.0.0.1", 5, "127.0.0.1", 7077);
        Assert.That(output.Success);

        var worldServerId = await InstanceRepo.StartWorldServer(CustomerGuid, LauncherGuid.ToString());
        Assert.That(worldServerId, Is.GreaterThan(0));

        // Login First
        PlayerLoginAndCreateSession playerLoginAndCreateSession = await UserRepo.LoginAndCreateSession(CustomerGuid, Accounts[0].User?.Email, Accounts[0].User?.Password);
        Assert.That(playerLoginAndCreateSession.Authenticated);

        // Create Character
        CreateCharacter createCharacter = await UserRepo.CreateCharacter(CustomerGuid, (Guid)playerLoginAndCreateSession.UserSessionGuid!, Accounts[0].Character?.CharacterName, Accounts[0].Character?.CharacterClass);
        Assert.Multiple(() =>
        {
            Assert.That(createCharacter.ClassName, Is.EqualTo(Accounts[0].Character?.CharacterClass));
            Assert.That(createCharacter.CharacterName, Is.EqualTo(Accounts[0].Character?.CharacterName));
        });
        MapInstances mapInstance = await CharRepo.SpinUpInstance(CustomerGuid, "ThirdPersonExampleMap");
        Assert.That(mapInstance.MapInstanceId, Is.GreaterThan(0));

        GetServerInstanceFromPort instanceFromPort = await InstanceRepo.GetServerInstanceFromPort(CustomerGuid, "127.0.0.1", 7077);
        Assert.That(instanceFromPort.MapName, Is.EqualTo("ThirdPersonExampleMap"));

        Thread.Sleep(120000);
        var instancesForWorld = await InstanceRepo.GetZoneInstancesForWorldServer(CustomerGuid, mapInstance.WorldServerId);
        Assert.That(instancesForWorld.First().MinutesSinceLastUpdate, Is.GreaterThan(1));
        Assert.That(instancesForWorld.First().MinutesSinceLastUpdate, Is.LessThan(3));
    }

    [Test]
    public async Task InactiveCleanup()
    {
        // Create WorldServer
        SuccessAndErrorMessage output = await InstanceRepo.RegisterLauncher(CustomerGuid, LauncherGuid.ToString(), "127.0.0.1", 5, "127.0.0.1", 7077);
        Assert.That(output.Success);

        var worldServerId = await InstanceRepo.StartWorldServer(CustomerGuid, LauncherGuid.ToString());
        Assert.That(worldServerId, Is.GreaterThan(0));

        // Login First
        PlayerLoginAndCreateSession playerLoginAndCreateSession = await UserRepo.LoginAndCreateSession(CustomerGuid, Accounts[0].User?.Email, Accounts[0].User?.Password);
        Assert.That(playerLoginAndCreateSession.Authenticated);

        // Create Character
        CreateCharacter createCharacter = await UserRepo.CreateCharacter(CustomerGuid, (Guid)playerLoginAndCreateSession.UserSessionGuid!, Accounts[0].Character?.CharacterName, Accounts[0].Character?.CharacterClass);
        Assert.Multiple(() =>
        {
            Assert.That(createCharacter.ClassName, Is.EqualTo(Accounts[0].Character?.CharacterClass));
            Assert.That(createCharacter.CharacterName, Is.EqualTo(Accounts[0].Character?.CharacterName));
        });
        JoinMapByCharName mapInstance1 = await CharRepo.JoinMapByCharName(CustomerGuid, Accounts[0].Character?.CharacterName, "ThirdPersonExampleMap", 0);
        Assert.That(mapInstance1.MapInstanceID, Is.GreaterThan(0));
        await CharRepo.AddCharacterToMapInstanceByCharName(CustomerGuid, Accounts[0].Character?.CharacterName, mapInstance1.MapInstanceID);

        output = await InstanceRepo.SetZoneInstanceStatus(CustomerGuid, mapInstance1.MapInstanceID, 2);
        Assert.That(output.Success);

        Thread.Sleep(15000);

        JoinMapByCharName mapInstance2 = await CharRepo.JoinMapByCharName(CustomerGuid, Accounts[0].Character?.CharacterName, "DungeonMap", 0);
        Assert.That(mapInstance2.MapInstanceID, Is.GreaterThan(0));
        await CharRepo.AddCharacterToMapInstanceByCharName(CustomerGuid, Accounts[0].Character?.CharacterName, mapInstance2.MapInstanceID);

        output = await InstanceRepo.SetZoneInstanceStatus(CustomerGuid, mapInstance2.MapInstanceID, 2);
        Assert.That(output.Success);

        Thread.Sleep(120000);

        JoinMapByCharName mapInstance3 = await CharRepo.JoinMapByCharName(CustomerGuid, Accounts[0].Character?.CharacterName, "Map2", 0);
        Assert.That(mapInstance3.MapInstanceID, Is.GreaterThan(0));
        await CharRepo.AddCharacterToMapInstanceByCharName(CustomerGuid, Accounts[0].Character?.CharacterName, mapInstance3.MapInstanceID);

        output = await InstanceRepo.SetZoneInstanceStatus(CustomerGuid, mapInstance3.MapInstanceID, 2);
        Assert.That(output.Success);

        Thread.Sleep(15000);

        await CharRepo.CleanUpInstances(CustomerGuid);

        MapInstances mapInstanceCheck1 = await CharRepo.CheckMapInstanceStatus(CustomerGuid, mapInstance1.MapInstanceID);
        Assert.That(mapInstanceCheck1.Status, Is.EqualTo(0));
        MapInstances mapInstanceCheck2 = await CharRepo.CheckMapInstanceStatus(CustomerGuid, mapInstance2.MapInstanceID);
        Assert.That(mapInstanceCheck2.Status, Is.EqualTo(0));
        MapInstances mapInstanceCheck3 = await CharRepo.CheckMapInstanceStatus(CustomerGuid, mapInstance3.MapInstanceID);
        Assert.That(mapInstanceCheck3.Status, Is.EqualTo(2));
    }
}
