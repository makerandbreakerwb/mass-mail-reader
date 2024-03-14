using HtmlAgilityPack;

namespace MassMailReader
{
    internal static class HtmlReader
    {

        public static string Read(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            return doc.DocumentNode.InnerText;
        }
    }
}
