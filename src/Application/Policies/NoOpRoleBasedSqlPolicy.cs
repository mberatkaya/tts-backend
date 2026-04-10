namespace TTS.Application.Policies;

public sealed class NoOpRoleBasedSqlPolicy : IRoleBasedSqlPolicy
{
    public Task<string> ApplyAsync(string sql, RoleBasedSqlPolicyContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(sql);
    }
}
