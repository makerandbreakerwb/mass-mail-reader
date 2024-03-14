using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace MassMailReader;

public static class DocxReader
{
    public static string Read(Stream stream)
    {
        using var document = WordprocessingDocument.Open(stream, isEditable: false);

        if (document.MainDocumentPart?.Document.Body is null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder(1000);

        foreach (var text in EnumerateAllTexts(document.MainDocumentPart.Document.Body))
        {
            sb.Append(text.Text).Append(' ');
        }

        return sb.ToString();
    }

    private static IEnumerable<Text> EnumerateAllTexts(OpenXmlCompositeElement elm)
    {
        foreach (var item in elm)
        {
            if (item is Text text && !string.IsNullOrWhiteSpace(item.InnerText) && item.InnerText.Any(char.IsAsciiLetterOrDigit))
            {
                yield return text;
            }
            else if (item is OpenXmlCompositeElement composite)
            {
                foreach (var subItem in EnumerateAllTexts(composite))
                {
                    yield return subItem;
                }
            }
        }
    }
}
