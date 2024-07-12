// #define SHOW_EXPORT_BUTTON

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif
using UnityEngine.Rendering;

namespace CartoonFX
{
    namespace CustomShaderImporter
    {
        static class Utils
        {
            public static bool IsUsingURP()
            {
#if UNITY_2019_3_OR_NEWER
                var renderPipeline = GraphicsSettings.currentRenderPipeline;
#else
                var renderPipeline = GraphicsSettings.renderPipelineAsset;
#endif
                return renderPipeline != null && renderPipeline.GetType().Name.Contains("Universal");
            }
        }

        [ScriptedImporter(0, FILE_EXTENSION)]
        public class CFXR_ShaderImporter : ScriptedImporter
        {
            public enum RenderPipeline
            {
                Auto,
                ForceBuiltInRenderPipeline,
                ForceUniversalRenderPipeline
            }

            public const string FILE_EXTENSION = "cfxrshader";

            [Tooltip("In case of errors when building the project or with addressables, you can try forcing a specific render pipeline")]
            public RenderPipeline renderPipelineDetection = RenderPipeline.Auto;
            public string detectedRenderPipeline = "Built-In Render Pipeline";
            public int strippedLinesCount = 0;
            public string shaderSourceCode;
            public string shaderName;
            public string[] shaderErrors;
            public ulong variantCount;
            public ulong variantCountUsed;

            enum ComparisonOperator
            {
                Equal,
                Greater,
                GreaterOrEqual,
                Less,
                LessOrEqual
            }

#if UNITY_2022_2_OR_NEWER
            const int URP_VERSION = 14;
#elif UNITY_2021_2_OR_NEWER
            const int URP_VERSION = 12;
#elif UNITY_2021_1_OR_NEWER
            const int URP_VERSION = 11;
#elif UNITY_2020_3_OR_NEWER
            const int URP_VERSION = 10;
#else
            const int URP_VERSION = 7;
#endif

            static ComparisonOperator ParseComparisonOperator(string symbols)
            {
                switch (symbols)
                {
                    case "==": return ComparisonOperator.Equal;
                    case "<=": return ComparisonOperator.LessOrEqual;
                    case "<": return ComparisonOperator.Less;
                    case ">": return ComparisonOperator.Greater;
                    case ">=": return ComparisonOperator.GreaterOrEqual;
                    default: throw new Exception("Invalid comparison operator: " + symbols);
                }
            }

            static bool CompareWithOperator(int value1, int value2, ComparisonOperator comparisonOperator)
            {
                switch (comparisonOperator)
                {
                    case ComparisonOperator.Equal: return value1 == value2;
                    case ComparisonOperator.Greater: return value1 > value2;
                    case ComparisonOperator.GreaterOrEqual: return value1 >= value2;
                    case ComparisonOperator.Less: return value1 < value2;
                    case ComparisonOperator.LessOrEqual: return value1 <= value2;
                    default: throw new Exception("Invalid comparison operator value: " + comparisonOperator);
                }
            }

            bool StartsOrEndWithSpecialTag(string line)
            {
                bool startsWithTag = (line.Length > 4 && line[0] == '/' && line[1] == '*' && line[2] == '*' && line[3] == '*');
                if (startsWithTag) return true;

                int l = line.Length-1;
                bool endsWithTag = (line.Length > 4 && line[l] == '/' && line[l-1] == '*' && line[l-2] == '*' && line[l-3] == '*');
                return endsWithTag;
            }

