namespace Momento.Etl.Validation;

public static class ErrorMessage
{
    public static readonly string INVALID_JSON = "invalid_json";
    public static readonly string DATA_TYPE_NOT_SUPPORTED = "data_type_not_supported";
    public static readonly string DATA_TOO_LARGE = "data_too_large";
    public static readonly string ALREADY_EXPIRED = "already_expired";
    public static readonly string TTL_TOO_LONG = "ttl_too_long";
    public static readonly string NO_TTL = "no_ttl";
}
