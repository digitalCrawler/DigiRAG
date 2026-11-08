using DigiRAG.Application.Ingestion;
using UglyToad.PdfPig;

namespace DigiRAG.Infrastructure.Extractors;

public class PdfDocumentExtractor : IDocumentExtractor
{
    public bool CanHandle(string contentType, string fileName)
    {
        return contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) ||
               fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
    }

    public Task<ExtractedDocument> ExtractAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        var result = new ExtractedDocument { FileName = fileName };

        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        memoryStream.Position = 0;

        using var pdfDocument = PdfDocument.Open(memoryStream);

        foreach (var page in pdfDocument.GetPages())
        {
            var pageText = page.Text;
            if (string.IsNullOrWhiteSpace(pageText))
            {
                continue;
            }

            result.Pages.Add(new ExtractedPage
            {
                PageNumber = page.Number,
                Text = pageText.Trim()
            });
        }

        return Task.FromResult(result);
    }
}
