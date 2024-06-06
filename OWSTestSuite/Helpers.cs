using System.Reflection;
using OWSData.SQL;

namespace OWSTestSuite;

public static class Helpers
{
    public static string PropertyList(object obj)
    {
        var props = obj.GetType().GetProperties();
        var sb = new StringBuilder();
        foreach (PropertyInfo p in props)
        {
            sb.AppendLine("\t" + p.Name + ": " + p.GetValue(obj, null));
        }
        return sb.ToString();
    }

    public static async Task SetupAbilities(IDbConnection connection, Guid customerGuid)
    {
        // Setup Test AbilityType and Ability
        using (connection)
        {
            await RemoveAbilities(connection, customerGuid);
            await RemoveUsers(connection, customerGuid);
            await RemoveServers(connection, customerGuid);
            await RemoveCustomData(connection, customerGuid);
            var p = new DynamicParameters();
            p.Add("@CustomerGUID", customerGuid);
            p.Add("@AbilityTypeID", -1);
            p.Add("@AbilityTypeName", "Test");

            await connection.ExecuteAsync("INSERT INTO AbilityTypes (CustomerGUID, AbilityTypeName) VALUES (@CustomerGUID, @AbilityTypeName)",
                p,
                commandType: CommandType.Text);

            p = new DynamicParameters();
            p.Add("@CustomerGUID", customerGuid);

            var output = await connection.QueryAsync<AbilityTypes>("SELECT *, (SELECT COUNT(*) FROM Abilities AB WHERE AB.AbilityTypeID = ABT.AbilityTypeID) AS NumberOfAbilities FROM AbilityTypes ABT WHERE ABT.CustomerGUID = @CustomerGUID;",
                p,
                commandType: CommandType.Text);

            var abilityTypeId = -1;
            foreach (AbilityTypes abilityType in output)
            {
                if (abilityType.AbilityTypeName == "Test")
                {
                    abilityTypeId = abilityType.AbilityTypeId;
                }
            }

            p = new DynamicParameters();
            p.Add("@CustomerGUID", customerGuid);
            p.Add("@AbilityID", -1);
            p.Add("@AbilityName", "Test");
            p.Add("@AbilityTypeID", abilityTypeId);
            p.Add("@TextureToUseForIcon", "");
            p.Add("@Class", -1);
            p.Add("@Race", -1);
            p.Add("@GameplayAbilityClassName", "");
            p.Add("@AbilityCustomJSON", "");

            await connection.ExecuteAsync("INSERT INTO Abilities (CustomerGUID, AbilityName, AbilityTypeID, TextureToUseForIcon, Class, Race, GameplayAbilityClassName, AbilityCustomJSON) VALUES (@CustomerGUID, @AbilityName, @AbilityTypeID, @TextureToUseForIcon, @Class, @Race, @GameplayAbilityClassName, @AbilityCustomJSON);",
                p,
                commandType: CommandType.Text);
        }
    }

