using System.CommandLine.Invocation;
using Deneb.Configuration;
using Deneb.Models;
using Deneb.Services;
using Deneb.Utils;
using Djinn.Utils;

namespace Deneb.Commands.Handlers;

public class DownloadCommandHandler : ICommandHandler
{
    private readonly DenebConfig _config = DenebConfig.Load();
    private readonly OpenLibraryService _openLibraryService = new();
    private readonly RetryableQueue<Book> _downloadQueue = new();
    private readonly LibraryService _libraryService;
    
    public DownloadCommandHandler()
    {
        _libraryService = new LibraryService(new DirectoryInfo(_config.LibraryPath));
    }

    public async Task<int> InvokeAsync(InvocationContext context)
    { 
        ParseOptions(context);

        if (await HandleBookDownloads(context))
            return 1;

        return await PerformDownloads(context);
    }

    public int Invoke(InvocationContext context)
    {
        throw new NotImplementedException();
    }
    
    private void ParseOptions(InvocationContext context)
    {
        if (_config.Verbose)
            Log.Level = LogLevel.Verbose;
    }

    private async Task<bool> HandleBookDownloads(InvocationContext context)
    {
        var author = context.ParseResult.GetValueForOption(DownloadCommand.Author);
        var title = context.ParseResult.GetValueForOption(DownloadCommand.Title);
        
        if (author is null && title is null)
            return false;

        var books = await _openLibraryService.FindBooks(title, author);

        if (books is null)
        {
            Log.Error("Unable to locate any books");
            return true;
        }
        
        switch (books.Count)
        {
            case 0:
                return true;
            case 1:
                _downloadQueue.Enqueue(books[0]);
                break;
            default:
            {
                var interactive = context.ParseResult.GetValueForOption(DownloadCommand.Interactive);

                if (interactive)
                {
                    var selectList = new SelectList(books.Select(x =>
                    {
                        var sb = new FixedWidthStringBuilder();

                        sb.Append(x.Title, 60);
                        sb.Append(x.Author.Name, 40);

                        var publishedOn = x.PublishedOn;
                        if (publishedOn is not null)
                        {
                            sb.Append(publishedOn.ToString(), 20);
                        }

                        return sb.ToString();
                    }).ToList());

                    var selectedBookIndex = selectList.Show($"Please select a book:");
                    var selectedBook = books[selectedBookIndex];

                    _downloadQueue.Enqueue(selectedBook);
                }
                else
                {
                    _downloadQueue.Enqueue(books[0]);
                }

                break;
            }
        }

        await PerformDownloads(context);
        
        return true;
    }

    private async Task<int> PerformDownloads(InvocationContext context)
    {
        // var replace = context.ParseResult.GetValueForOption(DownloadCommand.Replace);

        while (_downloadQueue.TryDequeue(out var book))
        {
            if (_libraryService.Contains(book))
            {
                Log.Warning($"Book {book!.Title} by {book!.Author.Name} already exists in library");
                continue;
            }
            
            var libGenDownloader = new LibGenDownloader(book!);

            try
            {
                var epub = await libGenDownloader.Download();

                if (epub is null)
                {
                    _downloadQueue.Enqueue(book!);
                    continue;
                }

                _libraryService.Add(book!, epub.FilePath);
                
                Log.Success($"Downloaded {book!.Title} by {book!.Author.Name}");
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to download book");
                _downloadQueue.Enqueue(book!);
            }
        }

        return 0;
    }
}