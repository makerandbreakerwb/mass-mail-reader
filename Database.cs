using Microsoft.Data.Sqlite;

namespace MassMailReader;
public static class Database
{
    public static async Task CreateDatabase(SqliteConnection conn)
    {
        var dbCreation = conn.CreateCommand();
        dbCreation.CommandText = @"CREATE VIRTUAL TABLE Mails USING fts5(Subject, Content, Path, Type, Date)";
        await dbCreation.ExecuteNonQueryAsync();
    }

    public static async Task SaveAttachment(SqliteConnection conn, string mailFileName, MailAttachement item)
    {
        await SaveItemToDatabase(conn,
            item.FileName,
            item.Content,
            mailFileName,
            "attachment",
            item.Date
        );
    }

    public static async Task SaveMailToDatabaseAsync(SqliteConnection conn, string mailFileName, Mail mail)
    {
        await SaveItemToDatabase(conn,
            mail.Subject,
            mail.Content,
            mailFileName,
            "mail",
            mail.Date
        );
    }

    static async Task SaveItemToDatabase(SqliteConnection conn, string subject, string content, string path, string type, DateTimeOffset date)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO Mails (Subject, Content, Path, Type, Date) VALUES (@Subject, @Content, @Path, @Type, @Date)";

        cmd.Parameters.AddWithValue("@Subject", subject ?? "");
        cmd.Parameters.AddWithValue("@Content", content ?? "");
        cmd.Parameters.AddWithValue("@Path", path ?? "");
        cmd.Parameters.AddWithValue("@Type", type ?? "");
        cmd.Parameters.AddWithValue("@Date", date.DateTime.ToString("s"));

        await cmd.ExecuteNonQueryAsync();
    }

}
