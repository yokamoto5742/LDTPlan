using System.Globalization;
using System.Text;
using LDTPlan.Utils;

namespace LDTPlan.Services;

/// <summary>pat.csv の1行分の患者情報</summary>
public class PatientCsvRecord
{
    public int PatientId { get; init; }
    public string Name { get; init; } = "";
    public string Kana { get; init; } = "";
    public int GenderCode { get; init; }
    public DateTime? Birthdate { get; init; }
    public int DoctorId { get; init; }
    public string DoctorName { get; init; } = "";
    public int DepartmentId { get; init; }
    public string Department { get; init; } = "";

    public string Gender => GenderCode == 1 ? Constants.GenderMale : Constants.GenderFemale;
}

public static class PatientCsvService
{
    private const int MaxRows = 3;

    /// <summary>患者CSVデータ読み込み（Shift_JIS・ヘッダなし・先頭3行のみ）</summary>
    public static (string ErrorMessage, List<PatientCsvRecord>? Records) LoadPatientData()
    {
        try
        {
            var encoding = Encoding.GetEncoding("shift_jis");
            var records = new List<PatientCsvRecord>();

            foreach (var line in File.ReadLines(AppConfig.PatientDataPath, encoding).Take(MaxRows))
            {
                var f = SplitCsvLine(line);
                if (f.Count < 15)
                    continue;

                records.Add(new PatientCsvRecord
                {
                    PatientId = ParseInt(f[2]),
                    Name = f[3],
                    Kana = f[4],
                    GenderCode = ParseInt(f[5]),
                    Birthdate = ParseDate(f[6]),
                    DoctorId = ParseInt(f[9]),
                    DoctorName = f[10],
                    DepartmentId = ParseInt(f[13]),
                    Department = f[14],
                });
            }

            return ("", records);
        }
        catch (Exception e)
        {
            return ($"エラー: {e.Message}", null);
        }
    }

    private static int ParseInt(string value)
        => int.TryParse(value.Trim(), out var v) ? v : 0;

    private static DateTime? ParseDate(string value)
    {
        value = value.Trim();
        if (value.Length == 0)
            return null;
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var v))
            return v.Date;
        if (DateTime.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out v))
            return v.Date;
        return null;
    }

    /// <summary>ダブルクォート対応のCSV1行分割</summary>
    internal static List<string> SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == ',')
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return fields;
    }
}
