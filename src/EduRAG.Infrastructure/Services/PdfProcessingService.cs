using Microsoft.Extensions.Configuration;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace EduRAG.Infrastructure.Services;

public class PdfProcessingService
{
    private readonly int _chunkSize;
    private readonly int _chunkOverlap;

    public PdfProcessingService(IConfiguration config)
    {
        _chunkSize    = int.Parse(config["Chunking:ChunkSize"]    ?? "500");
        _chunkOverlap = int.Parse(config["Chunking:ChunkOverlap"] ?? "50");
    }

    public List<(string Text, int PageNumber)> ExtractAndChunk(Stream pdfStream)
    {
        var result = new List<(string, int)>();
        using var doc = PdfDocument.Open(pdfStream);

        var allWords = new List<(string Word, int Page)>();
        foreach (var page in doc.GetPages())
        {
            var words = page.GetWords().Select(w => w.Text).ToList();
            foreach (var w in words)
                allWords.Add((w, page.Number));
        }

        int step = _chunkSize - _chunkOverlap;
        for (int i = 0; i < allWords.Count; i += step)
        {
            var slice    = allWords.Skip(i).Take(_chunkSize).ToList();
            if (slice.Count == 0) break;
            var text     = string.Join(" ", slice.Select(w => w.Word));
            var pageNum  = slice[slice.Count / 2].Page;
            result.Add((text, pageNum));
        }

        return result;
    }
}
