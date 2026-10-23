namespace LDTPlan.Tests;

/// <summary>配布先のExcelテンプレートがある環境でだけ実行するテスト</summary>
public sealed class TemplateFactAttribute : FactAttribute
{
    public TemplateFactAttribute()
    {
        if (!File.Exists(TestEnvironment.TemplatePath))
            Skip = $"Excelテンプレートがありません: {TestEnvironment.TemplatePath}";
    }
}
