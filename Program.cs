using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.Models;
using Spectre.Console;

namespace HyperSploit;

/// <summary>
/// Main program class
/// </summary>
public static class Program {
    /// <summary>
    /// Unlock API host
    /// </summary>
    private static string Host => IsGlobal ? "https://unlock.update.intl.miui.com" : "https://unlock.update.miui.com";
    
    /// <summary>
    /// Should global API be used
    /// </summary>
    private static bool IsGlobal;
    
    /// <summary>
    /// Program entrypoint
    /// </summary>
    /// <param name="args">Arguments</param>
    public static async Task Main(string[] args) {
        AnsiConsole.Write(new FigletText("HyperSploit").LeftJustified().Color(Color.Cyan1));
        AnsiConsole.MarkupLine("[green]Welcome to HyperSploit v1.0 by TheAirBlow![/]");
        var root = Extract();
        if (!AdbServer.Instance.GetStatus().IsRunning) {
            AnsiConsole.MarkupLine("[yellow]No ADB server is running, trying to start...[/]");
            var server = new AdbServer();
            var path = Path.Combine(root, "adb");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                path += ".exe";
            var result = await server.StartServerAsync(path);
            if (result != StartServerResult.Started) {
                AnsiConsole.MarkupLine("[red]Failed to start ADB server![/]");
                AnsiConsole.MarkupLine("[red]Install ADB first you're on Linux or MacOS.[/]");
                AnsiConsole.MarkupLine("[red]If you're on Windows - make a GitHub issue.[/]");
                return;
            }
        }

        var client = new AdbClient();
        while (true) {
            var devices = await client.GetDevicesAsync();
            var dict = devices.ToDictionary(x => $"{x.Model} codename {x.Name}", x => x);
            var choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("[cyan]Choose android device to use:[/]")
                .AddChoices(dict.Keys.Append("[yellow]Refresh list[/]")));
            if (!dict.TryGetValue(choice, out var device)) continue;
            AnsiConsole.MarkupLine($"[green]You chose [cyan]{device.Model} codename {device.Name}[/]![/]");
            if (device.State != DeviceState.Online) {
                AnsiConsole.MarkupLine($"[yellow]Chosen device is in an invalid state: {device.State}![/]");
                AnsiConsole.MarkupLine("[yellow]Make sure to authorize your computer if asked to.[/]");
                continue;
            }

            IsGlobal = !AnsiConsole.Confirm("[yellow]Is this device from Mainland China?[/]", false);
            try {
                await Bypass(client, device);
            } catch (Exception e) {
                AnsiConsole.MarkupLine("[red]Unhandled exception caught, can't continue![/]");
                Console.WriteLine(e);
            }
            if (!AnsiConsole.Confirm("[yellow]Would you like to run HyperSploit on another device?[/]", false)) break;
        }
        
