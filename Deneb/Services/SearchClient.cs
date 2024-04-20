using System.Diagnostics;
using Deneb.Utils;
using OpenLibraryNET;
using OpenLibraryNET.Data;

namespace Deneb.Services;

public class SearchClient
{
    private readonly OpenLibraryClient _client = new();

    public async Task<IReadOnlyList<SearchResult>> FindBooks(string? title, string? author)
    {
        var works = new List<OLWorkData>();

        var query = string.Empty;
        switch (string.IsNullOrEmpty(title))
        {
            case false when !string.IsNullOrEmpty(author):
                query = $"title:{title} AND author:{author} AND language:eng";
                break;
            case false:
                query = $"title:{title} AND language:eng";
                break;
            default:
            {
                if (!string.IsNullOrEmpty(author))
                    query = $"author:{author} AND language:eng";
                break;
            }
        }

        var results = await _client.Search.GetSearchResultsAsync(query, []);
        if (results is not null)
            works.AddRange(results);

        return works.Select(WorkToSearchResult).ToList();
    }

    private static SearchResult WorkToSearchResult(OLWorkData work)
    {
        try
        {
            var authors = new List<string>();
            if (work.ExtensionData.ContainsKey("author_name"))
                authors = work.ExtensionData["author_name"].Values<string>().ToList();

            int? firstPublishedYear = null;
            if (work.ExtensionData.ContainsKey("first_publish_year"))
                firstPublishedYear = (int)work.ExtensionData["first_publish_year"];

            string? isbn = null;
            if (work.ExtensionData.ContainsKey("isbn"))
                isbn = work.ExtensionData["isbn"].Values<string>().FirstOrDefault(x => !string.IsNullOrEmpty(x) && IsbnValidator.IsValidIsbn(x));

            string? description = null;
            if (work.ExtensionData.ContainsKey("first_sentence"))
                description = work.ExtensionData["first_sentence"].Values<string>().FirstOrDefault();

            return new SearchResult
            {
                Identifier = work.Key.Replace("/works/", ""),
                Isbn = isbn,
                Title = work.Title,
                Authors = authors,
                Description = description,
                FirstPublishedOn = firstPublishedYear,
                PublishedOn = null
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            Debug.WriteLine(ex.StackTrace);
            throw;
        }
    }
}