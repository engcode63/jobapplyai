using JobApplyAI.Core.Entities;

namespace JobApplyAI.Application.Interfaces;

/// <summary>Extracts text and best-effort structured data from an uploaded resume file.</summary>
public interface IResumeParsingService
{
    Task<(string RawText, ResumeStructuredData Structured)> ParseAsync(
        Stream fileStream, string fileName, CancellationToken ct = default);
}
