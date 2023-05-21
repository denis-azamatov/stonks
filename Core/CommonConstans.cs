namespace Core;

public static class CommonConstants
{
    public const string ENVIRONMENT_VAR_NAME = "ASPNETCORE_ENVIRONMENT";

    public static readonly string EnvironmentName = Environment.GetEnvironmentVariable(ENVIRONMENT_VAR_NAME)!;

    public const string LOG_OUTPUT_TEMPLATE = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}";
}
