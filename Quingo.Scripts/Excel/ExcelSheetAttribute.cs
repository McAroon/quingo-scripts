namespace Quingo.Scripts.Excel;

[AttributeUsage(AttributeTargets.Property)]
public class ExcelSheetAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}