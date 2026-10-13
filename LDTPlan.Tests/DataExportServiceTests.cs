using LDTPlan.Data;
using LDTPlan.Models;
using LDTPlan.Services;

namespace LDTPlan.Tests;

public sealed class DataExportServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();

    public void Dispose() => _env.Dispose();

    [Fact]
    public void 出力したCSVを取り込むと全列が元の値に戻る()
    {
        var original = TestData.FullPatientInfo();
        using (var db = new AppDbContext())
        {
            db.PatientInfos.Add(original);
            db.PatientInfos.Add(new PatientInfo { PatientId = 456 }); // 空欄だらけの行
            db.SaveChanges();
        }

        var (fileName, _, error) = DataExportService.ExportToCsv(_env.ExportFolder);
        Assert.Null(error);
        var importError = DataExportService.ImportFromCsv(Path.Combine(_env.ExportFolder, fileName!));
        Assert.Null(importError);

        using var verify = new AppDbContext();
        var rows = verify.PatientInfos.OrderBy(p => p.Id).ToList();
        Assert.Equal(4, rows.Count);
        foreach (var property in typeof(PatientInfo).GetProperties().Where(p => p.Name != nameof(PatientInfo.Id)))
        {
            Assert.Equal(property.GetValue(original), property.GetValue(rows[2]));
            // 空欄は、文字列の列では空文字、それ以外の列では null として取り込まれる
            var emptyValue = property.PropertyType == typeof(string) ? "" : null;
            Assert.Equal(property.GetValue(rows[1]) ?? emptyValue, property.GetValue(rows[3]));
        }
    }

    [Fact]
    public void ファイル名がpatient_infoで始まらないCSVは取り込まない()
    {
        var path = Path.Combine(_env.Root, "other.csv");
        File.WriteAllText(path, "id,patient_id\n1,123\n");

        var error = DataExportService.ImportFromCsv(path);

        Assert.Equal(Constants.MsgCsvImportInvalidFile, error);
        using var db = new AppDbContext();
        Assert.Empty(db.PatientInfos);
    }
}
