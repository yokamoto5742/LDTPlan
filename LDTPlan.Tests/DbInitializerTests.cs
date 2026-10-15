using LDTPlan.Data;
using Microsoft.Data.Sqlite;

namespace LDTPlan.Tests;

public sealed class DbInitializerTests : IDisposable
{
    private readonly TestEnvironment _env = new();

    public void Dispose() => _env.Dispose();

    private void Execute(string sql)
    {
        using var connection = new SqliteConnection($"Data Source={_env.DbPath};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private bool PatientIdIndexExists()
    {
        using var connection = new SqliteConnection($"Data Source={_env.DbPath};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = $name";
        command.Parameters.AddWithValue("$name", DbInitializer.PatientIdIndexName);
        return (long)command.ExecuteScalar()! == 1;
    }

    [Fact]
    public void 新規DBには患者IDのインデックスが作られる()
    {
        Assert.True(PatientIdIndexExists());
    }

    [Fact]
    public void インデックスのない既存DBに患者IDのインデックスを追加する()
    {
        Execute($"DROP INDEX {DbInitializer.PatientIdIndexName}");

        DbInitializer.EnsurePatientIdIndex();

        Assert.True(PatientIdIndexExists());
    }

    [Fact]
    public void インデックスが作成済みでも例外にならない()
    {
        DbInitializer.EnsurePatientIdIndex();

        Assert.True(PatientIdIndexExists());
    }
}
