using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// Guards the convention behind <c>BacnetClient.BeginRemoteScope</c>: every method that talks to a
/// remote device - it logs "Sending ..." or "Broadcasting ..." and takes the address of the device -
/// opens the scope as its first statement, and so does the receive entry point. A source generator
/// cannot inject a statement into a method body, so the convention is checked against the source
/// here instead: forgetting the scope in a new method fails this test rather than silently logging
/// that device's traffic without its address.
/// </summary>
public class BacnetClientRemoteScopeConventionTests
{
    private const string ScopeStatement = "using var remoteScope = BeginRemoteScope(";

    // a member declaration of the class: 4-space indent, access modifier, a parameter list
    private static readonly Regex MemberSignature = new(@"^    (?:public|private|protected|internal) [^;=]*\(", RegexOptions.Compiled);
    private static readonly Regex MemberName = new(@"(\w+)\s*\(", RegexOptions.Compiled);

    // the address of the remote device - a "source" parameter is the address the frame is sent from
    private static readonly Regex RemoteAddressParameter = new(@"BacnetAddress\s+(?:receiver|adr|address|remoteAddress)\b", RegexOptions.Compiled);
    private static readonly Regex SendingLog = new(@"Log\.LogDebug\(\$?""(?:Sending|Broadcasting)", RegexOptions.Compiled);

    [Fact]
    public void Every_method_talking_to_a_remote_device_opens_its_scope_first()
    {
        var members = Members(File.ReadAllLines(ClientSourcePath())).ToList();

        var talking = members
            .Where(m => m.Name == "OnRecieve" || SendingLog.IsMatch(m.Body) && RemoteAddressParameter.IsMatch(m.Signature))
            .ToList();

        // the scan has to keep finding the methods, or a formatting change would silently disarm it
        Assert.Contains(talking, m => m.Name == "OnRecieve");
        Assert.Contains(talking, m => m.Name == "BeginReadPropertyRequest");
        Assert.True(talking.Count >= 30, $"only {talking.Count} methods found - has the formatting of BACnetClient.cs changed?");

        var missing = talking
            .Where(m => !m.FirstStatement.StartsWith(ScopeStatement, StringComparison.Ordinal))
            .Select(m => m.Name)
            .ToList();

        Assert.True(missing.Count == 0,
            $"Methods talking to a remote device without '{ScopeStatement}...)' as their first statement: {string.Join(", ", missing)}");
    }

    private static IEnumerable<Member> Members(string[] lines)
    {
        var starts = Enumerable.Range(0, lines.Length)
            .Where(i => MemberSignature.IsMatch(lines[i]) && !lines[i].TrimEnd().EndsWith(";"))
            .ToList();

        for (var k = 0; k < starts.Count; k++)
        {
            var start = starts[k];
            var end = k + 1 < starts.Count ? starts[k + 1] - 1 : lines.Length - 1;

            // the opening brace of the body sits alone on a line at the indentation of the member;
            // expression-bodied members have none and cannot talk to a device
            var brace = Enumerable.Range(start, end - start + 1).FirstOrDefault(i => lines[i].TrimEnd() == "    {", -1);
            if (brace < 0)
                continue;

            var signature = string.Join(" ", lines.Skip(start).Take(brace - start));
            var body = lines.Skip(brace + 1).Take(end - brace).ToList();

            yield return new Member(
                MemberName.Match(signature).Groups[1].Value,
                signature,
                string.Join("\n", body),
                body.Select(l => l.Trim()).FirstOrDefault(l => l.Length > 0) ?? "");
        }
    }

    /// <summary>
    /// The source file, found by walking up from the test output directory (Tests/bin/...). Not
    /// through [CallerFilePath]: a ContinuousIntegrationBuild maps source paths to /_/.
    /// </summary>
    private static string ClientSourcePath()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "BACnetClient.cs");
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException($"BACnetClient.cs not found above the test output directory {AppContext.BaseDirectory}");
    }

    private sealed record Member(string Name, string Signature, string Body, string FirstStatement);
}
