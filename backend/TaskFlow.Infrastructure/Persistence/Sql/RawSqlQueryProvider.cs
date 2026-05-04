using System.Xml.Linq;

namespace TaskFlow.Infrastructure.Persistence.Sql;

internal static class RawSqlQueryProvider
{
    private const string ResourceName = "TaskFlow.Infrastructure.Persistence.Sql.RawSqlQueries.xml";

    private static readonly Lazy<IReadOnlyDictionary<string, string>> Queries = new(LoadQueries);

    public static string GetByKey(string key)
    {
        if (Queries.Value.TryGetValue(key, out var sql))
        {
            return sql;
        }

        throw new KeyNotFoundException($"Raw SQL query key '{key}' was not found.");
    }

    private static IReadOnlyDictionary<string, string> LoadQueries()
    {
        var assembly = typeof(RawSqlQueryProvider).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded SQL resource '{ResourceName}' was not found.");

        var document = XDocument.Load(stream);
        var queries = document.Root?.Elements("query")
            .Select(e => new
            {
                Key = (string?)e.Attribute("key"),
                Sql = (e.Value ?? string.Empty).Trim()
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Key) && !string.IsNullOrWhiteSpace(x.Sql))
            .ToDictionary(x => x.Key!, x => x.Sql, StringComparer.Ordinal)
            ?? new Dictionary<string, string>(StringComparer.Ordinal);

        return queries;
    }
}
