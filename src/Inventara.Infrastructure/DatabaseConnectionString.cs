using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Inventara.Infrastructure;

public static class DatabaseConnectionString
{
    public static string Create(IConfiguration configuration, bool requirePassword = true)
    {
        var configuredConnectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection must be configured.");
        }

        var connectionStringBuilder = new SqlConnectionStringBuilder(configuredConnectionString);
        if (string.IsNullOrWhiteSpace(connectionStringBuilder.Password))
        {
            var password = configuration["MSSQL_SA_PASSWORD"];
            if (string.IsNullOrWhiteSpace(password) && requirePassword)
            {
                throw new InvalidOperationException(
                    "Set MSSQL_SA_PASSWORD using an environment variable or API User Secrets.");
            }

            if (!string.IsNullOrWhiteSpace(password))
            {
                connectionStringBuilder.Password = password;
            }
        }

        return connectionStringBuilder.ConnectionString;
    }
}
