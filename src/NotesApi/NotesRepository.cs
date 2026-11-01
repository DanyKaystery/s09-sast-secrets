using Microsoft.Data.Sqlite;

namespace NotesApi;

// Намеренно учебный код. Каждый сигнал требует проверки контекста.
public sealed class NotesRepository
{
    private readonly string _connectionString;

    public NotesRepository(string connectionString)
    {
        _connectionString = connectionString;
        Initialize();
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    private void Initialize()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS notes (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                owner TEXT NOT NULL, tag TEXT NOT NULL, text TEXT NOT NULL);
        """;
        cmd.ExecuteNonQuery();

        using var check = conn.CreateCommand();
        check.CommandText = "SELECT COUNT(*) FROM notes;";
        if (Convert.ToInt64(check.ExecuteScalar()) == 0)
        {
            Add("alice", "personal", "alice note");
            Add("bob", "personal", "bob note");
        }
    }

    // Поиск заметок по подстроке.
    public List<Note> Search(string owner, string q)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT id, owner, tag, text FROM notes " +
            "WHERE owner = '" + owner + "' AND text LIKE '%" + q + "%';";
        return Read(cmd);
    }

    // Создание заметки.
    public Note Add(string owner, string tag, string text)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO notes (owner, tag, text) VALUES ($o, $g, $t);
            SELECT last_insert_rowid();
        """;
        cmd.Parameters.AddWithValue("$o", owner);
        cmd.Parameters.AddWithValue("$g", tag);
        cmd.Parameters.AddWithValue("$t", text);
        var id = Convert.ToInt32(cmd.ExecuteScalar());
        return new Note(id, owner, tag, text);
    }

    private static List<Note> Read(SqliteCommand cmd)
    {
        var result = new List<Note>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result.Add(new Note(reader.GetInt32(0), reader.GetString(1),
                reader.GetString(2), reader.GetString(3)));
        return result;
    }
}

public record Note(int Id, string Owner, string Tag, string Text);