    public static async Task RemoveUsers(IDbConnection connection, Guid customerGuid)
    {
        var characterIds = await connection.QueryAsync<Int32>(@"SELECT CharacterID FROM Characters WHERE CustomerGUID = @CustomerGUID;", new { CustomerGUID = customerGuid });
        foreach (var characterId in characterIds)
        {
            await connection.ExecuteAsync(GenericQueries.RemoveCharacterFromAllInstances, new  { CustomerGUID = customerGuid, CharacterID = characterId }, commandType: CommandType.Text);
            await connection.ExecuteAsync(GenericQueries.RemoveCharacterAbilities, new  { CustomerGUID = customerGuid, CharacterID = characterId }, commandType: CommandType.Text);
            await connection.ExecuteAsync(GenericQueries.RemoveCharacterAbilityBars, new  { CustomerGUID = customerGuid, CharacterID = characterId }, commandType: CommandType.Text);
            await connection.ExecuteAsync(GenericQueries.RemoveCharacterHasAbilities, new  { CustomerGUID = customerGuid, CharacterID = characterId }, commandType: CommandType.Text);
            await connection.ExecuteAsync(GenericQueries.RemoveCharacterHasItems, new  { CustomerGUID = customerGuid, CharacterID = characterId }, commandType: CommandType.Text);
            await connection.ExecuteAsync(GenericQueries.RemoveCharacterInventoryItems, new  { CustomerGUID = customerGuid, CharacterID = characterId }, commandType: CommandType.Text);
            await connection.ExecuteAsync(GenericQueries.RemoveCharacterInventory, new  { CustomerGUID = customerGuid, CharacterID = characterId }, commandType: CommandType.Text);
            await connection.ExecuteAsync(GenericQueries.RemoveCharacterGroupUsers, new  { CustomerGUID = customerGuid, CharacterID = characterId }, commandType: CommandType.Text);
            await connection.ExecuteAsync(GenericQueries.RemoveCharacterCharacterData, new  { CustomerGUID = customerGuid, CharacterID = characterId }, commandType: CommandType.Text);
            await connection.ExecuteAsync(GenericQueries.RemoveCharacterFromPlayerGroupCharacters, new  { CustomerGUID = customerGuid, CharacterID = characterId }, commandType: CommandType.Text);
            await connection.ExecuteAsync(GenericQueries.RemoveCharacter, new  { CustomerGUID = customerGuid, CharacterID = characterId }, commandType: CommandType.Text);
        }

        var userGuids = await connection.QueryAsync<Guid>(@"SELECT UserGUID FROM UserSessions WHERE CustomerGUID = @CustomerGUID;", new { CustomerGUID = customerGuid });
        foreach (var userGuid in userGuids)
        {
            await connection.ExecuteAsync(@"DELETE FROM UsersInQueue WHERE CustomerGUID = @CustomerGUID AND UserGUID = @UserGUID;", new { CustomerGUID = customerGuid, UserGUID = userGuid });
            await connection.ExecuteAsync(@"DELETE FROM UserSessions WHERE CustomerGUID = @CustomerGUID AND UserGUID = @UserGUID;", new { CustomerGUID = customerGuid, UserGUID = userGuid });
            await connection.ExecuteAsync(@"DELETE FROM Users WHERE CustomerGUID = @CustomerGUID AND UserGUID = @UserGUID;", new { CustomerGUID = customerGuid, UserGUID = userGuid });
        }
    }

    public static async Task RemoveAbilities(IDbConnection connection, Guid customerGuid)
    {
        // Remove Ability and AbilityType
        await connection.ExecuteAsync(@"DELETE FROM Abilities WHERE CustomerGUID = @CustomerGUID AND AbilityName='Test'", new { CustomerGUID = customerGuid });
        await connection.ExecuteAsync(@"DELETE FROM AbilityTypes WHERE CustomerGUID = @CustomerGUID AND AbilityTypeName='Test'", new { CustomerGUID = customerGuid });
    }

    public static async Task RemoveServers(IDbConnection connection, Guid customerGuid)
    {
        // Remove World Servers and Cleanup Logs
        await connection.ExecuteAsync(@"DELETE FROM WorldServers WHERE CustomerGUID = @CustomerGUID", new { CustomerGUID = customerGuid });
        await connection.ExecuteAsync(@"DELETE FROM MapInstances WHERE CustomerGUID = @CustomerGUID", new { CustomerGUID = customerGuid });
    }

    public static async Task RemoveCustomData(IDbConnection connection, Guid customerGuid)
    {
        // Remove Custom Data
        await connection.ExecuteAsync(@"DELETE FROM CustomCharacterData WHERE CustomerGUID = @CustomerGUID", new { CustomerGUID = customerGuid });
        await connection.ExecuteAsync(@"DELETE FROM GlobalData WHERE CustomerGUID = @CustomerGUID", new { CustomerGUID = customerGuid });
    }
}
