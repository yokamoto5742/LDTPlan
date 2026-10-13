using LDTPlan.Data;
using LDTPlan.Utils;
using Microsoft.EntityFrameworkCore;

namespace LDTPlan.Services;

/// <summary>計画書一覧の1行分</summary>
public class PatientHistoryRow
{
    public int Id { get; init; }
    public string IssueDate { get; init; } = "";
    public string Department { get; init; } = "";
    public string DoctorName { get; init; } = "";
    public string MainDiagnosis { get; init; } = "";
    public string SheetName { get; init; } = "";
    public int? CreationCount { get; init; }
}

public static class PatientService
{
    /// <summary>主病名マスタ読み込み</summary>
    public static List<string> LoadMainDiseases()
    {
        using var db = new AppDbContext();
        return db.MainDiseases.Select(d => d.Name ?? "").ToList();
    }

    /// <summary>シート名マスタ読み込み（主病名ID指定時は絞り込み）</summary>
    public static List<string> LoadSheetNames(int? mainDiseaseId = null)
    {
        using var db = new AppDbContext();
        var query = db.SheetNames.AsQueryable();
        if (mainDiseaseId.HasValue)
            query = query.Where(s => s.MainDiseaseId == mainDiseaseId.Value);
        return query.Select(s => s.Name ?? "").ToList();
    }

    /// <summary>主病名から主病名IDを取得</summary>
    public static int? FindMainDiseaseId(string? mainDiseaseName)
    {
        if (string.IsNullOrEmpty(mainDiseaseName))
            return null;
        using var db = new AppDbContext();
        return db.MainDiseases.FirstOrDefault(d => d.Name == mainDiseaseName)?.Id;
    }

    /// <summary>患者履歴取得</summary>
    public static List<PatientHistoryRow> FetchPatientHistory(int? filterPatientId)
    {
        if (!filterPatientId.HasValue)
            return [];

        using var db = new AppDbContext();
        return db.PatientInfos
            .Where(p => p.PatientId == filterPatientId.Value)
            .OrderByDescending(p => p.Id)
            .Select(p => new { p.Id, p.IssueDate, p.Department, p.DoctorName, p.MainDiagnosis, p.SheetName, p.CreationCount })
            .AsEnumerable()
            .Select(p => new PatientHistoryRow
            {
                Id = p.Id,
                IssueDate = p.IssueDate?.ToString("yyyy/MM/dd") ?? "",
                Department = p.Department ?? "",
                DoctorName = p.DoctorName ?? "",
                MainDiagnosis = p.MainDiagnosis ?? "",
                SheetName = p.SheetName ?? "",
                CreationCount = p.CreationCount,
            })
            .ToList();
    }

    /// <summary>前回計画コピー: 患者の最新の計画書を、発行日と医師情報を更新して複製する（計画書がなければ false）</summary>
    public static bool CopyLatestPlan(PatientCsvRecord patient, DateTime today)
    {
        using var db = new AppDbContext();
        // 追跡なしで読み込み、主キーを 0 に戻して追加する（列を追加しても複写漏れが起きない）
        var copy = db.PatientInfos.AsNoTracking()
            .Where(p => p.PatientId == patient.PatientId)
            .OrderByDescending(p => p.Id)
            .FirstOrDefault();
        if (copy == null)
            return false;

        copy.Id = 0;
        copy.IssueDate = today;
        copy.IssueDateAge = copy.Birthdate.HasValue
            ? DateUtils.CalculateIssueDateAge(copy.Birthdate.Value, today)
            : null;
        copy.DoctorId = patient.DoctorId;
        copy.DoctorName = patient.DoctorName;
        copy.Department = patient.Department;
        copy.DepartmentId = patient.DepartmentId;
        copy.CreationCount++;
        db.PatientInfos.Add(copy);
        db.SaveChanges();
        return true;
    }
}
