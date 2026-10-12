using Jint;

namespace MoreRewards.Tests;

/// <summary>
/// page.js exactly as the plugin embeds it, run in Jint on top of FakePage.js, a Root.html with only the parts the
/// script touches. Tests talk to it in JavaScript and read back plain strings, numbers and booleans.
/// </summary>
internal sealed class PageScript
{
    private readonly Engine _engine = new();

    public PageScript()
    {
        _engine.Execute(Resource("MoreRewards.Tests.FakePage.js"));
        Install();
    }

    /// <summary>What the plugin sends when the page answers that the script is missing.</summary>
    public void Install() => _engine.Execute(Resource("MoreRewards.page.js"));

    /// <summary>One round of the script's own timer, as the browser runs it every quarter second.</summary>
    public void Tick() => _engine.Execute("runTimers()");

    /// <summary>What the plugin's check reads.</summary>
    public string Take() => Text("window.__slmorerewards.take()");

    public void Run(string statements) => _engine.Execute(statements);

    public string Text(string expression) => _engine.Evaluate($"String({expression})").AsString();

    public bool Is(string expression) => _engine.Evaluate($"!!({expression})").AsBoolean();

    public double Number(string expression) => _engine.Evaluate($"Number({expression})").AsNumber();

    private static string Resource(string name)
    {
        using var stream = typeof(PageScript).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException(name + " is not embedded in the test assembly");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
