using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using LDTPlan.Models;
using LDTPlan.Services;
using System.IO.Compression;
using System.Xml.Linq;
using Spreadsheet = DocumentFormat.OpenXml.Spreadsheet;

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

    [Fact]
    public void 主キー以外の全列を共通情報シートのB2から順に書き込む()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("共通情報");

        TreatmentPlanGenerator.PopulateCommonSheet(sheet, TestData.FullPatientInfo());

        // 列を追加してセルの書き込みを忘れると、書き込まれたセルの数が列数より少なくなる
        var columnCount = typeof(PatientInfo).GetProperties().Length - 1;
        Assert.Equal(columnCount, sheet.Column(2).CellsUsed().Count());
        Assert.All(Enumerable.Range(2, columnCount), row => Assert.False(sheet.Cell(row, 2).Value.IsBlank));
    }
}

/// <summary>配布先のExcelテンプレートから実際に計画書ファイルを生成する</summary>
public sealed class TreatmentPlanFileTests : IDisposable
{
    private readonly TestEnvironment _env = new();

    public void Dispose() => _env.Dispose();

    [TemplateFact]
    public void 初回の計画書は初回用シートを開いた状態で保存する()
    {
        var filePath = TreatmentPlanGenerator.CreatePlanFile(TestData.FullPatientInfo());

        using var workbook = new XLWorkbook(filePath);
        Assert.Equal(1, workbook.Worksheet("共通情報").Cell("B13").Value.GetNumber());
        Assert.Equal("山田太郎", workbook.Worksheet("共通情報").Cell("B3").Value.GetText());
        Assert.True(workbook.Worksheet("初回用").TabActive);
    }

    [TemplateFact]
    public void 継続の計画書は継続用シートを開いた状態で保存する()
    {
        var patientInfo = TestData.FullPatientInfo();
        patientInfo.CreationCount = 2;

        var filePath = TreatmentPlanGenerator.CreatePlanFile(patientInfo);

        using var workbook = new XLWorkbook(filePath);
        Assert.Equal(2, workbook.Worksheet("共通情報").Cell("B13").Value.GetNumber());
        Assert.True(workbook.Worksheet("継続用").TabActive);
    }

    [TemplateFact]
    public void VBAマクロをテンプレートから変えずに保持する()
    {
        var filePath = TreatmentPlanGenerator.CreatePlanFile(TestData.FullPatientInfo());

        Assert.Equal(ReadVbaProject(TestEnvironment.TemplatePath), ReadVbaProject(filePath));
    }

    [TemplateFact]
    public void 初回用と継続用のシートにバーコード画像を1枚ずつ追加する()
    {
        var filePath = TreatmentPlanGenerator.CreatePlanFile(TestData.FullPatientInfo());

        using var template = new XLWorkbook(TestEnvironment.TemplatePath);
        using var output = new XLWorkbook(filePath);
        foreach (var sheetName in new[] { "初回用", "継続用" })
            Assert.Equal(template.Worksheet(sheetName).Pictures.Count + 1, output.Worksheet(sheetName).Pictures.Count);
    }

    [TemplateFact]
    public void フォームコントロールの描画をテンプレートと同じ内容で保持する()
    {
        var filePath = TreatmentPlanGenerator.CreatePlanFile(TestData.FullPatientInfo());

        using var template = SpreadsheetDocument.Open(TestEnvironment.TemplatePath, false);
        using var output = SpreadsheetDocument.Open(filePath, false);
        var outputParts = SheetParts(output).ToDictionary(s => s.Name, s => s.Part);
        var templateSheetsWithVml = SheetParts(template).Where(s => s.Part.VmlDrawingParts.Any()).ToList();
        Assert.NotEmpty(templateSheetsWithVml);
        foreach (var (name, templatePart) in templateSheetsWithVml)
        {
            var outputPart = outputParts[name];
            Assert.NotNull(outputPart.Worksheet.GetFirstChild<Spreadsheet.LegacyDrawing>());
            // ClosedXML は保存時にVMLの書式（改行・空白）を整え直すため、XMLとして比較する
            Assert.True(XNode.DeepEquals(ReadVml(templatePart), ReadVml(outputPart)), name);
        }
    }

    private static byte[] ReadVbaProject(string path)
    {
        using var zip = ZipFile.OpenRead(path);
        using var stream = zip.GetEntry("xl/vbaProject.bin")!.Open();
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private static XElement ReadVml(WorksheetPart part)
    {
        using var stream = part.VmlDrawingParts.Single().GetStream();
        return XElement.Load(stream);
    }

    private static IEnumerable<(string Name, WorksheetPart Part)> SheetParts(SpreadsheetDocument document)
        => document.WorkbookPart!.Workbook.Sheets!.Elements<Spreadsheet.Sheet>()
            .Select(s => (s.Name!.Value!, (WorksheetPart)document.WorkbookPart.GetPartById(s.Id!.Value!)));
}
