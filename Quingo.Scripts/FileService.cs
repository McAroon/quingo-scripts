using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Options;

namespace Quingo.Scripts;

public class FileService
{
    private readonly ScriptsSettings _settings;

    public FileService(IOptions<ScriptsSettings> settings)
    {
        _settings = settings.Value;
    }

    public string FilePath(string filename)
    {
        var rootDir = Environment.ExpandEnvironmentVariables(_settings.FootbingoFileDirectory);
        return Path.Combine(rootDir, filename);
    }

    public string ReadTextFile(string filePath)
    {
        return File.ReadAllText(FilePath(filePath));
    }

    public void SaveTextFile(string filePath, string data)
    {
        var path = FilePath(filePath);
        File.WriteAllText(path, data);
    }
    
    public List<T> ReadCsv<T>(string path)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";",
            TrimOptions = TrimOptions.Trim,
            HeaderValidated = null,
            MissingFieldFound = null,
        };
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(fs);
        using var csv = new CsvReader(reader, config);

        var result = csv.GetRecords<T>().ToList();
        return result;
    }

    public void SaveCsv<T>(List<T> data, string path)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";",
        };
        using var writer = new StreamWriter(path);
        using var csv = new CsvWriter(writer, config);
        csv.WriteRecords(data);
    }
}