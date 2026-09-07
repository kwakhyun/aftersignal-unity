param([switch]$Check)
$ErrorActionPreference='Stop'
if($PSVersionTable.PSVersion.Major -lt 7){throw 'Run with PowerShell 7 (pwsh).'}
$taskRoot=Split-Path $PSScriptRoot -Parent
Add-Type -Path (Join-Path $PSHOME 'Microsoft.CodeAnalysis.dll')
Add-Type -Path (Join-Path $PSHOME 'Microsoft.CodeAnalysis.CSharp.dll')
Add-Type -CompilerOptions '/nowarn:1701' -ReferencedAssemblies @((Join-Path $PSHOME 'Microsoft.CodeAnalysis.dll'),(Join-Path $PSHOME 'Microsoft.CodeAnalysis.CSharp.dll'),(Join-Path $PSHOME 'System.Collections.Immutable.dll')) -TypeDefinition @'
using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
public static class AfterSignalSourceFormat
{
    public static string Apply(string source)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();
        foreach (var diagnostic in root.GetDiagnostics())
            if (diagnostic.Severity == DiagnosticSeverity.Error)
                throw new InvalidOperationException(diagnostic.ToString());
        string formatted = root.NormalizeWhitespace("    ", "\n", false).ToFullString().TrimEnd() + "\n";
        var before = root.DescendantTokens(descendIntoTrivia: true).GetEnumerator();
        var after = CSharpSyntaxTree.ParseText(formatted).GetRoot().DescendantTokens(descendIntoTrivia: true).GetEnumerator();
        while (before.MoveNext())
            if (!after.MoveNext() || before.Current.RawKind != after.Current.RawKind || before.Current.Text != after.Current.Text)
                throw new InvalidOperationException("Formatting changed a C# token.");
        if (after.MoveNext())
            throw new InvalidOperationException("Formatting added a C# token.");
        return formatted;
    }
}
'@
# Stable entry points and audio files stay untouched during the separate BGM task.
$protected=@('SignalAudio.cs','QualityAudioCapture.cs','GameDirector.cs','GameTuning.cs','SignalHud.cs','PresentationSettings.cs','ProjectBuilder.cs','ArtImporter.cs','QualityPass.cs')
$changed=0
foreach($file in Get-ChildItem -LiteralPath (Join-Path $taskRoot 'Assets/AfterSignal') -Filter '*.cs' -Recurse -File){
    if($file.Name -in $protected -or $file.FullName -match '(?i)(audio|music|bgm)'){continue}
    $original=[IO.File]::ReadAllText($file.FullName)
    try{$formatted=[AfterSignalSourceFormat]::Apply($original)}catch{throw ($file.FullName+': '+$_.Exception.Message)}
    if($original -ceq $formatted){continue}
    if([IO.File]::ReadAllText($file.FullName) -cne $original){throw ('File changed concurrently: '+$file.FullName)}
    $changed++
    if(-not $Check){[IO.File]::WriteAllText($file.FullName,$formatted,[Text.UTF8Encoding]::new($false))}
}
Write-Output ('C# files '+$(if($Check){'requiring formatting'}else{'formatted'})+': '+$changed)
if($Check -and $changed -gt 0){exit 1}
