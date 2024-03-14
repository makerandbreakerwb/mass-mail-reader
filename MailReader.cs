using System.IO.Compression;
using System.Text;
using MimeKit;
using Spectre.Console;

namespace MassMailReader;
public static class MailReader
{
    public static async Task<Mail> ReadMailAsync(string ident, string fileName)
    {
        // Load a MimeMessage from a stream
        AnsiConsole.WriteLine($"{ident} Parsing mail content");

        using var stream = File.OpenRead(fileName);
        var parser = new MimeParser(stream, MimeFormat.Entity);
        var message = await parser.ParseMessageAsync();

        var content = !string.IsNullOrEmpty(message.HtmlBody) ? HtmlReader.Read(message.HtmlBody) : message.TextBody;

        var subject = GetTextFromMangledCyrilic(message.Subject);

        var result = new Mail(
            subject.Trim(),
            content?.Trim() ?? "",
            [],
            message.Date
        );

        AnsiConsole.WriteLine($"{ident} Reading attachments");
        foreach (var attachment in message.Attachments)
        {
            AnsiConsole.WriteLine($"{ident} Reading attachment '{attachment.ContentDisposition.FileName}'");

            if (attachment is not MimePart part)
            {
                continue;
            }

            try
            {
                using var memoryStream = new MemoryStream();
                part.Content.DecodeTo(memoryStream);
                memoryStream.Position = 0;

                result.Attachements.AddRange(ProcessFile(attachment.ContentDisposition.FileName, memoryStream, DateTimeOffset.MinValue));
            }
            catch (Exception e)
            {
                AnsiConsole.MarkupLineInterpolated($"[red]{ident} Error reading attachment: '{attachment.ContentDisposition.FileName}'[/]");
                AnsiConsole.WriteException(e, ExceptionFormats.ShortenEverything);
            }
        }

        AnsiConsole.WriteLine($"{ident} Mail read");
        return result;
    }

    private static string GetTextFromMangledCyrilic(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "";
        }

        var bytes = Encoding.GetEncoding(1252).GetBytes(text);
        var result = Encoding.GetEncoding(20866).GetString(bytes);

        return result;
    }

    private static byte[] GetBytes(Stream s)
    {
        using var ms = new MemoryStream();
        s.CopyTo(ms);
        return ms.ToArray();
    }

    public static IEnumerable<MailAttachement> ProcessFile(string fileName, Stream stream, DateTimeOffset date)
    {
        return Path.GetExtension(fileName)?.ToLowerInvariant() switch
        {
            //".eml" => ReadMail(fileName).Content,
            ".zip" => ExtractZipFile(fileName, stream),
            ".pdf" => [new MailAttachement(fileName, PdfReader.Read(stream), date)],
            ".htm" or ".html" => [new MailAttachement(fileName, HtmlReader.Read(Encoding.UTF8.GetString(GetBytes(stream))), date)],
            ".txt" => [new MailAttachement(fileName, Encoding.UTF8.GetString(GetBytes(stream)), date)],
            ".docx" => [new MailAttachement(fileName, DocxReader.Read(stream), date)],
            _ => [new MailAttachement(fileName, "UNSUPPORTED", date)]
        };
    }

    public static IEnumerable<MailAttachement> ExtractZipFile(string fileName, Stream zipStream)
    {
        using var zip = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: false, Encoding.UTF8);
        foreach (var entry in zip.Entries)
        {
            using var stream = entry.Open();

            var list = new List<MailAttachement>();
            try
            {
                list.AddRange(ProcessFile(entry.Name, stream, entry.LastWriteTime));
            }
            catch (Exception e)
            {
                AnsiConsole.MarkupLineInterpolated($"[red]Error reading attachment '{Markup.Escape(entry.Name)}' in '{Markup.Escape(fileName)}'[/]");
                AnsiConsole.WriteException(e, ExceptionFormats.ShortenEverything);
            }
            foreach (var item in list)
            {
                yield return item;
            }
        }
    }
}
