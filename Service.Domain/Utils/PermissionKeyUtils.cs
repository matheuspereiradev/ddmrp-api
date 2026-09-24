using System.Text.RegularExpressions;

namespace Service.Domain.Utils
{
    public static class PermissionKeyUtils
    {
        private static readonly Regex ApiPrefixPattern = new(@"^api/", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex RouteParameterPattern = new(@"\{[^}]+\}", RegexOptions.Compiled);
        private static readonly Regex MultipleSlashesPattern = new(@"/{2,}", RegexOptions.Compiled);

        public static string BuildKey(string routeTemplate, string httpMethod)
        {
            var template = routeTemplate.Trim('/');
            template = ApiPrefixPattern.Replace(template, string.Empty);
            template = RouteParameterPattern.Replace(template, string.Empty);
            template = MultipleSlashesPattern.Replace(template, "/");
            template = template.Trim('/').ToLowerInvariant();

            return $"{template}:{httpMethod.ToUpperInvariant()}";
        }
    }
}
