using Microsoft.Data.SqlClient;

namespace ConvictionCore.Infrastructure.Persistence;

internal static class SqlConnectionGuard
{
    public static void RejectReservedCatalogs(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        var catalog = builder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(catalog))
        {
            return;
        }

        if (catalog.EndsWith("_Prod", StringComparison.OrdinalIgnoreCase)
            || string.Equals(catalog, "ConvictionCore_Test", StringComparison.OrdinalIgnoreCase)
            || string.Equals(catalog, "ConvictionOS_Test", StringComparison.OrdinalIgnoreCase)
            || string.Equals(catalog, "ConvictionOS_Prod", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Database catalog '{catalog}' is reserved and not authorized for this host.");
        }
    }
}
