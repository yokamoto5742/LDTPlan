using LDTPlan.Services;

namespace LDTPlan.Tests;

public sealed class PatientCsvServiceTests : IDisposable
{
    private const string Line1 = "x,x,123,山田太郎,ヤマダタロウ,1,19600315,x,x,10,佐藤医師,x,x,3,内科";
    private const string Line2 = "x,x,456,鈴木花子,スズキハナコ,2,1975/08/01,x,x,20,田中医師,x,x,5,外科";

    private readonly TestEnvironment _env = new();

    public void Dispose() => _env.Dispose();

    [Fact]
    public void 患者CSVの各列を読み込む()
    {
        _env.WritePatientCsv(Line1, Line2);

        var records = PatientCsvService.LoadPatientData();

        Assert.Equal(2, records.Count);
        var first = records[0];
        Assert.Equal(123, first.PatientId);
        Assert.Equal("山田太郎", first.Name);
        Assert.Equal("ヤマダタロウ", first.Kana);
        Assert.Equal(Constants.GenderMale, first.Gender);
        Assert.Equal(new DateTime(1960, 3, 15), first.Birthdate);
        Assert.Equal(10, first.DoctorId);
        Assert.Equal("佐藤医師", first.DoctorName);
        Assert.Equal(3, first.DepartmentId);
        Assert.Equal("内科", first.Department);
        Assert.Equal(Constants.GenderFemale, records[1].Gender);
        Assert.Equal(new DateTime(1975, 8, 1), records[1].Birthdate);
    }

    [Fact]
    public void 先頭3行だけを読み込む()
    {
        _env.WritePatientCsv(Line1, Line2, Line1, Line2);

        var records = PatientCsvService.LoadPatientData();

        Assert.Equal(3, records.Count);
    }

    [Fact]
    public void 列数が足りない行は読み飛ばす()
    {
        _env.WritePatientCsv("x,x,999,列不足", Line1);

        var records = PatientCsvService.LoadPatientData();

        Assert.Equal(123, Assert.Single(records).PatientId);
    }

    [Fact]
    public void 患者IDが数値でない行は読み飛ばす()
    {
        _env.WritePatientCsv(Line1.Replace(",123,", ",山田,"), Line2);

        var records = PatientCsvService.LoadPatientData();

        Assert.Equal(456, Assert.Single(records).PatientId);
    }

    [Fact]
    public void 患者CSVがない場合は例外を投げる()
    {
        Assert.Throws<FileNotFoundException>(() => PatientCsvService.LoadPatientData());
    }

    [Theory]
    [InlineData("a,b,c", new[] { "a", "b", "c" })]
    [InlineData("a,,c", new[] { "a", "", "c" })]
    [InlineData("\"a,b\",c", new[] { "a,b", "c" })]
    [InlineData("\"a\"\"b\",c", new[] { "a\"b", "c" })]
    [InlineData("", new[] { "" })]
    public void CSVの1行を分割する(string line, string[] expected)
    {
        Assert.Equal(expected, PatientCsvService.SplitCsvLine(line));
    }
}
