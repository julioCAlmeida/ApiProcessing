using Api.Models;
using System.Text.Json;

namespace Api.Dtos
{
    public sealed record ProcessingJobDto(
         Guid Id,
         Guid ScriptId,
         JobStatus Status,
         DateTime CreatedAt,
         DateTime? FinishedAt,
         JsonElement? Result
     );
}
