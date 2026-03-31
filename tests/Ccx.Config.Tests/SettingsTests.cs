using System.Text.Json;
using Ccx.Config;
using FluentAssertions;

namespace Ccx.Config.Tests;

public class SettingsTests
{
    [Fact]
    public void Settings_Deserialization_Works()
    {
        var json = """{"model": "claude-opus-4", "permissions": {"allow": ["Read", "Glob"]}}""";
        var settings = JsonSerializer.Deserialize(json, CcxSettingsContext.Default.CcxSettings);

        settings.Should().NotBeNull();
        settings!.Model.Should().Be("claude-opus-4");
        settings.Permissions!.Allow.Should().Contain("Read");
    }

    [Fact]
    public void Settings_Serialization_SkipsNulls()
    {
        var settings = new CcxSettings { Model = "test" };
        var json = JsonSerializer.Serialize(settings, CcxSettingsContext.Default.CcxSettings);

        json.Should().Contain("model");
        json.Should().NotContain("permissions");
    }
}

public class SettingsCascadeTests
{
    [Fact]
    public void GetModel_CliOverridesAll()
    {
        var settings = new CcxSettings { Model = "settings-model" };
        var cascade = new SettingsCascade(settings);
        cascade.SetOverride("model", "override-model");

        cascade.GetModel("cli-model").Should().Be("cli-model");
    }

    [Fact]
    public void GetModel_OverrideOverridesSettings()
    {
        var settings = new CcxSettings { Model = "settings-model" };
        var cascade = new SettingsCascade(settings);
        cascade.SetOverride("model", "override-model");

        cascade.GetModel().Should().Be("override-model");
    }

    [Fact]
    public void GetModel_FallsBackToSettings()
    {
        var settings = new CcxSettings { Model = "settings-model" };
        var cascade = new SettingsCascade(settings);

        cascade.GetModel().Should().Be("settings-model");
    }

    [Fact]
    public void GetModel_FallsBackToDefault()
    {
        var settings = new CcxSettings();
        var cascade = new SettingsCascade(settings);

        cascade.GetModel().Should().Contain("claude");
    }
}
