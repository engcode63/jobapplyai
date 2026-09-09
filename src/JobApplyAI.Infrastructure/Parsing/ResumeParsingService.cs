using JobApplyAI.Application.Interfaces;
using JobApplyAI.Core.Entities;
using UglyToad.PdfPig;

namespace JobApplyAI.Infrastructure.Parsing;

/// <summary>
/// Extracts raw text from an uploaded resume (PDF supported via PdfPig). Structured section
/// extraction (skills/experience/education) is delegated to the AI model for robustness,
/// since resume layouts vary too much for reliable rule-based parsing alone.
/// </summary>
public class ResumeParsingService : IResumeParsingService
{
    private readonly IAiCompletionService _ai;

    public ResumeParsingService(IAiCompletionService ai)
    {
        _ai = ai;
    }

    public async Task<(string RawText, ResumeStructuredData Structured)> ParseAsync(
        Stream fileStream, string fileName, CancellationToken ct = default)
    {
        var rawText = fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
            ? ExtractPdfText(fileStream)
            : await new StreamReader(fileStream).ReadToEndAsync(ct);

        var structured = await ExtractStructuredDataAsync(rawText, ct);
        return (rawText, structured);
    }

    private static string ExtractPdfText(Stream fileStream)
    {
        using var document = PdfDocument.Open(fileStream);
        var text = new System.Text.StringBuilder();
        foreach (var page in document.GetPages())
        {
            text.AppendLine(page.Text);
        }
        return text.ToString();
    }

    private const string StructuringPrompt =
        """
        Extract the following from the resume text as strict JSON matching this shape (no commentary,
        JSON only): { "summary": string|null, "skills": string[], "experience": [{ "company": string,
        "title": string, "startDate": string|null, "endDate": string|null, "highlights": string[] }],
        "education": [{ "institution": string, "qualification": string, "year": string|null }] }
        """;

    private async Task<ResumeStructuredData> ExtractStructuredDataAsync(string rawText, CancellationToken ct)
    {
        try
        {
            var json = await _ai.CompleteAsync(StructuringPrompt, rawText, ct);
            var parsed = System.Text.Json.JsonSerializer.Deserialize<ResumeStructuredData>(
                json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return parsed ?? new ResumeStructuredData();
        }
        catch
        {
            // AI structuring is best-effort; raw text is always preserved regardless.
            return new ResumeStructuredData();
        }
    }
}
