using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Deneb.Ebooks;
using Deneb.Utils;

namespace Deneb.Services;

public partial class SearchResultViewModel
{
    private Guid Id = Guid.NewGuid();
    
    public List<string> Authors { get; set; }

    public string TruncatedAuthor
    {
        get
        {
            if (Authors.Count == 0)
                return "Unknown Author";
            
            var author = Authors[0];
            
            if (author.Length > 50)
                return author.Substring(0, 50).Trim() + "...";
            
            return author;
        }
    }
    
    public string Title { get; set; }
    
    public string TruncatedTitle => Title.Length > 50 ? Title.Substring(0, 50) + "..." : Title;
    
    public List<string> Urls { get; set; }
    
    public string? Isbn { get; set; }
    
    public string FileType { get; set; }
    
    public string Size { get; set; }
    
    public int Score { get; set; }
    
    public SearchResultViewModel(List<string> authors, string title, List<string> urls, string? isbn, string fileType, string size, int score)
    {
        Authors = authors;
        Title = title;
        Urls = urls;
        Isbn = isbn;
        FileType = fileType;
        Size = size;
        Score = score;
    }
}

public class LibGenClient
{
    private readonly HttpClient _client;
    private readonly HtmlParser _parser;
    
    public LibGenClient()
    {
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (HttpRequestMessage requestMessage, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors sslErrors) => true;
        _client = new HttpClient(handler);
        
        _parser = new HtmlParser();
    }
    
    public async Task<List<SearchResultViewModel>> Search(string author, string title)
    {
        if (string.IsNullOrEmpty(author) && string.IsNullOrEmpty(title))
            return [];
        
        var query = Uri.EscapeDataString($"{author} {title}");
        var format = "epub";

        var currentPage = 1;
        int? numPages = null;
        var results = new List<SearchResultViewModel>();

        while (true)
        {
            if (results.Count >= 300)
                break;
            
            var url = $"https://libgen.is/fiction/?q={query}&language=English&format={format}&page={currentPage}";
            var response = await _client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            var document = await _parser.ParseDocumentAsync(content);

            var rows = document.QuerySelectorAll(".catalog tbody tr");

            if (!numPages.HasValue)
            {
                var numResultsElement = document.QuerySelector("div.catalog_paginator > div");
                if (numResultsElement is null)
                    break;
                var numResultsText = numResultsElement.TextContent;
                numResultsText = new string(numResultsText.Where(char.IsDigit).ToArray());
                var numResults = int.Parse(numResultsText);
                numPages = (int)Math.Ceiling(numResults / (double)rows.Length);
            }

            foreach (var row in rows)
            {
                var cells = row.QuerySelectorAll("td");

                var authorListItems = cells[0].QuerySelectorAll("li");
                if (authorListItems.Length == 0) continue;

                var authors = new List<string>();
                foreach (var li in authorListItems)
                {
                    authors.Add(FixAuthor(li.TextContent));
                }

                var titleElement = cells[2].QuerySelector("a");
                var resultTitle = titleElement.TextContent;

                var isbn = cells[2].QuerySelector("p.catalog_identifier")?.TextContent;
                if (!string.IsNullOrEmpty(isbn))
                {
                    if (isbn.StartsWith("ISBN:"))
                    {
                        isbn = isbn.Replace("ISBN:", "").Trim();
                        
                        if (isbn.Contains(','))
                            isbn = isbn.Split(',')[0].Trim();
                        
                        if (!IsbnValidator.IsValidIsbn(isbn))
                            isbn = null;
                    }
                }

                var links = cells[5].QuerySelectorAll("a");
                if (links.Length == 0) continue;
                var urls = new List<string>();
                foreach (var link in links)
                {
                    var href = link.GetAttribute("href");
                    if (!string.IsNullOrEmpty(href))
                    {
                        urls.Add(href);
                    }
                }

                var fileInfo = cells[4].TextContent.Split('/');
                var fileType = fileInfo[0].Trim().ToLower();
                var size = fileInfo[1].Trim();

                var sizeInBytes = FileSizeToBytes(size);
                var sizeInMegabytes = (float)sizeInBytes / 1024 / 1024;
                var sizeStr = $"{sizeInMegabytes:F1} MB";

                int score;
                switch (string.IsNullOrEmpty(title))
                {
                    case false when !string.IsNullOrEmpty(author):
                        score = FuzzySharp.Fuzz.Ratio(title, resultTitle);
                        score += authors.Max(x => FuzzySharp.Fuzz.Ratio(author, x));
                        score /= 2;
                        break;
                    case false:
                        score = FuzzySharp.Fuzz.Ratio(title, resultTitle);
                        break;
                    default:
                        score = authors.Max(x => FuzzySharp.Fuzz.Ratio(x, author));
                        break;
                }

                var result = new SearchResultViewModel(authors, resultTitle, urls, isbn, fileType, sizeStr, score);
                results.Add(result);
            }

            currentPage++;

            if (currentPage > numPages) 
                break;
        }

        return results.OrderByDescending(x => x.Score).ThenByDescending(x => x.Size).ToList();
    }
    
