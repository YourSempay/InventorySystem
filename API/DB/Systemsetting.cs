using System;
using System.Collections.Generic;

namespace API.DB;

public partial class Systemsetting
{
    public int Id { get; set; }

    public string SettingKey { get; set; } = null!;

    public string SettingValue { get; set; } = null!;

    public string? Description { get; set; }
}
