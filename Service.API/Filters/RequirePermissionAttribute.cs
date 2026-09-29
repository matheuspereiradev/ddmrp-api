namespace Service.API.Filters
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RequirePermissionAttribute : Attribute
    {
        // When null (the default), the permission key is derived from the route template + HTTP
        // method (see PermissionKeyUtils.BuildKey) — one key per route, the behavior every module
        // besides Importer/Exporter uses. When set, every action sharing the same key is gated by
        // a single permission instead — e.g. every Importer action uses "importer", since having
        // access to run an importer implies access to list/inspect them too.
        public string? Key { get; }

        public RequirePermissionAttribute(string? key = null)
        {
            Key = key;
        }
    }
}