            public override void OnImportAsset(AssetImportContext context)
            {
                bool isUsingURP;
                switch (renderPipelineDetection)
                {
                    default:
                    case RenderPipeline.Auto:
                    {
                        isUsingURP = Utils.IsUsingURP();
                        detectedRenderPipeline = isUsingURP ? "Universal Render Pipeline" : "Built-In Render Pipeline";
                        break;
                    }
                    case RenderPipeline.ForceBuiltInRenderPipeline:
                    {
                        detectedRenderPipeline = "Built-In Render Pipeline";
                        isUsingURP = false;
                        break;
                    }
                    case RenderPipeline.ForceUniversalRenderPipeline:
                    {
                        detectedRenderPipeline = "Universal Render Pipeline";
                        isUsingURP = true;
                        break;
                    }
                }

                StringWriter shaderSource = new StringWriter();
                string[] sourceLines = File.ReadAllLines(context.assetPath);
                Stack<bool> excludeCurrentLines = new Stack<bool>();
                strippedLinesCount = 0;

                for (int i = 0; i < sourceLines.Length; i++)
                {
                    bool excludeThisLine = excludeCurrentLines.Count > 0 && excludeCurrentLines.Peek();

                    string line = sourceLines[i];
                    if (StartsOrEndWithSpecialTag(line))
                    {
                        if (line.StartsWith("/*** BIRP ***/"))
                        {
                            excludeCurrentLines.Push(excludeThisLine || isUsingURP);
                        }
                        else if (line.StartsWith("/*** URP ***/"))
                        {
                            excludeCurrentLines.Push(excludeThisLine || !isUsingURP);
                        }
                        else if (line.StartsWith("/*** URP_VERSION "))
                        {
                            string subline = line.Substring("/*** URP_VERSION ".Length);
                            int spaceIndex = subline.IndexOf(' ');
                            string version = subline.Substring(spaceIndex, subline.LastIndexOf(' ') - spaceIndex);
                            string op = subline.Substring(0, spaceIndex);

                            var compOp = ParseComparisonOperator(op);
                            int compVersion = int.Parse(version);

                            bool isCorrectURP = CompareWithOperator(URP_VERSION, compVersion, compOp);
                            excludeCurrentLines.Push(excludeThisLine || !isCorrectURP);
                        }
                        else if (excludeThisLine && line.StartsWith("/*** END"))
                        {
                            excludeCurrentLines.Pop();
                        }
                        else if (!excludeThisLine && line.StartsWith("/*** #define URP_VERSION ***/"))
                        {
                            shaderSource.WriteLine("\t\t\t#define URP_VERSION " + URP_VERSION);
                        }
                    }
                    else
                    {
                        if (excludeThisLine)
                        {
                            strippedLinesCount++;
                            continue;
                        }

                        shaderSource.WriteLine(line);
                    }
                }

                // Get source code and extract name
                shaderSourceCode = shaderSource.ToString();
                int idx = shaderSourceCode.IndexOf("Shader \"", StringComparison.InvariantCulture) + 8;
                int idx2 = shaderSourceCode.IndexOf('"', idx);
                shaderName = shaderSourceCode.Substring(idx, idx2 - idx);
                shaderErrors = null;

                Shader shader = ShaderUtil.CreateShaderAsset(context, shaderSourceCode, true);

                if (ShaderUtil.ShaderHasError(shader))
                {
                    string[] shaderSourceLines = shaderSourceCode.Split(new [] {'\n'}, StringSplitOptions.None);
                    var errors = ShaderUtil.GetShaderMessages(shader);
                    shaderErrors = Array.ConvertAll(errors, err => $"{err.message} (line {err.line})");
                    foreach (ShaderMessage error in errors)
                    {
                        string message = error.line <= 0 ?
                            string.Format("Shader Error in '{0}' (in file '{2}')\nError: {1}\n", shaderName, error.message, error.file) :
                            string.Format("Shader Error in '{0}' (line {2} in file '{3}')\nError: {1}\nLine: {4}\n", shaderName, error.message, error.line, error.file, shaderSourceLines[error.line-1]);
                        if (error.severity == ShaderCompilerMessageSeverity.Warning)
                        {
                            Debug.LogWarning(message);
                        }
                        else
                        {
                            Debug.LogError(message);
                        }
                    }
                }
                else
                {
                    ShaderUtil.ClearShaderMessages(shader);
                }

                context.AddObjectToAsset("MainAsset", shader);
                context.SetMainObject(shader);

                // Try to count variant using reflection:
                // internal static extern ulong GetVariantCount(Shader s, bool usedBySceneOnly);
                variantCount = 0;
                variantCountUsed = 0;
                MethodInfo getVariantCountReflection = typeof(ShaderUtil).GetMethod("GetVariantCount", BindingFlags.Static | BindingFlags.NonPublic);
                if (getVariantCountReflection != null)
                {
                    try
                    {
                        object result = getVariantCountReflection.Invoke(null, new object[] {shader, false});
                        variantCount = (ulong)result;
                        result = getVariantCountReflection.Invoke(null, new object[] {shader, true});
                        variantCountUsed = (ulong)result;
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
        }

        namespace Inspector
        {
            [CustomEditor(typeof(CFXR_ShaderImporter)), CanEditMultipleObjects]
            public class TCP2ShaderImporter_Editor : Editor
            {
                CFXR_ShaderImporter Importer => (CFXR_ShaderImporter) this.target;

                // From: UnityEditor.ShaderInspectorPlatformsPopup
                static string FormatCount(ulong count)
                {
                    bool flag = count > 1000000000uL;
                    string result;
                    if (flag)
                    {
                        result = (count / 1000000000.0).ToString("f2", CultureInfo.InvariantCulture.NumberFormat) + "B";
                    }
                    else
                    {
                        bool flag2 = count > 1000000uL;
                        if (flag2)
                        {
                            result = (count / 1000000.0).ToString("f2", CultureInfo.InvariantCulture.NumberFormat) + "M";
                        }
                        else
                        {
                            bool flag3 = count > 1000uL;
                            if (flag3)
                            {
                                result = (count / 1000.0).ToString("f2", CultureInfo.InvariantCulture.NumberFormat) + "k";
                            }
                            else
                            {
                                result = count.ToString();
                            }
                        }
                    }
                    return result;
                }

                static GUIStyle _HelpBoxRichTextStyle;
                static GUIStyle HelpBoxRichTextStyle
                {
                    get
                    {
                        if (_HelpBoxRichTextStyle == null)
                        {
                            _HelpBoxRichTextStyle = new GUIStyle("HelpBox");
                            _HelpBoxRichTextStyle.richText = true;
                            _HelpBoxRichTextStyle.margin = new RectOffset(4, 4, 0, 0);
                            _HelpBoxRichTextStyle.padding = new RectOffset(4, 4, 4, 4);
                        }
                        return _HelpBoxRichTextStyle;
                    }
                }

                public override void OnInspectorGUI()
                {
                    bool multipleValues = serializedObject.isEditingMultipleObjects;

                    CFXR_ShaderImporter.RenderPipeline detection = ((CFXR_ShaderImporter)target).renderPipelineDetection;
                    bool isUsingURP = Utils.IsUsingURP();
                    serializedObject.Update();

                    GUILayout.Label(Importer.shaderName);
                    string variantsText = "";
                    if (Importer.variantCount > 0 && Importer.variantCountUsed > 0)
                    {
                        string variantsCount = multipleValues ? "-" : FormatCount(Importer.variantCount);
                        string variantsCountUsed = multipleValues ? "-" : FormatCount(Importer.variantCountUsed);
                        variantsText = $"\nVariants (currently used): <b>{variantsCountUsed}</b>\nVariants (including unused): <b>{variantsCount}</b>";
                    }
                    string strippedLinesCount = multipleValues ? "-" : Importer.strippedLinesCount.ToString();
                    string renderPipeline = Importer.detectedRenderPipeline;
                    if (targets is { Length: > 1 })
                    {
                        foreach (CFXR_ShaderImporter importer in targets)
                        {
                            if (importer.detectedRenderPipeline != renderPipeline)
                            {
                                renderPipeline = "-";
                                break;
                            }
                        }
                    }
                    GUILayout.Label($"{(detection == CFXR_ShaderImporter.RenderPipeline.Auto ? "Detected" : "Forced")} render pipeline: <b>{renderPipeline}</b>\nStripped lines: <b>{strippedLinesCount}</b>{variantsText}", HelpBoxRichTextStyle);

                    if (Importer.shaderErrors != null && Importer.shaderErrors.Length > 0)
                    {
                        GUILayout.Space(4);
                        var color = GUI.color;
                        GUI.color = new Color32(0xFF, 0x80, 0x80, 0xFF);
                        GUILayout.Label($"<b>Errors:</b>\n{string.Join("\n", Importer.shaderErrors)}", HelpBoxRichTextStyle);
                        GUI.color = color;
                    }

                    bool shouldReimportShader = false;
                    bool compiledForURP = Importer.detectedRenderPipeline.Contains("Universal");
                    if (detection == CFXR_ShaderImporter.RenderPipeline.Auto
                        && ((isUsingURP && !compiledForURP) || (!isUsingURP && compiledForURP)))
                    {
                        GUILayout.Space(4);
                        Color guiColor = GUI.color;
                        GUI.color *= Color.yellow;
                        EditorGUILayout.HelpBox("The detected render pipeline doesn't match the pipeline this shader was compiled for!\nPlease reimport the shaders for them to work in the current render pipeline.", MessageType.Warning);
                        if (GUILayout.Button("Reimport Shader"))
                        {
                            shouldReimportShader = true;
                        }
                        GUI.color = guiColor;
                    }

                    GUILayout.Space(4);


                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CFXR_ShaderImporter.renderPipelineDetection)));
                    if (EditorGUI.EndChangeCheck())
                    {
                        shouldReimportShader = true;
                    }

                    if (GUILayout.Button("View Source", GUILayout.ExpandWidth(false)))
                    {
                        string path = Application.temporaryCachePath + "/" + Importer.shaderName.Replace("/", "-") + "_Source.shader";
                        if (File.Exists(path))
                        {
                            File.SetAttributes(path, FileAttributes.Normal);
                        }

                        File.WriteAllText(path, Importer.shaderSourceCode);
                        File.SetAttributes(path, FileAttributes.ReadOnly);
                        UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(path, 0);
                    }

#if SHOW_EXPORT_BUTTON
                    GUILayout.Space(8);

                    EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(importer.shaderSourceCode));
                    {
                        if (GUILayout.Button("Export .shader file", GUILayout.ExpandWidth(false)))
                        {
                            string savePath = EditorUtility.SaveFilePanel("Export CFXR shader", Application.dataPath, "CFXR Shader","shader");
                            if (!string.IsNullOrEmpty(savePath))
                            {
                                File.WriteAllText(savePath, importer.shaderSourceCode);
                            }
                        }
                    }
                    EditorGUI.EndDisabledGroup();
#endif

                    serializedObject.ApplyModifiedProperties();

                    if (shouldReimportShader)
                    {
                        ReimportShader();
                    }
                }

                void ReimportShader()
                {
                    foreach (UnityEngine.Object t in targets)
                    {
                        string path = AssetDatabase.GetAssetPath(t);
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                    }
                }
            }
        }
    }
}