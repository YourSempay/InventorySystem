using API.DB;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public class SystemSettingsService : ISystemSettingsService
{
    private readonly _1135InventorySystemContext db;
    private readonly Dictionary<string, string> cache = new();

    public SystemSettingsService(_1135InventorySystemContext db)
    {
        this.db = db;
    }

    public async Task<string> GetSettingValueAsync(string key)
    {
        if (cache.ContainsKey(key))
            return cache[key];

        var setting = await db.Systemsettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SettingKey == key);

        if (setting == null)
            throw new KeyNotFoundException($"Настройка '{key}' не найдена");

        cache[key] = setting.SettingValue;
        return setting.SettingValue;
    }

    public async Task<int> GetSettingValueAsIntAsync(string key)
    {
        var value = await GetSettingValueAsync(key);
        if (!int.TryParse(value, out var result))
            throw new InvalidOperationException($"Настройка '{key}' не является числом");
        return result;
    }

    public async Task<bool> GetSettingValueAsBoolAsync(string key)
    {
        var value = await GetSettingValueAsync(key);
        if (!bool.TryParse(value, out var result))
            throw new InvalidOperationException($"Настройка '{key}' не является логическим значением");
        return result;
    }
}