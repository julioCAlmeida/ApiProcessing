using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Api.Dtos
{
    public sealed record ProcessingCreateDto(
        [param: Required] Guid ScriptId,
        [param: Required] JsonElement Data
    );
}
