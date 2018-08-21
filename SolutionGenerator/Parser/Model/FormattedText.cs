using System.Collections.Generic;
using System.Linq;

namespace SolutionGen.Parser.Model
{
    public class FormattedText : ValueElement
    {
        public FormattedText(IEnumerable<char> lead, string body)
            : base(StripLeadPadding(new string(lead.ToArray()), body.Split('\n')))
        {
        }

        public static string StripLeadPadding(string lead, string[] body)
        {
            for (int i = 0; i < body.Length; i++)
            {
                string line = body[i];
                if (line.StartsWith(lead))
                {
                    body[i] = line.Substring(lead.Length);
                }
            }

            return string.Join('\n', body);
        }
    }
}