    public async Task<Epub?> DownloadResult(SearchResultViewModel searchResultViewModel, Action<double>? progressCallback = null, CancellationToken cancellationToken = default)
    {
        var url = searchResultViewModel.Urls[0];
        var response = await _client.GetAsync(url, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        var document = await _parser.ParseDocumentAsync(content);
        
        var primaryLink = document.QuerySelector("#download > h2 > a");
        var secondaryLinks = document.QuerySelectorAll("#download > ul > li > a");
        
        var allLinks = new List<IElement> { primaryLink };
        allLinks.AddRange(secondaryLinks);
        
        foreach (var link in allLinks)
        {
            var downloadUrl = link.GetAttribute("href");

            try
            {
                var tempDir = Path.GetTempPath();
                var fileName = Path.Combine(tempDir, Path.GetRandomFileName() + $".{searchResultViewModel.FileType}");

                var localPath = await TryDownloadFile(downloadUrl, fileName, progressCallback, cancellationToken);

                if (localPath is not null)
                {
                    var epub = Epub.Load(new FileInfo(localPath));
                    return epub;
                }
                
                Log.Warning($"Failed to download file from {downloadUrl}");
            }
            catch(Exception e)
            {
                Log.Warning($"Failed to download file from {downloadUrl}: {e.Message}");
            }
        }

        return null;
    }

    private async Task<string?> TryDownloadFile(string downloadUrl, string fileName, Action<double>? progressCallback = null, CancellationToken cancellationToken = default)
    {
        var response = await _client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var totalBytes = response.Content.Headers.ContentLength ?? 0;
        var bytesDownloaded = 0L;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = new FileStream(fileName, FileMode.Create);
        
        var buffer = new byte[1024];
        int bytesRead;
        
        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }
                
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            bytesDownloaded += bytesRead;
            var percent = Math.Round((double)bytesDownloaded / totalBytes * 100, 2);
            progressCallback?.Invoke(percent);
        }

        return fileName;
    }
    
    private static string FixAuthor(string author)
    {
        if (!author.Contains(',')) 
            return author;
        
        var parts = author.Split(',');
        return parts.Length == 2 ? $"{parts[1].Trim()} {parts[0].Trim()}" : author;
    }

    private static int FileSizeToBytes(string size)
    {
        var parts = size.Split('\u00A0');
        
        if (parts.Length != 2) 
            return 0;

        if (!double.TryParse(parts[0], out var numericSize)) 
            return 0;

        return parts[1].ToLower() switch
        {
            "kb" => (int)(numericSize * 1024),
            "mb" => (int)(numericSize * 1024 * 1024),
            "gb" => (int)(numericSize * 1024 * 1024 * 1024),
            _ => 0
        };
    }
}
