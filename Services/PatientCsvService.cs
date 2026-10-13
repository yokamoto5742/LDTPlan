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

    // pat.csv の列位置（0始まり）。ここにない列は使用しない
    private const int ColPatientId = 2;
    private const int ColName = 3;
    private const int ColKana = 4;
    private const int ColGenderCode = 5;
    private const int ColBirthdate = 6;
    private const int ColDoctorId = 9;
    private const int ColDoctorName = 10;
    private const int ColDepartmentId = 13;
    private const int ColDepartment = 14;
    private const int ColumnCount = 15;

    /// <summary>患者CSVデータ読み込み（Shift_JIS・ヘッダなし・先頭3行のみ）</summary>
    public static List<PatientCsvRecord> LoadPatientData()
    {
        var encoding = Encoding.GetEncoding("shift_jis");
        var records = new List<PatientCsvRecord>();

        foreach (var line in File.ReadLines(AppConfig.PatientDataPath, encoding).Take(MaxRows))
        {
            var f = SplitCsvLine(line);
            // 列数が足りない行・患者IDが数値でない行（列ずれなど）は読み飛ばす
            if (f.Count < ColumnCount || !int.TryParse(f[ColPatientId].Trim(), out var patientId))
                continue;

            records.Add(new PatientCsvRecord
            {
                PatientId = patientId,
                Name = f[ColName],
                Kana = f[ColKana],
                GenderCode = ParseInt(f[ColGenderCode]),
                Birthdate = ParseDate(f[ColBirthdate]),
                DoctorId = ParseInt(f[ColDoctorId]),
                DoctorName = f[ColDoctorName],
                DepartmentId = ParseInt(f[ColDepartmentId]),
                Department = f[ColDepartment],
            });
        }

        return records;
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
