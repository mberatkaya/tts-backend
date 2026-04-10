using TTS.Domain.Models.Schema;
using TTS.Domain.Models.Validation;

namespace TTS.Application.Interfaces;

public interface ISqlSafetyValidator
{
    SqlValidationResult Validate(string sql, AllowedSchemaMetadata allowedSchemaMetadata);
}
