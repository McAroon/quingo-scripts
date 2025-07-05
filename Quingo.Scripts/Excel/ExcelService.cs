using System.Reflection;
using Microsoft.Extensions.Options;
using OfficeOpenXml;
using OfficeOpenXml.Export.ToCollection;

namespace Quingo.Scripts.Excel;

public class ExcelService(IOptions<ScriptsSettings> settings) : FileService(settings)
{
    public TData ReadExcelFile<TData>(string fileName) where TData : IExcelData, new()
    {
        var path = FilePath(fileName);
        using var package = new ExcelPackage(path);

        var result = new TData();
        var props = typeof(TData).GetProperties();
        foreach (var propertyInfo in props)
        {
            var listType = GetPropertyListType(propertyInfo);
            var sheetName = GetSheetName(propertyInfo);
            if (sheetName == null) continue;

            var mappedList = ReadSheet(sheetName, listType, package);
            propertyInfo.SetValue(result, mappedList);
        }

        return result;
    }

    private object ReadSheet(string sheetName, Type listType, ExcelPackage package)
    {
        try
        {
            var sheet = package.Workbook.Worksheets[sheetName];
            var lastRow = sheet.Cells
                .LastOrDefault(cell => !string.IsNullOrEmpty(cell.Value?.ToString() ?? string.Empty))?.End.Row;
            var lastCol = sheet.Cells[1, 1, 1, sheet.Dimension.End.Column]
                .LastOrDefault(cell => !string.IsNullOrEmpty(cell.Value?.ToString() ?? string.Empty))?.End.Column;
            var cells = sheet.Cells[1, 1, lastRow ?? sheet.Dimension.End.Row, lastCol ?? sheet.Dimension.End.Column];

            var tst = sheet.Cells["B2"].Hyperlink;

            var mi = GetType().GetMethod("ToCollectionWithMappings",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var miGeneric = mi!.MakeGenericMethod(listType);
            var mappedList = miGeneric.Invoke(this, [cells]);
            return mappedList;
        }
        catch (Exception e)
        {
            throw new Exception($"Error parsing sheet {sheetName}", e);
        }
    }

    private string GetSheetName(PropertyInfo propertyInfo)
    {
        var attrs = propertyInfo.GetCustomAttributes(typeof(ExcelSheetAttribute), true);
        var attr = attrs.FirstOrDefault() as ExcelSheetAttribute;
        return attr?.Name;
    }

    private Type GetPropertyListType(PropertyInfo propertyInfo)
    {
        if (!propertyInfo.PropertyType.IsGenericType
            || propertyInfo.PropertyType.GetGenericTypeDefinition() != typeof(List<>))
        {
            throw new ArgumentException("Property is not a List");
        }

        return propertyInfo.PropertyType.GetGenericArguments()[0];
    }

    private List<T> ToCollectionWithMappings<T>(ExcelRange range) where T : class, new()
    {
        var result = range.ToCollectionWithMappings(row =>
        {
            var res = new T();
            row.Automap(res);
            return res;
        }, opts =>
        {
            opts.HeaderRow = 0;
            opts.ConversionFailureStrategy = ToCollectionConversionFailureStrategy.SetDefaultValue;
        });

        var urlProps = ExcelExtractUrlAttribute.GetPropertiesWithAttribute<T>();
        if (urlProps.Count <= 0) return result;

        foreach (var urlProp in urlProps)
        {
            var urlCol = range[1, 1, 1, range.End.Column]
                .LastOrDefault(cell => cell.Value?.ToString() == urlProp.fromPropName)?.End.Column;
            if (urlCol == null) continue;
            
            foreach (var (entry, idx) in result.Select((e, i) => (e, i)))
            {
                var cell = range[idx + 2, urlCol.Value];
                var hl = cell.Hyperlink;
                if (hl != null)
                {
                    urlProp.prop.SetValue(entry, hl.AbsoluteUri);
                }
            }
        }

        return result;
    }
}