using System;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using grate.Infrastructure;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace grate.Migration;

public class MariaDbDatabase : AnsiSqlDatabase
{
    public MariaDbDatabase(ILogger<MariaDbDatabase> logger) 
        : base(logger, new MariaDbSyntax())
    { }

    public override bool SupportsDdlTransactions => false;
    protected override bool SupportsSchemas => false;
    protected override DbConnection GetSqlConnection(string? connectionString) => new MySqlConnection(connectionString);

    public override async Task RestoreDatabase(string backupPath)
    {
        try
        {
            await OpenConnection();
            Logger.LogInformation("Restoring {DbName} database on {Server} server from path {Path}.", DatabaseName, ServerName, backupPath);
            using var s = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled);
            var sql = await File.ReadAllTextAsync(backupPath);
            await ExecuteNonQuery(Connection, sql, Config?.CommandTimeout);
            s.Complete();
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Got error: {ErrorMessage}", ex.Message);
            throw;
        }
        finally
        {
            await CloseConnection();
        }

        await WaitUntilDatabaseIsReady();

        Logger.LogInformation("Database {DbName} successfully restored from path {Path}.", DatabaseName, backupPath);
    }

    protected override Task ExecuteNonQuery(DbConnection conn, string sql, int? timeout)
    {
        var script = new MySqlScript((MySqlConnection)conn, sql);

        return script.ExecuteAsync();
    }

    public override async Task<bool> DatabaseExists()
    {
        var sql = $"SELECT SCHEMA_NAME FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = '{DatabaseName}'";
        try
        {
            await OpenConnection();
            var results = await Connection.QueryAsync<string>(sql, commandType: CommandType.Text);
            return results.Any();
        }
        catch (DbException ex)
        {
            Logger.LogDebug(ex, "An unexpected error occurred performing the CheckDatabaseExists check: {ErrorMessage}", ex.Message);
            return false; // base method also returns false on any DbException
        }
        finally
        {
            await CloseConnection();
        }
    }
}
