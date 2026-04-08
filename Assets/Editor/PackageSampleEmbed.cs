using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

[InitializeOnLoad]
public static class GPUUniformGridPackageSampleEmbed {
    static bool s_Copying;

    const string SamplesSourcePath = "Assets/Samples";
    const string SamplesDestPath = "Packages/jp.nobnak.gpu_uniform_grid/Samples~";
    const string ReadmeSourcePath = "README.md";
    const string ReadmeDestPath = "Packages/jp.nobnak.gpu_uniform_grid/README.md";

    static GPUUniformGridPackageSampleEmbed() {
        CompilationPipeline.compilationStarted += OnCompilationStarted;
        CompilationPipeline.compilationFinished += OnCompilationFinished;
        EditorApplication.delayCall += OnEditorDelayCall;
        EditorApplication.projectChanged += OnProjectChanged;
    }

    static void OnCompilationStarted(object _) => CopySamplesToPackage();
    static void OnCompilationFinished(object _) => CopySamplesToPackage();
    static void OnEditorDelayCall() => CopySamplesToPackage();
    static void OnProjectChanged() => CopySamplesToPackage();

    [MenuItem("Tools/GPU Uniform Grid/Force Copy Samples & README to Package")]
    static void ForceCopySamples() => CopySamplesToPackage();

    static void CopySamplesToPackage() {
        if (s_Copying)
            return;
        s_Copying = true;
        try {
            if (Directory.Exists(SamplesSourcePath)) {
                if (Directory.Exists(SamplesDestPath))
                    Directory.Delete(SamplesDestPath, true);
                Directory.CreateDirectory(SamplesDestPath);
                CopyDirectory(SamplesSourcePath, SamplesDestPath);
            } else {
                Debug.LogWarning($"[GPUUniformGridPackageSampleEmbed] Samples folder not found: {SamplesSourcePath}");
            }
            CopyReadmeToPackage();
            AssetDatabase.Refresh();
        } catch (System.Exception e) {
            Debug.LogError($"[GPUUniformGridPackageSampleEmbed] Copy failed: {e.Message}");
        } finally {
            s_Copying = false;
        }
    }

    static void CopyReadmeToPackage() {
        if (!File.Exists(ReadmeSourcePath)) {
            Debug.LogWarning($"[GPUUniformGridPackageSampleEmbed] README not found: {ReadmeSourcePath}");
            return;
        }
        string destDir = Path.GetDirectoryName(ReadmeDestPath);
        if (!string.IsNullOrEmpty(destDir))
            Directory.CreateDirectory(destDir);
        File.Copy(ReadmeSourcePath, ReadmeDestPath, true);
    }

    static void CopyDirectory(string sourcePath, string destPath) {
        Directory.CreateDirectory(destPath);
        foreach (string file in Directory.GetFiles(sourcePath)) {
            string name = Path.GetFileName(file);
            File.Copy(file, Path.Combine(destPath, name), true);
        }
        foreach (string directory in Directory.GetDirectories(sourcePath)) {
            string dirName = Path.GetFileName(directory);
            CopyDirectory(directory, Path.Combine(destPath, dirName));
        }
    }
}
