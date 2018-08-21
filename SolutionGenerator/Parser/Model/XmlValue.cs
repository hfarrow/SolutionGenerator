using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace SolutionGen.Parser.Model
{
    public class XmlValue : ValueElement
    {
        public XmlValue(FormattedText text) : base(FormatXml(text.Value.ToString()))
        {
        }

        public static string FormatXml(string text)
        {
            string formattedXml = text;
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml("<root>" + text + "</root>");
                var stream = new MemoryStream();
                var writer = new XmlTextWriter(stream, Encoding.Unicode)
                {
                    Formatting = Formatting.Indented,
                    Indentation = 4,
                    IndentChar = ' '
                };

                doc.WriteContentTo(writer);
                writer.Flush();
                stream.Flush();
                stream.Position = 0;
                var reader = new StreamReader(stream);
                string[] lines = reader.ReadToEnd().Split('\n');
                // remove the "root" element that was added in order to write the formatted xml.
                lines = lines.Skip(1).Take(lines.Length - 2).ToArray();
                
                // Remove the extra indentation added by the temporary root element during formatting.
                formattedXml = FormattedText.StripLeadPadding(
                    "".PadRight(writer.Indentation, writer.IndentChar),
                    lines);
            }
            catch
            {
                // ignored
            }

            return formattedXml;
        }
    }
}