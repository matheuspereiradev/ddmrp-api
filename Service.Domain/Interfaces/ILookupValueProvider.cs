namespace Service.Domain.Interfaces
{
    public interface ILookupValueProvider
    {
        Task<Dictionary<string, string>> ResolveAsync(string entity, string by, IEnumerable<string> values, CancellationToken cancellationToken = default);
    }
}
