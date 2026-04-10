using TTS.Domain.Models.Schema;

namespace TTS.Application.Interfaces;

public interface ISchemaMetadataProvider
{
    Task<AllowedSchemaMetadata> GetAllowedSchemaAsync(CancellationToken cancellationToken);
}
