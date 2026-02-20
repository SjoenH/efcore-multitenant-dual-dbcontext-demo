using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using ColorCode;
using Markdig;

namespace BankingApi.Infrastructure;

public sealed partial class DocRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    private static readonly CodeColorizer Colorizer = new();

    // resource name → rendered HTML
    private readonly Dictionary<string, string> _cache = new();

    private readonly HttpClient _http;

    public DocRenderer(HttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// Renders and caches each document sequentially. Call once at startup before serving requests.
    /// Mermaid diagram fetches within each document are already throttled to 3 concurrent requests.
    /// </summary>
    public async Task WarmupAsync(params string[] resourceNames)
    {
        foreach (var name in resourceNames)
            await PrimeAsync(name);
    }

    /// <summary>Returns the fully rendered HTML for the given embedded resource.</summary>
    public string GetHtml(string resourceName) =>
        _cache.TryGetValue(resourceName, out var html)
            ? html
            : throw new InvalidOperationException($"Doc '{resourceName}' has not been primed.");

    // -------------------------------------------------------------------------

    private async Task PrimeAsync(string resourceName)
    {
        var markdown = ReadEmbeddedResource(resourceName);
        var html = await RenderAsync(markdown);
        _cache[resourceName] = html;
    }

    private async Task<string> RenderAsync(string markdown)
    {
        // Extract all mermaid blocks and fetch their SVGs, throttled to avoid 503s from mermaid.ink
        var matches = MermaidBlockRegex().Matches(markdown);
        var semaphore = new SemaphoreSlim(3);
        var svgTasks = matches
            .Cast<Match>()
            .Select(async m =>
            {
                await semaphore.WaitAsync();
                try
                {
                    return await FetchSvgAsync(m.Groups[1].Value.Trim());
                }
                finally
                {
                    semaphore.Release();
                }
            });
        var svgs = await Task.WhenAll(svgTasks);

        // Replace each ```mermaid ... ``` block with its inline SVG
        var i = 0;
        var processedMarkdown = MermaidBlockRegex().Replace(markdown, _ => $"\n\nMERMAID_PLACEHOLDER_{i++}\n\n");

        // Render the rest of the markdown to HTML
        var body = Markdown.ToHtml(processedMarkdown, Pipeline);

        // Syntax highlight code blocks with language specified
        body = SyntaxHighlight(body);

        // Swap placeholders for the actual SVGs
        for (var j = 0; j < svgs.Length; j++)
        {
            body = body.Replace($"<p>MERMAID_PLACEHOLDER_{j}</p>", $"""<div class="mermaid-diagram">{svgs[j]}</div>""");
        }

        return body;
    }

    private static string SyntaxHighlight(string html)
    {
        // Find <pre><code class="language-xxx">...</code></pre> blocks and highlight them
        return CodeBlockRegex()
            .Replace(
                html,
                match =>
                {
                    var language = match.Groups[1].Value.ToLowerInvariant();
                    var code = HttpUtility.HtmlDecode(match.Groups[2].Value);

                    var lang = language switch
                    {
                        "csharp" or "c#" or "cs" => Languages.CSharp,
                        "javascript" or "js" => Languages.JavaScript,
                        "sql" => Languages.Sql,
                        "xml" => Languages.Xml,
                        "html" => Languages.Html,
                        "css" => Languages.Css,
                        "powershell" or "ps1" => Languages.PowerShell,
                        "php" => Languages.Php,
                        "java" => Languages.Java,
                        _ => null,
                    };

                    if (lang == null)
                        return match.Value;

                    var highlighted = Colorizer.Colorize(code, lang);
                    return $"""<pre class="highlight"><code>{highlighted}</code></pre>""";
                }
            );
    }

    private async Task<string> FetchSvgAsync(string diagram)
    {
        // mermaid.ink accepts base64-encoded diagram source
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(diagram));
        var url = $"https://mermaid.ink/svg/{encoded}";

        // Retry up to 5 times with exponential back-off (1s, 2s, 4s, 8s) to ride out
        // mermaid.ink rate-limiting (HTTP 503) without failing the entire warmup.
        const int maxAttempts = 5;
        var delay = TimeSpan.FromSeconds(1);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var response = await _http.GetAsync(url);
                if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable && attempt < maxAttempts)
                {
                    await Task.Delay(delay);
                    delay *= 2;
                    continue;
                }

                response.EnsureSuccessStatusCode();
                var svg = await response.Content.ReadAsStringAsync();
                // Strip the XML declaration if present so it embeds cleanly as inline SVG
                return XmlDeclarationRegex().Replace(svg, "").Trim();
            }
            catch (Exception ex) when (attempt == maxAttempts)
            {
                // Fall back to a plain code block so a rendering failure doesn't break the page
                return $"""<pre class="mermaid-fallback"><code>{System.Web.HttpUtility.HtmlEncode(diagram)}</code></pre><p class="mermaid-error">&#9888; Diagram unavailable: {System.Web.HttpUtility.HtmlEncode(ex.Message)}</p>""";
            }
            catch
            {
                await Task.Delay(delay);
                delay *= 2;
            }
        }

        // Unreachable, but satisfies the compiler
        return $"""<pre class="mermaid-fallback"><code>{System.Web.HttpUtility.HtmlEncode(diagram)}</code></pre>""";
    }

    private static string ReadEmbeddedResource(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream =
            assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Embedded resource '{name}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    [GeneratedRegex(@"```mermaid\s*\n([\s\S]*?)```", RegexOptions.Multiline)]
    private static partial Regex MermaidBlockRegex();

    [GeneratedRegex(@"<\?xml[^?]*\?>")]
    private static partial Regex XmlDeclarationRegex();

    [GeneratedRegex(@"<pre><code class=""language-(\w+)"">([\s\S]*?)</code></pre>", RegexOptions.Multiline)]
    private static partial Regex CodeBlockRegex();
}
