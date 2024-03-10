using Microsoft.Extensions.Configuration;
using OWSTestSuite.Models;
using User = OWSTestSuite.Models.User;

namespace OWSTestSuite;

public class BaseTest
{
    protected readonly IConfiguration Configuration;
    protected Guid CustomerGuid;
    protected Guid LauncherGuid;
    protected string BadPassword = null!;
    protected readonly IList<Account> Accounts = new List<Account>();

    public BaseTest()
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(@"appsettings.json", optional: false, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();
    }

    [SetUp]
    public void Init()
    {
        CustomerGuid = new Guid("00000000-0000-0000-0000-000000000001");
        LauncherGuid = new Guid("00000000-0000-0000-0000-000000000000");
        BadPassword = "12345";

        Accounts.Clear();

        Accounts.Add(new Account
        {
            User = new User
            {
                Email = "urist@mc.test",
                Password = "strongPassword",
                FirstName = "Urist",
                LastName = "McTest"
            },
            Character = new Character
            {
                CharacterName = "Urist McTest",
                CharacterClass = "MaleWarrior"
            }
        });

        Accounts.Add(new Account
        {
            User = new User
            {
                Email = "urist@mc.friend",
                Password = "strongPassword",
                FirstName = "Urist",
                LastName = "McFriend"
            },
            Character = new Character
            {
                CharacterName = "Urist McFriend",
                CharacterClass = "MaleWarrior"
            }
        });
    }
}
