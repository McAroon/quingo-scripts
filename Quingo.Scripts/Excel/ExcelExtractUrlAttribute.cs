using System.Reflection;

namespace Quingo.Scripts.Excel;

[AttributeUsage(AttributeTargets.Property)]
public class ExcelExtractUrlAttribute(string fromPropertyName) : Attribute
{
    public string FromPropertyName { get; } = fromPropertyName;

    public static List<(PropertyInfo prop, string fromPropName)> GetPropertiesWithAttribute<T>()
    {
        var props = typeof(T).GetProperties();
        List<(PropertyInfo prop, string fromPropName)> urlProps = [];
        foreach (var propertyInfo in props)
        {
            var attrs = propertyInfo.GetCustomAttributes(typeof(ExcelExtractUrlAttribute), true);
            if (attrs.FirstOrDefault() is not ExcelExtractUrlAttribute attr) continue;
            var fromPropName = attr.FromPropertyName;
            urlProps.Add((propertyInfo, fromPropName));
        }
        
        return urlProps;
    }
}