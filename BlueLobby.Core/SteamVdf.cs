using System;
using System.Collections.Generic;
using System.Text;

namespace BlueLobby.Core
{
    /// <summary>
    /// Minimal Valve KeyValues/VDF string tokenizer. It deliberately parses only
    /// quoted strings and braces; callers can safely query repeated key/value pairs
    /// without relying on fragile regular expressions.
    /// </summary>
    internal static class SteamVdf
    {
        public static IEnumerable<string> ValuesForKey(string text, string key)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(key)) yield break;

            List<string> tokens = Tokenize(text);
            for (int i = 0; i + 1 < tokens.Count; i++)
            {
                if (!string.Equals(tokens[i], key, StringComparison.OrdinalIgnoreCase)) continue;
                string value = tokens[i + 1];
                if (value != "{" && value != "}") yield return value;
            }
        }

        private static List<string> Tokenize(string text)
        {
            var tokens = new List<string>();
            var current = new StringBuilder();
            bool quoted = false;
            bool escape = false;

            void Flush()
            {
                if (current.Length == 0) return;
                tokens.Add(current.ToString());
                current.Clear();
            }

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (quoted)
                {
                    if (escape)
                    {
                        current.Append(c switch
                        {
                            'n' => '\n',
                            'r' => '\r',
                            't' => '\t',
                            '\\' => '\\',
                            '"' => '"',
                            _ => c,
                        });
                        escape = false;
                    }
                    else if (c == '\\')
                    {
                        escape = true;
                    }
                    else if (c == '"')
                    {
                        quoted = false;
                        Flush();
                    }
                    else
                    {
                        current.Append(c);
                    }
                    continue;
                }

                if (c == '"')
                {
                    Flush();
                    quoted = true;
                }
                else if (c == '{' || c == '}')
                {
                    Flush();
                    tokens.Add(c.ToString());
                }
                else if (char.IsWhiteSpace(c))
                {
                    Flush();
                }
                else if (c == '/' && i + 1 < text.Length && text[i + 1] == '/')
                {
                    Flush();
                    i += 2;
                    while (i < text.Length && text[i] != '\n') i++;
                }
                else
                {
                    current.Append(c);
                }
            }

            if (escape) current.Append('\\');
            Flush();
            return tokens;
        }
    }
}
