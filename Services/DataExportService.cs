using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using LDTPlan.Data;
using LDTPlan.Models;
using LDTPlan.Utils;

namespace LDTPlan.Services;

/// <summary>患者情報のCSVエクスポート・インポート</summary>
public static class DataExportService
{
    // Python版のエクスポート形式（列名・値の書式）と互換にする
    private static readonly (string Name, Func<PatientInfo, object?> Get, Action<PatientInfo, string> Set)[] Columns =
    [
        ("patient_id", p => p.PatientId, (p, v) => p.PatientId = ParseInt(v)),
        ("patient_name", p => p.PatientName, (p, v) => p.PatientName = v),
        ("kana", p => p.Kana, (p, v) => p.Kana = v),
        ("gender", p => p.Gender, (p, v) => p.Gender = v),
        ("birthdate", p => p.Birthdate, (p, v) => p.Birthdate = ParseDate(v)),
        ("issue_date", p => p.IssueDate, (p, v) => p.IssueDate = ParseDate(v)),
        ("issue_date_age", p => p.IssueDateAge, (p, v) => p.IssueDateAge = ParseInt(v)),
        ("doctor_id", p => p.DoctorId, (p, v) => p.DoctorId = ParseInt(v)),
        ("doctor_name", p => p.DoctorName, (p, v) => p.DoctorName = v),
        ("department", p => p.Department, (p, v) => p.Department = v),
        ("department_id", p => p.DepartmentId, (p, v) => p.DepartmentId = ParseInt(v)),
        ("main_diagnosis", p => p.MainDiagnosis, (p, v) => p.MainDiagnosis = v),
        ("creation_count", p => p.CreationCount, (p, v) => p.CreationCount = ParseInt(v)),
        ("target_weight", p => p.TargetWeight, (p, v) => p.TargetWeight = ParseDouble(v)),
        ("sheet_name", p => p.SheetName, (p, v) => p.SheetName = v),
        ("target_bp", p => p.TargetBp, (p, v) => p.TargetBp = v),
        ("target_hba1c", p => p.TargetHba1c, (p, v) => p.TargetHba1c = v),
        ("goal1", p => p.Goal1, (p, v) => p.Goal1 = v),
        ("goal2", p => p.Goal2, (p, v) => p.Goal2 = v),
        ("target_achievement", p => p.TargetAchievement, (p, v) => p.TargetAchievement = v),
        ("diet1", p => p.Diet1, (p, v) => p.Diet1 = v),
        ("diet2", p => p.Diet2, (p, v) => p.Diet2 = v),
        ("diet3", p => p.Diet3, (p, v) => p.Diet3 = v),
        ("diet4", p => p.Diet4, (p, v) => p.Diet4 = v),
        ("diet_comment", p => p.DietComment, (p, v) => p.DietComment = v),
        ("exercise_prescription", p => p.ExercisePrescription, (p, v) => p.ExercisePrescription = v),
        ("exercise_time", p => p.ExerciseTime, (p, v) => p.ExerciseTime = v),
        ("exercise_frequency", p => p.ExerciseFrequency, (p, v) => p.ExerciseFrequency = v),
        ("exercise_intensity", p => p.ExerciseIntensity, (p, v) => p.ExerciseIntensity = v),
        ("daily_activity", p => p.DailyActivity, (p, v) => p.DailyActivity = v),
        ("exercise_comment", p => p.ExerciseComment, (p, v) => p.ExerciseComment = v),
        ("nonsmoker", p => p.Nonsmoker, (p, v) => p.Nonsmoker = ParseBool(v)),
        ("smoking_cessation", p => p.SmokingCessation, (p, v) => p.SmokingCessation = ParseBool(v)),
        ("other1", p => p.Other1, (p, v) => p.Other1 = v),
        ("other2", p => p.Other2, (p, v) => p.Other2 = v),
        ("ophthalmology", p => p.Ophthalmology, (p, v) => p.Ophthalmology = ParseBool(v)),
        ("dental", p => p.Dental, (p, v) => p.Dental = ParseBool(v)),
        ("cancer_screening", p => p.CancerScreening, (p, v) => p.CancerScreening = ParseBool(v)),
    ];

    /// <summary>CSV出力。出力したファイル名を返す</summary>
    public static string ExportToCsv(string exportFolder)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var fileName = $"patient_info_export_{timestamp}.csv";
        var filePath = Path.Combine(exportFolder, fileName);

        Directory.CreateDirectory(exportFolder);

        using var db = new AppDbContext();
        var patients = db.PatientInfos.ToList();

        var encoding = Encoding.GetEncoding(
            "shift_jis", new EncoderReplacementFallback(""), DecoderFallback.ReplacementFallback);
        using var writer = new StreamWriter(filePath, false, encoding);

        writer.WriteLine(string.Join(",", Columns.Select(c => c.Name).Prepend("id")));
        foreach (var p in patients)
        {
            var values = new List<string> { p.Id.ToString() };
            values.AddRange(Columns.Select(c => FormatValue(c.Get(p))));
            writer.WriteLine(string.Join(",", values.Select(EscapeCsv)));
        }

        return fileName;
    }

    /// <summary>CSV取込。取り込めないファイルは例外を投げる（1行でも失敗すれば全体を取り込まない）</summary>
    public static void ImportFromCsv(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        if (!Regex.IsMatch(fileName, @"^patient_info_.*\.csv$"))
            throw new InvalidOperationException(Constants.MsgCsvImportInvalidFile);

        var encoding = Encoding.GetEncoding("shift_jis");
        var lines = File.ReadAllLines(filePath, encoding);
        if (lines.Length == 0)
            return;

        var header = PatientCsvService.SplitCsvLine(lines[0]);
        using var db = new AppDbContext();

        foreach (var line in lines.Skip(1))
        {
            if (line.Trim().Length == 0)
                continue;

            var values = PatientCsvService.SplitCsvLine(line);
            var patient = new PatientInfo();
            foreach (var column in Columns)
            {
                var index = header.IndexOf(column.Name);
                if (index >= 0 && index < values.Count)
                    column.Set(patient, values[index]);
            }
            db.PatientInfos.Add(patient);
        }

        db.SaveChanges();
    }

    private static string FormatValue(object? value) => value switch
    {
        null => "",
        DateTime dt => dt.ToString("yyyy-MM-dd"),
        bool b => b ? "True" : "False",
        double d => d.ToString(CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "",
    };

    private static string EscapeCsv(string value)
        => value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;

    private static int? ParseInt(string v) => v == "" ? null : int.Parse(v);
    private static double? ParseDouble(string v) => v == "" ? null : double.Parse(v, CultureInfo.InvariantCulture);
    private static bool? ParseBool(string v) => v == "" ? null : v == "True";

    private static DateTime? ParseDate(string v)
        => v == "" ? null : DateTime.ParseExact(v, "yyyy-MM-dd", CultureInfo.InvariantCulture);
}
