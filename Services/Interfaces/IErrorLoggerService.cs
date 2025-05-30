public interface IErrorLoggerService
{
    Task LogAsync(Exception ex, string source);
}