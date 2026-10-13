using ClosedXML.Excel;
using LDTPlan.Models;
using LDTPlan.Services;

namespace LDTPlan.Tests;

public class TreatmentPlanGeneratorTests
{
    [Fact]
    public void 共通情報シートに値の型を保って書き込む()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("共通情報");

        TreatmentPlanGenerator.PopulateCommonSheet(sheet, TestData.FullPatientInfo());

        Assert.Equal(123, sheet.Cell("B2").Value.GetNumber());
        Assert.Equal("山田太郎", sheet.Cell("B3").Value.GetText());
        Assert.Equal(new DateTime(1960, 3, 15), sheet.Cell("B6").Value.GetDateTime());
        Assert.Equal(new DateTime(2026, 4, 1), sheet.Cell("B7").Value.GetDateTime());
        Assert.Equal(1, sheet.Cell("B13").Value.GetNumber());
        Assert.Equal(65.5, sheet.Cell("B14").Value.GetNumber());
        Assert.True(sheet.Cell("B30").Value.GetBoolean());
        Assert.False(sheet.Cell("B31").Value.GetBoolean());
        Assert.Equal(66, sheet.Cell("B37").Value.GetNumber());
        Assert.Equal("運動コメント", sheet.Cell("B39").Value.GetText());
    }

    [Fact]
    public void 値がない項目はテンプレートに残っている値を消して空白にする()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("共通情報");
        foreach (var row in Enumerable.Range(2, 38))
            sheet.Cell(row, 2).Value = "前回の値";

        TreatmentPlanGenerator.PopulateCommonSheet(sheet, new PatientInfo());

        Assert.All(Enumerable.Range(2, 38), row => Assert.True(sheet.Cell(row, 2).Value.IsBlank));
    }
}
