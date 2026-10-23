using LDTPlan.Data;
using LDTPlan.Forms;
using LDTPlan.Models;

namespace LDTPlan.Tests;

/// <summary>画面の項目とレコードの対応（列を追加したときの LoadRecord などの更新漏れを検出する）</summary>
public sealed class PlanEditFormTests : IDisposable
{
    private readonly TestEnvironment _env = new();

    public void Dispose() => _env.Dispose();

    [Fact]
    public void 編集画面で読み込んだ計画書を保存すると全列が元の値に戻る()
    {
        var original = TestData.FullPatientInfo();
        using (var db = new AppDbContext())
        {
            db.PatientInfos.Add(original);
            db.SaveChanges();
        }

        using var form = new PlanEditForm(PlanFormMode.Edit, TestData.FullPatientCsvRecord(), original.Id);
        var saved = new PatientInfo();
        form.ApplyBasicInfo(saved);
        form.ApplyGuidanceFields(saved);

        AssertAllPropertiesEqual(original, saved, nameof(PatientInfo.Id));
    }

    [Fact]
    public void テンプレートを画面に適用して保存すると全列が元の値に戻る()
    {
        var original = TestData.FullPlanTemplate();

        using var form = new PlanEditForm(PlanFormMode.Template, null);
        form.ApplyTemplateFields(original);
        var saved = form.ReadTemplateFields(original.MainDisease!, original.SheetName!);

        AssertAllPropertiesEqual(original, saved, nameof(PlanTemplate.Id));
    }

    private static void AssertAllPropertiesEqual<T>(T expected, T actual, string ignoredProperty)
    {
        foreach (var property in typeof(T).GetProperties().Where(p => p.Name != ignoredProperty))
            Assert.True(Equals(property.GetValue(expected), property.GetValue(actual)), property.Name);
    }
}