        if (root != "") Directory.Delete(root);
    }

    /// <summary>
    /// Main bypass implementation
    /// </summary>
    /// <param name="client">ADB client</param>
    /// <param name="device">Device data</param>
    private static async Task Bypass(AdbClient client, DeviceData device) {
        AnsiConsole.MarkupLine("[green]Open Mi Unlock Status and attempt to bind account[/]");
        await client.ExecuteRemoteCommandAsync("svc wifi disable", device);
        await client.ExecuteRemoteCommandAsync("svc data enable", device);
        await client.ExecuteRemoteCommandAsync("am start -a android.settings.APPLICATION_DEVELOPMENT_SETTINGS", device);

        var receiver = new EventOutputReceiver();
        string? arguments = null;
        string? headers = null; 
        receiver.OnOutput += (_, line) => {
            var match = Regex.Match(line, "args: (.*)");
            if (match.Success) {
                AnsiConsole.MarkupLine("[green]Disabling mobile internet, taking over![/]");
                client.ExecuteRemoteCommand("svc data disable", device);
                arguments = Decrypt(match.Groups[1].Value);
                return;
            }
    
            match = Regex.Match(line, "headers: (.*)");
            if (match.Success) {
                headers = Decrypt(match.Groups[1].Value);
                receiver.Terminate();
            }
        };
        
        await client.ExecuteRemoteCommandAsync("logcat -T 1 *:S CloudDeviceStatus:V", device, receiver);
        if (arguments == null || headers == null) {
            await client.ExecuteRemoteCommandAsync("svc data enable", device);
            AnsiConsole.MarkupLine("[red]Failed to decrypt arguments and headers![/]");
            AnsiConsole.MarkupLine("[yellow]This probably means you have a patched Settings app.[/]");
            if (AnsiConsole.Confirm("[yellow]Try downgrading to an unpatched version?[/]", false))
                await Downgrade(client, device);
            return;
        }

        AnsiConsole.MarkupLine("[green]Successfully decrypted arguments and headers![/]");
        arguments = arguments.Replace("V816", "V14");
        
        const string signKey = "10f29ff413c89c8de02349cb3eb9a5f510f29ff413c89c8de02349cb3eb9a5f5";
        var toSign = $"POST\n/v1/unlock/applyBind\ndata={arguments}&sid=miui_sec_android";
        var signature = Convert.ToHexString(HMACSHA1.HashData(Encoding.ASCII.GetBytes(signKey), Encoding.UTF8.GetBytes(toSign)));
        
        AnsiConsole.MarkupLine("[cyan]Sending account bind request impersonating MIUI 14...[/]");
        var cookies = new CookieContainer();
        using var http = new HttpClient(new HttpClientHandler {
            CookieContainer = cookies
        });
        
        var match = Regex.Match(headers, "Cookie=\\[(.*)]").Groups[1].Value;
        foreach (var cookie in match.Split(";")) {
            var split = cookie.Split("=");
            cookies.Add(new Cookie(split[0], split[1], "/", "miui.com"));
        }

        var resp = await http.PostAsync($"{Host}/v1/unlock/applyBind",
            new FormUrlEncodedContent(new Dictionary<string, string> {
                ["data"] = arguments,
                ["sid"] = "miui_sec_android",
                ["sign"] = signature
            }));
        var json = JsonSerializer.Deserialize(await resp.Content.ReadAsStringAsync(),
            SourceGenerationContext.Default.JsonResponse)!;
        
        AnsiConsole.MarkupLine(json.Code switch {
            0 => "[green]Phone was successfully bound to Xiaomi account, you can now use Mi Unlock![/]",
            401 => "[red]Error 401: Xiaomi account credentials expired, login again[/]",
            10001 => "[red]Error 10001: Invalid bind request signature, make a GitHub issue[/]",
            20086 => "[red]Error 20086: Xiaomi account credentials expired, login again[/]",
            30001 => "[red]Error 30001: Device forced to verify, you're out of luck[/]",
            86015 => "[red]Error 86015: Invalid device signature[/]",
            _ => $"[red]Error {json.Code}: {json.Description}[/]"
        });
        
        await client.ExecuteRemoteCommandAsync("svc data enable", device);
    }

    /// <summary>
    /// Main downgrading implementation
    /// </summary>
    /// <param name="client">ADB client</param>
    /// <param name="device">Device data</param>
    private static async Task Downgrade(AdbClient client, DeviceData device) {
        AnsiConsole.MarkupLine("[green]Decompressing Settings.apk bundled within HyperSploit...[/]");
        await using var apk = typeof(Program).Assembly
            .GetManifestResourceStream("Settings.apk.gz")!;
        await using var stream = new GZipStream(
            apk, CompressionMode.Decompress);
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);
        memory.Position = 0;
        
        AnsiConsole.MarkupLine("[green]Installing unpatched Settings APK to the device...[/]");
        await client.InstallAsync(device, memory);

        AnsiConsole.MarkupLine("[green]Installation finished, trying the bypass again...[/]");
        AnsiConsole.MarkupLine("[yellow]Note: It's unknown whether it succeeded or not![/]");
        await Bypass(client, device);
    }
    
    /// <summary>
    /// Decrypt log line
    /// </summary>
    /// <param name="text">Text</param>
    /// <returns>Decrypted text</returns>
    private static string? Decrypt(string text) {
        if (text.StartsWith("#&^")) return null;
        using var aes = Aes.Create();
        aes.Key = "20nr1aobv2xi8ax4"u8.ToArray();
        aes.IV = "0102030405060708"u8.ToArray();
        using var decryptor = aes.CreateDecryptor();
        using var output = new MemoryStream();
        using var input = new MemoryStream(Convert.FromBase64String(text));
        using var stream = new CryptoStream(input, decryptor, CryptoStreamMode.Read);
        stream.CopyTo(output); return Encoding.UTF8.GetString(output.ToArray());
    }

    /// <summary>
    /// Extracts ADB binaries
    /// </summary>
    /// <returns>Path to folder</returns>
    private static string Extract() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "";
        var root = Path.Combine(Path.GetTempPath(), "hypersploit");
        if (Directory.Exists(root)) return root;
        AnsiConsole.MarkupLine("[yellow]Extracting Windows ADB binaries...[/]");
        var assembly = typeof(Program).Assembly; 
        var files = assembly.GetManifestResourceNames()
            .Where(x => x.StartsWith("Assets/adb"));
        foreach (var file in files) {
            var name = file.Replace("Assets/adb-windows/", "");
            var path = Path.Combine(root, name);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using (var src = assembly.GetManifestResourceStream(file)!)
            using (var dst = new FileStream(path, FileMode.Create, FileAccess.Write))
                src.CopyTo(dst);
            Process.Start("chmod", ["+x", path]);
        }

        return root;
    }
}