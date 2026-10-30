namespace LDTPlan.Tests;

/// <summary>列を追加したときにテストデータの更新漏れを検出する（null の列があると複写漏れを見逃す）</summary>
public class TestDataTests
{
    [Fact]
    public void FullPatientInfoは全列に値が入っている()
        => AssertNoNullProperty(TestData.FullPatientInfo());

    [Fact]
    public void FullPlanTemplateは全列に値が入っている()
        => AssertNoNullProperty(TestData.FullPlanTemplate());

    private static void AssertNoNullProperty<T>(T value)
    {
        var nullProperties = typeof(T).GetProperties()
            .Where(p => p.GetValue(value) == null)
            .Select(p => p.Name);
        Assert.Empty(nullProperties);
    }
}
