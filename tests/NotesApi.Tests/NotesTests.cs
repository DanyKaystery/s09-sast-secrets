using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Data.Sqlite;
using Xunit;
namespace NotesApi.Tests;
public sealed class NotesTests : IDisposable
{
    private readonly DemoFactory _factory = new();
    private HttpClient As(string user)
    {
        var c = _factory.CreateClient();
        c.DefaultRequestHeaders.Add("X-User", user);
        return c;
    }
    [Fact]
    public async Task Health_Ok()
    {
        using var c = As("alice");
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/health")).StatusCode);
    }
    [Fact]
    public async Task Search_Own_Note_Is_Returned()
    {
        using var c = As("alice");
        var notes = await c.GetFromJsonAsync<List<Note>>("/notes/search?q=note");
        Assert.Single(notes!);
        Assert.Equal("alice note", notes![0].Text);
        Assert.Equal("alice", notes[0].Owner);
    }
    [Fact]
    public async Task Search_No_Match_Returns_Empty()
    {
        using var c = As("alice");
        Assert.Empty((await c.GetFromJsonAsync<List<Note>>("/notes/search?q=not-present"))!);
    }
    [Fact]
    public async Task Add_Text_With_Apostrophe_Works()
    {
        using var c = As("alice");
        var r = await c.PostAsJsonAsync("/notes", new { text = "reader's note", tag = "work" });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var added = await r.Content.ReadFromJsonAsync<Note>();
        Assert.Equal("reader's note", added!.Text);
        var found = await c.GetFromJsonAsync<List<Note>>("/notes/search?q=reader");
        Assert.Single(found!);
        Assert.Equal(added.Id, found![0].Id);
    }
    // Снимите Skip на S09 до изменения Search; сначала получите красный тест.
    [Fact(Skip = "S09: включить до исправления SQL.")]
    public async Task Search_Injection_Does_Not_Return_Bobs_Note()
    {
        using var bob = As("bob");
        var target = await bob.GetFromJsonAsync<List<Note>>("/notes/search?q=note");
        Assert.Single(target!);
        var targetId = target![0].Id;
        using var alice = As("alice");
        var payload = "%' OR 1=1 -- ";
        var result = await alice.GetFromJsonAsync<List<Note>>(
            "/notes/search?q=" + Uri.EscapeDataString(payload));
        Assert.NotNull(result);
        Assert.DoesNotContain(result!, n => n.Id == targetId);
        Assert.All(result!, n => Assert.Equal("alice", n.Owner));
    }
    public void Dispose() => _factory.Dispose();
}
internal sealed class DemoFactory : WebApplicationFactory<Program>
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "rbpo-s09-tests", Guid.NewGuid().ToString("N"));
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(_root);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<NotesRepository>();
            var connection = new SqliteConnectionStringBuilder { DataSource = Path.Combine(_root, "notes.db") }.ToString();
            services.AddSingleton(_ => new NotesRepository(connection));
        });
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        }
    }
}
