using System.Text;

namespace CrispySearchbar.Core.Dictionary;

/// <summary>
/// 逐条读取 CSV 记录。支持双引号包裹的字段：字段内可含逗号与换行，
/// 连续两个双引号表示一个字面双引号。
/// </summary>
public static class CsvRecordReader
{
    public static IEnumerable<string[]> Read(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var fields = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;

        while (true)
        {
            var line = reader.ReadLine();
            if (line is null)
            {
                break;
            }

            if (inQuotes)
            {
                // 前一行在引号字段内结束，把被 ReadLine 去掉的换行补回字段。
                field.Append('\n');
            }

            for (var index = 0; index < line.Length; index++)
            {
                var character = line[index];
                if (inQuotes)
                {
                    if (character != '"')
                    {
                        field.Append(character);
                        continue;
                    }

                    if (index + 1 < line.Length && line[index + 1] == '"')
                    {
                        field.Append('"');
                        index++;
                    }
                    else
                    {
                        inQuotes = false;
                    }

                    continue;
                }

                if (character == '"' && field.Length == 0)
                {
                    inQuotes = true;
                }
                else if (character == ',')
                {
                    fields.Add(field.ToString());
                    field.Clear();
                }
                else
                {
                    field.Append(character);
                }
            }

            if (inQuotes)
            {
                continue;
            }

            if (fields.Count > 0 || field.Length > 0)
            {
                fields.Add(field.ToString());
                yield return fields.ToArray();
            }

            fields.Clear();
            field.Clear();
        }

        if (fields.Count > 0 || field.Length > 0)
        {
            fields.Add(field.ToString());
            yield return fields.ToArray();
        }
    }
}
