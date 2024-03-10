using System.Reflection;

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

    public static async Task MSSQLSetupAbilities(IDbConnection connection, Guid customerGuid)
    {
        // Setup Test AbilityType and Ability
        using (connection)
        {
            var p = new DynamicParameters();
            p.Add("@CustomerGUID", customerGuid);
            p.Add("@AbilityTypeID", -1);
            p.Add("@AbilityTypeName", "Test");

            await connection.ExecuteAsync("AddOrUpdateAbilityType",
                p,
                commandType: CommandType.StoredProcedure);

            p = new DynamicParameters();
            p.Add("@CustomerGUID", customerGuid);

            var output = await connection.QueryAsync<AbilityTypes>("GetAbilityTypes",
                p,
                commandType: CommandType.StoredProcedure);

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

            await connection.ExecuteAsync("AddOrUpdateAbility",
                p,
                commandType: CommandType.StoredProcedure);
        }
    }

    public static async Task MySQLSetupAbilities(IDbConnection connection, Guid customerGuid)
    {
        // Setup Test AbilityType and Ability
        using (connection)
        {
            var p = new DynamicParameters();
            p.Add("@CustomerGUID", customerGuid);
            p.Add("@AbilityTypeID", -1);
            p.Add("@AbilityTypeName", "Test");

            await connection.ExecuteAsync("call AddOrUpdateAbilityType(@CustomerGUID, @AbilityTypeID, @AbilityTypeName)",
                p,
                commandType: CommandType.Text);

            p = new DynamicParameters();
            p.Add("@CustomerGUID", customerGuid);

            var output = await connection.QueryAsync<AbilityTypes>("call GetAbilityTypes(@CustomerGUID)",
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

            await connection.ExecuteAsync("call AddOrUpdateAbility(@CustomerGUID, @AbilityID, @AbilityName, @AbilityTypeID, @TextureToUseForIcon, @Class, @Race, @GameplayAbilityClassName, @AbilityCustomJSON)",
                p,
                commandType: CommandType.Text);
        }
    }

    public static async Task PostgresSetupAbilities(IDbConnection connection, Guid customerGuid)
    {
        // Setup Test AbilityType and Ability
        using (connection)
        {
            var p = new DynamicParameters();
            p.Add("@CustomerGUID", customerGuid);
            p.Add("@AbilityTypeID", -1);
            p.Add("@AbilityTypeName", "Test");

            await connection.ExecuteAsync("call AddOrUpdateAbilityType(@CustomerGUID, @AbilityTypeID, @AbilityTypeName)",
                p,
                commandType: CommandType.Text);

            p = new DynamicParameters();
            p.Add("@CustomerGUID", customerGuid);

            var output = await connection.QueryAsync<AbilityTypes>("select * from GetAbilityTypes(@CustomerGUID)",
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

            await connection.ExecuteAsync("call AddOrUpdateAbility(@CustomerGUID, @AbilityID, @AbilityName, @AbilityTypeID, @TextureToUseForIcon, @Class, @Race, @GameplayAbilityClassName, @AbilityCustomJSON)",
                p,
                commandType: CommandType.Text);
        }
    }

    public static async Task RemoveUser(IDbConnection connection, Guid customerGuid, PlayerLoginAndCreateSession playerLoginAndCreateSession)
    {
        var userGuid = await connection.ExecuteScalarAsync<Guid>(@"SELECT UserGUID FROM UserSessions WHERE UserSessionGUID = @UserSessionGUID;", new { CustomerGUID = customerGuid, UserSessionGUID = (Guid)playerLoginAndCreateSession.UserSessionGuid! });
        await connection.ExecuteAsync(@"DELETE FROM UsersInQueue WHERE CustomerGUID = @CustomerGUID AND UserGUID = @UserGUID;", new { CustomerGUID = customerGuid, UserGUID = userGuid });
        await connection.ExecuteAsync(@"DELETE FROM UserSessions WHERE CustomerGUID = @CustomerGUID AND UserGUID = @UserGUID;", new { CustomerGUID = customerGuid, UserGUID = userGuid });
        await connection.ExecuteAsync(@"DELETE FROM Users WHERE CustomerGUID = @CustomerGUID AND UserGUID = @UserGUID;", new { CustomerGUID = customerGuid, UserGUID = userGuid });
    }

    public static async Task RemoveAbilities(IDbConnection connection, Guid customerGuid, Guid launcherGuid)
    {
        // Remove Ability and AbilityType
        await connection.ExecuteAsync(@"DELETE FROM Abilities WHERE CustomerGUID = @CustomerGUID AND AbilityName='Test'", new { CustomerGUID = customerGuid });
        await connection.ExecuteAsync(@"DELETE FROM AbilityTypes WHERE CustomerGUID = @CustomerGUID AND AbilityTypeName='Test'", new { CustomerGUID = customerGuid });

        // Remove World Servers and Cleanup Logs
        await connection.ExecuteAsync(@"DELETE FROM WorldServers WHERE CustomerGUID = @CustomerGUID AND ZoneServerGUID = @LauncherGuid", new { CustomerGUID = customerGuid, LauncherGuid = launcherGuid });
        await connection.ExecuteAsync(@"DELETE FROM MapInstances WHERE CustomerGUID = @CustomerGUID", new { CustomerGUID = customerGuid });
        await connection.ExecuteAsync(@"DELETE FROM DebugLog WHERE CustomerGUID = @CustomerGUID", new { CustomerGUID = customerGuid });
    }

    public static async Task RemoveCustomData(IDbConnection connection, Guid customerGuid)
    {
        // Remove Custom Data
        await connection.ExecuteAsync(@"DELETE FROM CustomCharacterData WHERE CustomerGUID = @CustomerGUID", new { CustomerGUID = customerGuid });
        await connection.ExecuteAsync(@"DELETE FROM GlobalData WHERE CustomerGUID = @CustomerGUID", new { CustomerGUID = customerGuid });
    }
}
