using System.Diagnostics;
using System.Text;
using Cocona;
using MassMailReader;
using Microsoft.Data.Sqlite;
using Spectre.Console;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

SQLitePCL.Batteries_V2.Init();
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var app = CoconaLiteApp.Create();

app.AddCommand("read", async (
    [Option('d', Description = "Root directory where all email files are")] string directory,
    [Option('p', Description = "Specify if you want to run read in paralel to incrase peformance")] bool parallel = false
    ) =>
{
    directory = Path.GetFullPath(directory);

    var sw = Stopwatch.StartNew();

    AnsiConsole.WriteLine($"Reading from {directory}");

    var dbPath = Path.Combine(directory, "mails.sqlite");
    if (File.Exists(dbPath))
    {
        File.Delete(dbPath);
    }

    using var conn = new SqliteConnection($"Data Source={dbPath}");
    await conn.OpenAsync();
    await Database.CreateDatabase(conn);

    var fileQuery = Directory.GetFiles(directory, "*.eml", SearchOption.AllDirectories);
    var totalFiles = fileQuery.Length;

    if (parallel)
    {
        var tasks = new List<Task>();
        var splitIndex = 0;
        var splits = ParalelizationSplit.SplitForParalelization(fileQuery);
        AnsiConsole.WriteLine($"Splitting to {splits.Count} parts");
        foreach (var mailFileSplit in splits)
        {
            var splitLetter = (char)('A' + splitIndex);
            tasks.Add(Task.Run(async () =>
            {
                var mailNumber = 1;
                foreach (var mailFileName in mailFileSplit)
                {
                    var ident = $"{splitLetter}:{mailNumber:N0}/{mailFileSplit.Count:N0}";
                    await ProcessMailFile(sw, conn, ident, mailFileName);
                    mailNumber++;
                }
            }));
            splitIndex++;
        }
        await Task.WhenAll(tasks);
    }
    else
    {
        var mailNumber = 1;
        foreach (var mailFileName in fileQuery)
        {
            var ident = $"{mailNumber:N0}/{totalFiles:N0}";
            await ProcessMailFile(sw, conn, ident, mailFileName);
            mailNumber++;
        }
    }

    AnsiConsole.WriteLine($"Reading finished");

    static async Task ProcessMailFile(Stopwatch sw, SqliteConnection conn, string ident, string mailFileName)
    {
        AnsiConsole.MarkupLineInterpolated($"[white]{sw.Elapsed:c} Reading file {ident}: '{mailFileName}'[/]");
        var mail = await MailReader.ReadMailAsync(ident, mailFileName);

        // Save the Mail object to Sqlite database
        await Database.SaveMailToDatabaseAsync(conn, mailFileName, mail);

        foreach (var attachment in mail.Attachements)
        {
            await Database.SaveAttachment(conn, mailFileName, attachment);
        }
    }
})
    .WithDescription(@"Read all .elm files from directory to sqlite database for later use.
If database already exist, it is overwritten.")
    .WithAliases("r", "parse");

app.AddCommand("search", async (
    [Option('d', Description = "Root directory where all email files and database are")] string directory
    ) =>
{
    directory = Path.GetFullPath(directory);

    AnsiConsole.WriteLine($"Reading from {directory}");

    var dbPath = Path.Combine(directory, "mails.sqlite");
    if (!File.Exists(dbPath))
    {
        AnsiConsole.MarkupLine("[yellow]This directory does not have database yes. Run `read` action first[/]");
        return;
    }

    while (true)
    {
        var query = AnsiConsole.Ask("What are you looking for?", defaultValue: "");
        if (string.IsNullOrWhiteSpace(query))
        {
            AnsiConsole.MarkupLine("[yellow]You must provide a search query[/]");
            return;
        }

        using var conn = new SqliteConnection($"Data Source={dbPath}");
        await conn.OpenAsync();

        // Save the Mail object to Sqlite database
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT Subject, Content, Path, Type, Date
FROM Mails 
WHERE Subject MATCH @Query OR Content MATCH @Query
LIMIT 250";

        cmd.Parameters.AddWithValue("@Query", query);

        using var rdr = await cmd.ExecuteReaderAsync();

        AnsiConsole.WriteLine(rdr.RecordsAffected.ToString());

        if (!rdr.HasRows)
        {
            AnsiConsole.MarkupLine("[yellow]No results found[/]");
            continue;
        }

        var table = new Table();
        table.AddColumn("Path");
        table.AddColumn("Subject");
        table.AddColumn("Type");
        table.AddColumn("Content");
        table.AddColumn("Date");

        while (await rdr.ReadAsync())
        {
            table.AddRow(
                Markup.Escape(rdr.GetString(2)),
                Markup.Escape(rdr.GetString(0)),
                Markup.Escape(rdr.GetString(3)),
                Markup.Escape(rdr.GetString(1).Substring(0, Math.Min(250, rdr.GetString(1).Length))),
                Markup.Escape(rdr.GetString(4))
            );
        }

        AnsiConsole.Write(table);
    }
})
    .WithDescription(@"Query database created by `read` command.
You can make as many queries as you want.
Amount of results is limited to 250.
Submit empty query to exit.")
    .WithAliases("filter", "find", "q", "f");

await app.RunAsync();
