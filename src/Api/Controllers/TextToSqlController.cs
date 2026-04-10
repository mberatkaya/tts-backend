using Microsoft.AspNetCore.Mvc;
using TTS.Api.DTOs;
using TTS.Application.Interfaces;

namespace TTS.Api.Controllers;

[ApiController]
[Route("api/text-to-sql")]
public sealed class TextToSqlController : ControllerBase
{
    private readonly ITextToSqlService _textToSqlService;

    public TextToSqlController(ITextToSqlService textToSqlService)
    {
        _textToSqlService = textToSqlService;
    }

    [HttpPost("query")]
    [ProducesResponseType(typeof(TextToSqlQueryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TextToSqlQueryResponseDto>> QueryAsync(
        [FromBody] TextToSqlQueryRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _textToSqlService.QueryAsync(request.Question, cancellationToken);

        return Ok(new TextToSqlQueryResponseDto
        {
            Success = result.Success,
            Question = result.Question,
            GeneratedSql = result.GeneratedSql,
            Rows = result.Rows,
            Warnings = result.Warnings,
            Confidence = result.Confidence
        });
    }
}
