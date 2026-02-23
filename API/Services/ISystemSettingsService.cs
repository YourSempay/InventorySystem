namespace API.Services;

public interface ISystemSettingsService
{
    Task<string> GetSettingValueAsync(string key);
    Task<int> GetSettingValueAsIntAsync(string key);
    Task<bool> GetSettingValueAsBoolAsync(string key);
}