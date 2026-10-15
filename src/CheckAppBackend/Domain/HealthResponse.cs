namespace CheckAppBackend.Domain;

public record HealthResponse(string Status, string Service);

public static class ServiceInfo
{
    public const string Name = "check-app-backend";
}
