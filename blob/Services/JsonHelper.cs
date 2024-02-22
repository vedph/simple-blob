using System;
using System.Text;

namespace SimpleBlob.Cli.Services;

public static class JsonHelper
{
    public static string Jsonize(string s, bool quoted)
    {
        ArgumentNullException.ThrowIfNull(s);

        StringBuilder sb = new();
        foreach (char c in s)
        {
            switch (c)
            {
                case '\u0008':
                    sb.Append(@"\b");
                    break;
                case '\u0009':
                    sb.Append(@"\t");
                    break;
                case '\u000A':
                    sb.Append(@"\n");
                    break;
                case '\u000C':
                    sb.Append(@"\f");
                    break;
                case '\u000D':
                    sb.Append(@"\r");
                    break;
                case '"':
                    sb.Append('\\').Append('"');
                    break;
                case '\\':
                    sb.Append(@"\\");
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }

        if (quoted)
        {
            sb.Insert(0, '"');
            sb.Append('"');
        }
        return sb.ToString();
    }
}
