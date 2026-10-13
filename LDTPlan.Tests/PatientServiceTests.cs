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
    public void 患者ID未指定の履歴は空になる()
    {
        Assert.Empty(PatientService.FetchPatientHistory(null));
    }
}
