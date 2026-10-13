using LDTPlan.Data;
using LDTPlan.Models;
using LDTPlan.Services;

namespace LDTPlan.Tests;

public sealed class PatientServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();

    public void Dispose() => _env.Dispose();

    [Fact]
    public void 新規DBにマスタが投入される()
    {
        Assert.Equal(["高血圧症", "脂質異常症", "糖尿病"], PatientService.LoadMainDiseases());
        Assert.Equal(9, PatientService.LoadSheetNames().Count);

        using var db = new AppDbContext();
        Assert.Equal(9, db.Templates.Count());
        // シート名マスタとテンプレートが対応している
        Assert.Equal(
            db.SheetNames.OrderBy(s => s.Id).Select(s => s.Name).ToList(),
            db.Templates.OrderBy(t => t.Id).Select(t => t.SheetName).ToList());
    }

    [Fact]
    public void シート名を主病名で絞り込む()
    {
        var mainDiseaseId = PatientService.FindMainDiseaseId("脂質異常症");

        var sheetNames = PatientService.LoadSheetNames(mainDiseaseId);

        Assert.Equal(["1_LDL120以下", "2_LDL100以下", "3_LDL70以下"], sheetNames);
    }

    [Fact]
    public void 履歴は指定した患者の計画書を新しい順に返す()
    {
        using (var db = new AppDbContext())
        {
            db.PatientInfos.Add(new PatientInfo { PatientId = 123, IssueDate = new DateTime(2026, 4, 1), CreationCount = 1 });
            db.PatientInfos.Add(new PatientInfo { PatientId = 456, IssueDate = new DateTime(2026, 5, 1), CreationCount = 1 });
            db.PatientInfos.Add(new PatientInfo { PatientId = 123, IssueDate = new DateTime(2026, 8, 1), CreationCount = 2 });
            db.SaveChanges();
        }

        var history = PatientService.FetchPatientHistory(123);

        Assert.Equal(["2026/08/01", "2026/04/01"], history.Select(h => h.IssueDate));
        Assert.Equal([2, 1], history.Select(h => h.CreationCount));
    }

    [Fact]
    public void 前回計画コピーは最新の計画書を複製し発行日と医師情報と作成回数だけを更新する()
    {
        var latest = TestData.FullPatientInfo();
        latest.CreationCount = 2;
        using (var db = new AppDbContext())
        {
            db.PatientInfos.Add(new PatientInfo { PatientId = 123, CreationCount = 1, Goal1 = "古い計画" });
            db.PatientInfos.Add(latest);
            db.PatientInfos.Add(new PatientInfo { PatientId = 456, Goal1 = "別の患者" });
            db.SaveChanges();
        }
        var patient = new PatientCsvRecord
        {
            PatientId = 123, DoctorId = 20, DoctorName = "田中医師", DepartmentId = 5, Department = "外科",
        };

        Assert.True(PatientService.CopyLatestPlan(patient, new DateTime(2026, 10, 13)));

        using var verify = new AppDbContext();
        Assert.Equal(4, verify.PatientInfos.Count());
        var copy = verify.PatientInfos.OrderByDescending(p => p.Id).First();
        var changed = new Dictionary<string, object?>
        {
            [nameof(PatientInfo.Id)] = latest.Id + 2,
            [nameof(PatientInfo.IssueDate)] = new DateTime(2026, 10, 13),
            [nameof(PatientInfo.IssueDateAge)] = 66,
            [nameof(PatientInfo.DoctorId)] = 20,
            [nameof(PatientInfo.DoctorName)] = "田中医師",
            [nameof(PatientInfo.DepartmentId)] = 5,
            [nameof(PatientInfo.Department)] = "外科",
            [nameof(PatientInfo.CreationCount)] = 3,
        };
        foreach (var property in typeof(PatientInfo).GetProperties())
        {
            var expected = changed.TryGetValue(property.Name, out var value) ? value : property.GetValue(latest);
            Assert.Equal(expected, property.GetValue(copy));
        }

        // コピー元は変更されない
        var source = verify.PatientInfos.Single(p => p.Id == latest.Id);
        Assert.Equal(new DateTime(2026, 4, 1), source.IssueDate);
        Assert.Equal(2, source.CreationCount);
    }

    [Fact]
    public void 計画書がない患者の前回計画コピーは何も追加しない()
    {
        Assert.False(PatientService.CopyLatestPlan(new PatientCsvRecord { PatientId = 123 }, DateTime.Today));

        using var db = new AppDbContext();
        Assert.Empty(db.PatientInfos);
    }

    [Fact]
    public void 患者ID未指定の履歴は空になる()
    {
        Assert.Empty(PatientService.FetchPatientHistory(null));
    }
}
