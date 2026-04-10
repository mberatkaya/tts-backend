namespace TTS.Application.Policies;

public interface IRoleBasedSqlPolicy
{
    Task<string> ApplyAsync(string sql, RoleBasedSqlPolicyContext context, CancellationToken cancellationToken);
}
