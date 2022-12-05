using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.log;
using FPCSharpUnity.unity.Editor.Utils;
using FPCSharpUnity.unity.Logger;
using UnityEditor;
using UnityEngine;
using static FPCSharpUnity.core.typeclasses.Str;

namespace FPCSharpUnity.unity.Editor.AssetTools; 

/// <summary>
/// Unity supports file-scoped namespaces for <see cref="MonoBehaviour"/> and <see cref="ScriptableObject"/> in
/// Unity 2020, but not in later versions.
/// <para/>
/// This script goes through your project and converts the `.cs` files to use the block-scoped namespace, instead of
/// file-scoped one in files which are not loaded by newer Unity versions otherwise.
/// </summary>
[HasLogger]
public static partial class ConvertUnityScriptFilesToBlockScopedNamespace {
  [MenuItem("Tools/FP C# Unity/Convert Unity Script Files To Block-Scoped Namespace")]
  public static void doConvert() {
    var regexContainsUnityScript =
      new Regex("class\\s+.+:\\s*(MonoBehaviour|ScriptableObject)", RegexOptions.Singleline);
    var regexContainsFileScopedNamespace = new Regex(@"^\s*namespace .+;\s*$", RegexOptions.Multiline);
    var regexNewline = new Regex(@"(\r?\n)");
    
    using var editorProgress = new EditorProgress("Converting Unity script files to block-scoped namespace");
    var csFiles = editorProgress.execute(
      "Collect .cs files",
      () => AssetDatabase.GetAllAssetPaths().Where(path => {
        var pathLower = path.ToLowerInvariant();
        return pathLower.StartsWithFast("assets/") && pathLower.EndsWithFast(".cs");
      }).ToArray()
    );

    editorProgress.progressCancellableParallel(
      "Processing .cs files", csFiles,
      csFilePath => {
        var contents = File.ReadAllText(csFilePath, Encoding.UTF8);
        if (!regexContainsUnityScript.IsMatch(contents)) return;
        var namespaceMatch = regexContainsFileScopedNamespace.Match(contents);
        if (!namespaceMatch.Success) return;

        var group = namespaceMatch.Groups[0];
        var semicolonAt = group.Index + group.Value.LastIndexOf(';');

        var untilBlock = contents.Substring(0, semicolonAt);
        var inNamespace = contents.Substring(semicolonAt + 1);
        var indentedInNamespace = regexNewline.Replace(inNamespace, "$1  ");
        var newContents = $"{untilBlock} {{{indentedInNamespace}{Environment.NewLine}}}";
        
        File.WriteAllText(csFilePath, newContents, Encoding.UTF8);
        log.mInfo($"Processed {s(csFilePath)}");
      }
    );
  }
}