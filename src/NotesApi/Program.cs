using NotesApi;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton(_ =>
{
    var data = Path.Combine(AppContext.BaseDirectory, "lab-data");
    Directory.CreateDirectory(data);
    var connection = Environment.GetEnvironmentVariable("NOTES_DB_CONNECTION")
        ?? new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
        { DataSource = Path.Combine(data, "notes.db") }.ToString();
    return new NotesRepository(connection);
});
var app = builder.Build();
// X-User — учебный переключатель пользователя, не механизм аутентификации.
static string CurrentUser(HttpRequest req) => req.Headers["X-User"].FirstOrDefault() ?? "alice";
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/notes/search", (HttpRequest req, string q, NotesRepository repo) =>
    Results.Ok(repo.Search(CurrentUser(req), q)));
app.MapPost("/notes", async (HttpRequest req, NotesRepository repo) =>
{
    var input = await req.ReadFromJsonAsync<NoteInput>();
    if (input is null || string.IsNullOrWhiteSpace(input.Text))
        return Results.BadRequest(new { error = "text is required" });
    var note = repo.Add(CurrentUser(req), input.Tag ?? "personal", input.Text);
    return Results.Created($"/notes/{note.Id}", note);
});
app.Run();
public record NoteInput(string Text, string? Tag);
public partial class Program { }
