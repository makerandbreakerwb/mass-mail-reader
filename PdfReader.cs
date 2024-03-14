using System.Text;
using UglyToad.PdfPig;

namespace MassMailReader;

internal static class PdfReader
{
    public static string Read(Stream stream)
    {
        using var document = PdfDocument.Open(stream);
        var sb = new StringBuilder(1000);
        foreach (var page in document.GetPages())
        {
            sb.Append("Page ").Append(page.Number).AppendLine();
            foreach (var word in page.GetWords())
            {
                sb.Append(word.Text);
                sb.Append(' ');
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
