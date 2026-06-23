using System.Globalization;
using Microsoft.Data.Sqlite;

namespace LuluPet.Core.Storage;

public sealed class SqlitePetRepository
{
    private const long SingletonId = 1;
    private readonly string _connectionString;

    public SqlitePetRepository(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("Database path is required.", nameof(databasePath));
        }

        DatabasePath = databasePath;
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath
        }.ToString();
    }

    public string DatabasePath { get; }

    public void Initialize()
    {
        var directory = Path.GetDirectoryName(DatabasePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var connection = OpenConnection();
        ExecuteNonQuery(connection, "PRAGMA journal_mode=WAL;");
        ExecuteNonQuery(connection, "PRAGMA foreign_keys=ON;");
        ExecuteNonQuery(connection, SchemaSql);
        EnsureSingletonRows(connection);
    }

    public PetProfile LoadProfile()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT name, created_at_utc, updated_at_utc, interaction_count
            FROM pet_profile
            WHERE id = 1;
            """;

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            throw new InvalidOperationException("Pet profile is not initialized.");
        }

        return new PetProfile(
            reader.GetString(0),
            ParseDateTimeOffset(reader.GetString(1)),
            ParseDateTimeOffset(reader.GetString(2)),
            reader.GetInt64(3));
    }

    public PetStatus LoadStatus()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT state, window_left, window_top, updated_at_utc
            FROM pet_status
            WHERE id = 1;
            """;

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            throw new InvalidOperationException("Pet status is not initialized.");
        }

        return new PetStatus(
            reader.GetString(0),
            reader.GetDouble(1),
            reader.GetDouble(2),
            ParseDateTimeOffset(reader.GetString(3)));
    }

    public void SaveStatus(PetStatus status)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO pet_status (id, state, window_left, window_top, updated_at_utc)
            VALUES (1, $state, $window_left, $window_top, $updated_at_utc)
            ON CONFLICT(id) DO UPDATE SET
                state = excluded.state,
                window_left = excluded.window_left,
                window_top = excluded.window_top,
                updated_at_utc = excluded.updated_at_utc;
            """;
        command.Parameters.AddWithValue("$state", NormalizeText(status.State, "Idle"));
        command.Parameters.AddWithValue("$window_left", status.WindowLeft);
        command.Parameters.AddWithValue("$window_top", status.WindowTop);
        command.Parameters.AddWithValue("$updated_at_utc", FormatDateTimeOffset(status.UpdatedAtUtc));
        command.ExecuteNonQuery();
    }

    public long RecordInteraction(PetInteraction interaction)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        using var insertCommand = connection.CreateCommand();
        insertCommand.Transaction = transaction;
        insertCommand.CommandText = """
            INSERT INTO interaction_log (
                interaction_type,
                message,
                state,
                window_left,
                window_top,
                created_at_utc
            )
            VALUES (
                $interaction_type,
                $message,
                $state,
                $window_left,
                $window_top,
                $created_at_utc
            );
            """;
        insertCommand.Parameters.AddWithValue(
            "$interaction_type",
            NormalizeText(interaction.InteractionType, "unknown"));
        insertCommand.Parameters.AddWithValue("$message", (object?)interaction.Message ?? DBNull.Value);
        insertCommand.Parameters.AddWithValue("$state", NormalizeText(interaction.State, "Idle"));
        insertCommand.Parameters.AddWithValue("$window_left", interaction.WindowLeft);
        insertCommand.Parameters.AddWithValue("$window_top", interaction.WindowTop);
        insertCommand.Parameters.AddWithValue("$created_at_utc", FormatDateTimeOffset(interaction.CreatedAtUtc));
        insertCommand.ExecuteNonQuery();

        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = transaction;
        updateCommand.CommandText = """
            UPDATE pet_profile
            SET interaction_count = interaction_count + 1,
                updated_at_utc = $updated_at_utc
            WHERE id = 1;
            """;
        updateCommand.Parameters.AddWithValue("$updated_at_utc", FormatDateTimeOffset(interaction.CreatedAtUtc));
        updateCommand.ExecuteNonQuery();

        using var idCommand = connection.CreateCommand();
        idCommand.Transaction = transaction;
        idCommand.CommandText = "SELECT last_insert_rowid();";
        var id = (long)(idCommand.ExecuteScalar() ?? 0L);

        transaction.Commit();
        return id;
    }

    public IReadOnlyList<PetInteraction> LoadRecentInteractions(int limit)
    {
        if (limit <= 0)
        {
            return Array.Empty<PetInteraction>();
        }

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT interaction_type, message, state, window_left, window_top, created_at_utc
            FROM interaction_log
            ORDER BY id DESC
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$limit", limit);

        using var reader = command.ExecuteReader();
        var interactions = new List<PetInteraction>();

        while (reader.Read())
        {
            interactions.Add(new PetInteraction(
                reader.GetString(0),
                reader.IsDBNull(1) ? null : reader.GetString(1),
                reader.GetString(2),
                reader.GetDouble(3),
                reader.GetDouble(4),
                ParseDateTimeOffset(reader.GetString(5))));
        }

        return interactions;
    }

    private static string SchemaSql => """
        CREATE TABLE IF NOT EXISTS pet_profile (
            id INTEGER PRIMARY KEY CHECK (id = 1),
            name TEXT NOT NULL,
            created_at_utc TEXT NOT NULL,
            updated_at_utc TEXT NOT NULL,
            interaction_count INTEGER NOT NULL DEFAULT 0
        );

        CREATE TABLE IF NOT EXISTS pet_status (
            id INTEGER PRIMARY KEY CHECK (id = 1),
            state TEXT NOT NULL,
            window_left REAL NOT NULL,
            window_top REAL NOT NULL,
            updated_at_utc TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS interaction_log (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            interaction_type TEXT NOT NULL,
            message TEXT NULL,
            state TEXT NOT NULL,
            window_left REAL NOT NULL,
            window_top REAL NOT NULL,
            created_at_utc TEXT NOT NULL
        );

        CREATE INDEX IF NOT EXISTS ix_interaction_log_created_at_utc
            ON interaction_log (created_at_utc);
        """;

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private static void EnsureSingletonRows(SqliteConnection connection)
    {
        var now = FormatDateTimeOffset(DateTimeOffset.UtcNow);

        using var profileCommand = connection.CreateCommand();
        profileCommand.CommandText = """
            INSERT OR IGNORE INTO pet_profile (
                id,
                name,
                created_at_utc,
                updated_at_utc,
                interaction_count
            )
            VALUES (1, 'Lulu', $now, $now, 0);
            """;
        profileCommand.Parameters.AddWithValue("$now", now);
        profileCommand.ExecuteNonQuery();

        using var statusCommand = connection.CreateCommand();
        statusCommand.CommandText = """
            INSERT OR IGNORE INTO pet_status (
                id,
                state,
                window_left,
                window_top,
                updated_at_utc
            )
            VALUES (1, 'Idle', 1200, 650, $now);
            """;
        statusCommand.Parameters.AddWithValue("$now", now);
        statusCommand.ExecuteNonQuery();
    }

    private static void ExecuteNonQuery(SqliteConnection connection, string commandText)
    {
        using var command = connection.CreateCommand();
        command.CommandText = commandText;
        command.ExecuteNonQuery();
    }

    private static string NormalizeText(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string FormatDateTimeOffset(DateTimeOffset value)
    {
        return value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
    }

    private static DateTimeOffset ParseDateTimeOffset(string value)
    {
        return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }
}
