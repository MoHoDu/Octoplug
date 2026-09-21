using System.Collections.Generic;
using System.Text;

namespace Octoplug.Balance
{
    public static class CsvParser
    {
        public static List<List<string>> Parse(string csvText)
        {
            var rows = new List<List<string>>();
            var currentRow = new List<string>();
            var currentValue = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < csvText.Length; i++)
            {
                char c = csvText[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < csvText.Length && csvText[i + 1] == '"')
                    {
                        currentValue.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    currentRow.Add(currentValue.ToString());
                    currentValue.Clear();
                }
                else if ((c == '\n' || c == '\r') && !inQuotes)
                {
                    if (c == '\r' && i + 1 < csvText.Length && csvText[i + 1] == '\n')
                    {
                        i++;
                    }
                    currentRow.Add(currentValue.ToString());
                    currentValue.Clear();
                    rows.Add(currentRow);
                    currentRow = new List<string>();
                }
                else
                {
                    currentValue.Append(c);
                }
            }

            if (currentValue.Length > 0 || csvText.EndsWith(","))
            {
                currentRow.Add(currentValue.ToString());
            }

            if (currentRow.Count > 0)
            {
                rows.Add(currentRow);
            }

            if (rows.Count > 0 && rows[rows.Count - 1].Count == 1 && string.IsNullOrEmpty(rows[rows.Count - 1][0]))
            {
                rows.RemoveAt(rows.Count - 1);
            }

            return rows;
        }
    }
}
