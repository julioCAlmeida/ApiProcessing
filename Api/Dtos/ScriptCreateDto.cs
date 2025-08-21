using System.ComponentModel.DataAnnotations;

namespace Api.Dtos
{
    public sealed record ScriptCreateDto(
        [param: Required, StringLength(100)] string Name,
        [param: Required] string Code
    );
}
