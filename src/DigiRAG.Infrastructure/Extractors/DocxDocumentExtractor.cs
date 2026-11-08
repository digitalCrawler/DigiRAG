using System.Text;
using DigiRAG.Application.Ingestion;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DigiRAG.Infrastructure.Extractors;

public class DocxDocumentExtractor : IDocumentExtractor
{
    public bool CanHandle(string contentType, string fileName)
    {
        return contentType.Contains("wordprocessingml") ||
               fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase);
    }

    public Task<ExtractedDocument> ExtractAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        var result = new ExtractedDocument { FileName = fileName };

        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        memoryStream.Position = 0;

        using var wordDoc = WordprocessingDocument.Open(memoryStream, false);
        var body = wordDoc.MainDocumentPart?.Document.Body;
        if (body == null)
        {
            return Task.FromResult(result);
        }

        var sb = new StringBuilder();
        foreach (var element in body.Elements())
        {
            if (element is Paragraph p)
            {
                var text = p.InnerText;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    sb.AppendLine(text);
                }
            }
            else if (element is Table t)
            {
                foreach (var row in t.Elements<TableRow>())
                {
                    var cells = row.Elements<TableCell>().Select(c => c.InnerText.Trim());
                    sb.AppendLine("| " + string.Join(" | ", cells) + " |");
                }
                sb.AppendLine();
            }
        }

        result.Pages.Add(new ExtractedPage
        {
            PageNumber = 1,
            Text = sb.ToString()
        });

        return Task.FromResult(result);
    }
}
