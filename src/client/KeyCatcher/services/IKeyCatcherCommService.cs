namespace KeyCatcher.services
{
    public interface IKeyCatcherCommService
    {
        Task<bool> SendTextAsync(string text);
        Task<bool> SendLongMessageAsync(string text);
        Task<string?> GetConfigAsync();
        Task<bool> SendConfigAsync(string configJson);
        Task<bool> ProbeAsync();
    }
}
