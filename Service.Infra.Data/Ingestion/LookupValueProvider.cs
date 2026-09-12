using Service.Domain.Interfaces;

namespace Service.Infra.Data.Ingestion
{
    public class LookupValueProvider : ILookupValueProvider
    {
        private readonly IProductRepository _productRepository;
        private readonly ICenterRepository _centerRepository;

        public LookupValueProvider(IProductRepository productRepository, ICenterRepository centerRepository)
        {
            _productRepository = productRepository;
            _centerRepository = centerRepository;
        }

        public async Task<Dictionary<string, string>> ResolveAsync(string entity, string by, IEnumerable<string> values, CancellationToken cancellationToken = default)
        {
            var valueList = values.ToList();
            if (valueList.Count == 0)
                return [];

            if (string.Equals(entity, "Product", StringComparison.OrdinalIgnoreCase) && string.Equals(by, "Reference", StringComparison.OrdinalIgnoreCase))
            {
                var ids = await _productRepository.GetIdsByReferencesAsync(valueList, cancellationToken);
                return ids.ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
            }

            if (string.Equals(entity, "Center", StringComparison.OrdinalIgnoreCase) && string.Equals(by, "Code", StringComparison.OrdinalIgnoreCase))
            {
                var ids = await _centerRepository.GetIdsByCodesAsync(valueList, cancellationToken);
                return ids.ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
            }

            throw new NotSupportedException($"Unsupported ingestion lookup '{entity}.{by}'.");
        }
    }
}
