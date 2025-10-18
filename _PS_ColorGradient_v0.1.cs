/*
@name: _PS_ColorGradient
@version: 0.1

Copyright (c) 2025 AetherusFX

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System;

// 🔹 ParticleSystem 관련 호환성을 위해 추가
//    Unity 기본 환경(집)에서도 존재하는 네임스페이스만 사용
using UnityEngine.ParticleSystemJobs;

// ⚠️ 주의: 'UnityEngine.ParticleSystemModule' 네임스페이스는
//    Unity 표준 설치에서는 아예 없음 → 코드에 추가하면 집에서는 에러 발생.
//    따라서 여기서는 넣지 않고, 회사 환경에서 필요하다면 asmdef 참조 문제일 가능성이 큼.

public class _PS_ColorGradient : EditorWindow
{
    /*******************************************************************************************
        ★★★★★★★★★★ UI 변수 구조화 ★★★★★★★★★★         
    *******************************************************************************************/
    private const string JsonPath = @"D:/00_PresetBackup/@Unity/@Editor_Json/_PS_ColorGradientPresets.json";
    private const float PresetBarHeight = 10f;
    private const float NameFieldHeight = 20f;
    /*******************************************************************************************/

    private UnityEngine.Gradient editingGradient = new UnityEngine.Gradient();

    public enum PresetGroup { Color_Fixed, Color_Blend, Alpha }
    static readonly string[] GroupNames = { "Color_Fixed", "Color_Blend", "Alpha" };

    private enum ApplyTarget { StartColor, ColorOverLifetime }
    private ApplyTarget applyTarget = ApplyTarget.ColorOverLifetime; // 기본값

    [System.Serializable]
    public class PresetList
    {
        public List<GradientPreset> color_fixed = new();
        public List<GradientPreset> color_blend = new();
        public List<GradientPreset> alpha = new();
    }
    [System.Serializable]
    public class GradientPreset
    {
        public string name;
        public List<ColorKey> colorKeys;
        public List<AlphaKey> alphaKeys;
        public GradientMode mode; // GradientMode 저장 필드 추가

        public GradientPreset() { }
        public GradientPreset(string name, UnityEngine.Gradient grad)
        {
            this.name = name;
            colorKeys = new();
            alphaKeys = new();
            this.mode = grad.mode; // 현재 편집 중인 그라디언트의 모드 저장
            foreach (var ck in grad.colorKeys) colorKeys.Add(new ColorKey(ck));
            foreach (var ak in grad.alphaKeys) alphaKeys.Add(new AlphaKey(ak));
        }
        public UnityEngine.Gradient ToGradient()
        {
            UnityEngine.Gradient grad = new UnityEngine.Gradient();
            
            if (colorKeys == null || colorKeys.Count == 0)
            {
                colorKeys = new List<ColorKey> {
                    new ColorKey(new GradientColorKey(Color.white, 0)),
                    new ColorKey(new GradientColorKey(Color.white, 1))
                };
            }
            if (alphaKeys == null || alphaKeys.Count == 0)
            {
                alphaKeys = new List<AlphaKey> {
                    new AlphaKey(new GradientAlphaKey(1, 0)),
                    new AlphaKey(new GradientAlphaKey(1, 1))
                };
            }

            var cArr = colorKeys.ConvertAll(k => k.ToKey()).ToArray();
            var aArr = alphaKeys.ConvertAll(k => k.ToKey()).ToArray();
            for (int i = 0; i < cArr.Length; ++i) cArr[i].time = Mathf.Clamp01(cArr[i].time);
            for (int i = 0; i < aArr.Length; ++i) aArr[i].time = Mathf.Clamp01(aArr[i].time);

            grad.colorKeys = cArr;
            grad.alphaKeys = aArr;
            grad.mode = this.mode; // 저장된 모드 적용
            return grad;
        }
    }
    [System.Serializable]
    public class ColorKey
    {
        public float r, g, b, a, time;
        public ColorKey() { }
        public ColorKey(GradientColorKey ck)
        {
            r = ck.color.r; g = ck.color.g; b = ck.color.b; a = ck.color.a; time = ck.time;
        }
        public GradientColorKey ToKey() => new GradientColorKey(new Color(r, g, b, a), time);
    }
    [System.Serializable]
    public class AlphaKey
    {
        public float a, time;
        public AlphaKey() { }
        public AlphaKey(GradientAlphaKey ak) { a = ak.alpha; time = ak.time; }
        public GradientAlphaKey ToKey() => new GradientAlphaKey(a, time);
    }

    private PresetList presetList = new();

    [MenuItem("Tools/@FX_Tools/_PS_ColorGradient")]
    public static void ShowWindow()
    {
        GetWindow<_PS_ColorGradient>("_PS_ColorGradient");
    }

    private void OnEnable()
    {
        LoadPresets();
    }

    // ------[여기서부터 OnGUI만 "가로 자동 폭" 반영]-------
    private void OnGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Position:", GUILayout.Width(60));
        bool prevStart = (applyTarget == ApplyTarget.StartColor);
        bool prevColLt = (applyTarget == ApplyTarget.ColorOverLifetime);
        bool newStart = GUILayout.Toggle(prevStart, "Start Color", "Radio");
        bool newColLt = GUILayout.Toggle(prevColLt, "Color Over Lifetime", "Radio");
        if (newStart && !prevStart) applyTarget = ApplyTarget.StartColor;
        if (newColLt && !prevColLt) applyTarget = ApplyTarget.ColorOverLifetime;
        GUILayout.EndHorizontal();
        GUILayout.Space(8);

        GUILayout.Label("Color Over Lifetime", EditorStyles.boldLabel);
        editingGradient = EditorGUILayout.GradientField(editingGradient);

        GUILayout.Space(8);

        // === [가로폭 자동 계산] ===
        float padding = 30f;
        float spacing = 4f;
        int boxPerRow = 3;
        float totalBoxWidth = position.width - padding;
        float autoBoxWidth = (totalBoxWidth - (spacing * (boxPerRow - 1))) / boxPerRow;

        GUILayout.BeginHorizontal();
        for (int i = 0; i < boxPerRow; i++)
        {
            GUILayout.BeginVertical("box", GUILayout.Width(autoBoxWidth));
            DrawPresetColumn((PresetGroup)i, i, autoBoxWidth);
            GUILayout.EndVertical();

            if (i < boxPerRow - 1)
                GUILayout.Space(spacing);
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(8);
        if (GUILayout.Button("Json경로", GUILayout.Width(100)))
        {
            EditorUtility.RevealInFinder(JsonPath);
        }
    }
    // ------[여기까지 OnGUI]-------

    // DrawPresetColumn도 폭 인자 추가
    void DrawPresetColumn(PresetGroup group, int idx, float boxWidth)
    {
        if (GUILayout.Button($"{GroupNames[idx]}", GUILayout.Height(NameFieldHeight)))
        {
            string autoName = GroupNames[idx] + "_Preset_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            SaveNewPreset(group, autoName);
        }

        GUILayout.Space(3);

        List<GradientPreset> list = GetPresetList(group);
        for (int j = 0; j < list.Count; ++j)
        {
            DrawGradientPreview(group, list[j], j, boxWidth);
            GUILayout.Space(2);
        }
    }

    List<GradientPreset> GetPresetList(PresetGroup g)
    {
        return g switch
        {
            PresetGroup.Color_Fixed => presetList.color_fixed,
            PresetGroup.Color_Blend => presetList.color_blend,
            PresetGroup.Alpha      => presetList.alpha,
            _                      => presetList.color_fixed,
        };
    }

    void SaveNewPreset(PresetGroup group, string name)
    {
        if (group == PresetGroup.Color_Fixed)
        {
            editingGradient.mode = GradientMode.Fixed;
        }
        else if (group == PresetGroup.Color_Blend)
        {
            editingGradient.mode = GradientMode.Blend;
        }

        var newPreset = new GradientPreset(name, editingGradient);
        var list = GetPresetList(group);
        list.Add(newPreset);
        SavePresets();
    }

    void LoadPresets()
    {
        if (File.Exists(JsonPath))
        {
            var json = File.ReadAllText(JsonPath);
            presetList = JsonUtility.FromJson<PresetList>(json) ?? new PresetList();
        }
        else
        {
            presetList = new PresetList();
        }
    }

    void SavePresets()
    {
        string json = JsonUtility.ToJson(presetList, true);
        File.WriteAllText(JsonPath, json);
    }

    UnityEngine.Gradient ApplyAlphaOnly(UnityEngine.Gradient baseGradient, GradientPreset alphaPreset)
    {
        UnityEngine.Gradient result = new UnityEngine.Gradient();
        var colorKeys = baseGradient.colorKeys;
        var alphaKeys = alphaPreset.ToGradient().alphaKeys;
        result.SetKeys(colorKeys, alphaKeys);
        return result;
    }
    UnityEngine.Gradient ApplyColorOnly(UnityEngine.Gradient baseGradient, GradientPreset colorPreset)
    {
        UnityEngine.Gradient result = new UnityEngine.Gradient();
        var colorKeys = colorPreset.ToGradient().colorKeys;
        var alphaKeys = baseGradient.alphaKeys;
        result.SetKeys(colorKeys, alphaKeys);
        return result;
    }

    // 새로운 Apply 함수 추가
    void Apply(ParticleSystem ps, UnityEngine.Gradient grad, GradientMode mode)
    {
        if (applyTarget == ApplyTarget.ColorOverLifetime)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var minMaxGrad = new ParticleSystem.MinMaxGradient(grad);
            minMaxGrad.mode = (ParticleSystemGradientMode)mode;
            col.color = minMaxGrad;
        }
        else if (applyTarget == ApplyTarget.StartColor)
        {
            var main = ps.main;
            var minMaxGrad = new ParticleSystem.MinMaxGradient(grad);
            minMaxGrad.mode = (ParticleSystemGradientMode)mode;
            main.startColor = minMaxGrad;
        }
        EditorUtility.SetDirty(ps);
    }

    // DrawGradientPreview도 폭 인자 추가
    void DrawGradientPreview(PresetGroup group, GradientPreset preset, int index, float barWidth)
    {
        UnityEngine.Gradient grad = preset.ToGradient();
        if (grad.colorKeys == null || grad.colorKeys.Length == 0 || grad.alphaKeys == null || grad.alphaKeys.Length == 0)
            return;

        Rect rect = GUILayoutUtility.GetRect(barWidth, PresetBarHeight, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, new Color(0.17f, 0.17f, 0.17f));
        int texWidth = Mathf.Max(1, (int)rect.width);
        Texture2D tex = new Texture2D(texWidth, 1, TextureFormat.RGBA32, false);

        for (int x = 0; x < texWidth; x++)
        {
            float t = x / Mathf.Max(1f, (texWidth - 1));
            t = Mathf.Clamp01(t);
            Color gradColor = grad.Evaluate(t);
            int checkerSize = 6;
            bool isLight = ((x / checkerSize) % 2 == 0);
            Color checkerColor = isLight ? new Color(0.8f, 0.8f, 0.8f) : new Color(0.6f, 0.6f, 0.6f);
            Color finalColor = Color.Lerp(checkerColor, gradColor, gradColor.a);
            tex.SetPixel(x, 0, finalColor);
        }
        tex.Apply();
        GUI.DrawTexture(rect, tex);

        var nameRect = rect;
        nameRect.y += rect.height + 1;
        nameRect.height = 10;
        EditorGUI.LabelField(nameRect, "", EditorStyles.miniLabel);

        if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
        {
            if (Event.current.button == 1)
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Replace"), false, () =>
                {
                    if (group == PresetGroup.Color_Fixed) editingGradient.mode = GradientMode.Fixed;
                    else if (group == PresetGroup.Color_Blend) editingGradient.mode = GradientMode.Blend;
                    preset.colorKeys = new List<ColorKey>();
                    preset.alphaKeys = new List<AlphaKey>();
                    preset.mode = editingGradient.mode; // 모드도 함께 저장
                    foreach (var ck in editingGradient.colorKeys) preset.colorKeys.Add(new ColorKey(ck));
                    foreach (var ak in editingGradient.alphaKeys) preset.alphaKeys.Add(new AlphaKey(ak));
                    SavePresets();
                    Repaint();
                });
                menu.AddItem(new GUIContent("Delete"), false, () =>
                {
                    GetPresetList(group).RemoveAt(index);
                    SavePresets();
                    Repaint();
                });
                menu.AddItem(new GUIContent("Move to First"), false, () =>
                {
                    var list = GetPresetList(group);
                    var movePreset = list[index];
                    list.RemoveAt(index);
                    list.Insert(0, movePreset);
                    SavePresets();
                    Repaint();
                });
                menu.ShowAsContext();
                Event.current.Use();
            }
            else if (Event.current.button == 0)
            {
                editingGradient = grad;
                editingGradient.mode = preset.mode;
                Repaint();

                foreach (var obj in Selection.gameObjects)
                {
                    var ps = obj.GetComponent<ParticleSystem>();
                    if (ps != null)
                    {
                        // Fixed 그룹 적용 로직 (Start Color 모드 유지 기능 추가)
                        if (group == PresetGroup.Color_Fixed)
                        {
                            if (applyTarget == ApplyTarget.ColorOverLifetime) // Color Over Lifetime 처리 복구
                            {
                                // Color Over Lifetime은 모드 유지 요구사항이 없으므로 Apply로 덮어씀
                                Apply(ps, grad, preset.mode);
                            }
                            else if (applyTarget == ApplyTarget.StartColor) // Start Color 처리
                            {
                                var main = ps.main;
                                ParticleSystemGradientMode mode = main.startColor.mode;
                                
                                if (mode == ParticleSystemGradientMode.Color)
                                {
                                    // 단일 색상 모드: 프리셋의 첫 번째 색상으로 단일 색상을 설정
                                    Color newColor = grad.colorKeys.Length > 0 ? grad.colorKeys[0].color : Color.white;
                                    main.startColor = new ParticleSystem.MinMaxGradient(newColor);
                                }
                                // TwoColors와 RandomColor는 Min/Max Color를 업데이트함.
                                else if (mode == ParticleSystemGradientMode.TwoColors || mode == ParticleSystemGradientMode.RandomColor)
                                {
                                    // MinMaxGradient(TwoColors 또는 RandomColor) 모드 유지하며 색상만 변경
                                    // 프리셋의 첫 색상(min)과 마지막 색상(max)을 Min/Max Color에 적용
                                    Color minCol = grad.colorKeys.Length > 0 ? grad.colorKeys[0].color : Color.white;
                                    Color maxCol = grad.colorKeys.Length > 1 ? grad.colorKeys[grad.colorKeys.Length - 1].color : minCol;
                                    main.startColor = new ParticleSystem.MinMaxGradient(minCol, maxCol);
                                }
                                else if (mode == ParticleSystemGradientMode.Gradient || mode == ParticleSystemGradientMode.TwoGradients)
                                {
                                    // 그라디언트 모드: Apply 함수를 호출하여 덮어쓰기 (모드를 Gradient로 설정)
                                    Apply(ps, grad, preset.mode); 
                                }
                                // 다른 모드(Random between two curves, etc.)는 무시.
                            }
                            EditorUtility.SetDirty(ps);
                        }
                        else
                        {
                            // Blend 및 Alpha 그룹일 때는 기존 로직 유지
                            if (applyTarget == ApplyTarget.ColorOverLifetime)
                            {
                                var col = ps.colorOverLifetime;
                                col.enabled = true;

                                ParticleSystemGradientMode mode = col.color.mode;
                                UnityEngine.Gradient baseGradMin = col.color.gradientMin;
                                UnityEngine.Gradient baseGradMax = col.color.gradientMax;
                                UnityEngine.Gradient baseGrad = col.color.gradient;

                                // === [Two Gradient/TWO COLOR 지원] ===
                                if (mode == ParticleSystemGradientMode.TwoGradients)
                                {
                                    UnityEngine.Gradient gradMin = (baseGradMin != null) ? baseGradMin : grad;
                                    UnityEngine.Gradient gradMax = (baseGradMax != null) ? baseGradMax : grad;

                                    if (group == PresetGroup.Alpha)
                                    {
                                        gradMin = ApplyAlphaOnly(gradMin, preset);
                                        gradMax = ApplyAlphaOnly(gradMax, preset);
                                    }
                                    else
                                    {
                                        gradMin = ApplyColorOnly(gradMin, preset);
                                        gradMax = ApplyColorOnly(gradMax, preset);
                                    }
                                    col.color = new ParticleSystem.MinMaxGradient(gradMin, gradMax);
                                }
                                else if (mode == ParticleSystemGradientMode.TwoColors)
                                {
                                    Color minCol = (baseGradMin != null && baseGradMin.colorKeys.Length > 0) ? baseGradMin.colorKeys[0].color : Color.white;
                                    Color maxCol = (baseGradMax != null && baseGradMax.colorKeys.Length > 0) ? baseGradMax.colorKeys[0].color : Color.white;
                                    if (group == PresetGroup.Alpha)
                                    {
                                        float minA = grad.alphaKeys.Length > 0 ? grad.alphaKeys[0].alpha : 1f;
                                        float maxA = grad.alphaKeys.Length > 0 ? grad.alphaKeys[grad.alphaKeys.Length - 1].alpha : 1f;
                                        minCol.a = minA;
                                        maxCol.a = maxA;
                                    }
                                    else
                                    {
                                        minCol = grad.colorKeys.Length > 0 ? grad.colorKeys[0].color : minCol;
                                        maxCol = grad.colorKeys.Length > 1 ? grad.colorKeys[1].color : minCol;
                                    }
                                    col.color = new ParticleSystem.MinMaxGradient(minCol, maxCol);
                                }
                                else
                                {
                                    // 기존(One Gradient/Color)
                                    if (group == PresetGroup.Alpha)
                                    {
                                        UnityEngine.Gradient merged = ApplyAlphaOnly((baseGrad != null) ? baseGrad : grad, preset);
                                        col.color = new ParticleSystem.MinMaxGradient(merged);
                                    }
                                    else
                                    {
                                        UnityEngine.Gradient merged = ApplyColorOnly((baseGrad != null) ? baseGrad : grad, preset);
                                        col.color = new ParticleSystem.MinMaxGradient(merged);
                                    }
                                }
                                EditorUtility.SetDirty(ps);
                            }
                            else if (applyTarget == ApplyTarget.StartColor)
                            {
                                var main = ps.main;
                                ParticleSystemGradientMode mode = main.startColor.mode;
                                UnityEngine.Gradient baseGradMin = main.startColor.gradientMin;
                                UnityEngine.Gradient baseGradMax = main.startColor.gradientMax;
                                UnityEngine.Gradient baseGrad = main.startColor.gradient;

                                if (mode == ParticleSystemGradientMode.TwoGradients)
                                {
                                    UnityEngine.Gradient gradMin = (baseGradMin != null) ? baseGradMin : grad;
                                    UnityEngine.Gradient gradMax = (baseGradMax != null) ? baseGradMax : grad;

                                    if (group == PresetGroup.Alpha)
                                    {
                                        gradMin = ApplyAlphaOnly(gradMin, preset);
                                        gradMax = ApplyAlphaOnly(gradMax, preset);
                                    }
                                    else
                                    {
                                        gradMin = ApplyColorOnly(gradMin, preset);
                                        gradMax = ApplyColorOnly(gradMax, preset);
                                    }
                                    main.startColor = new ParticleSystem.MinMaxGradient(gradMin, gradMax);
                                }
                                else if (mode == ParticleSystemGradientMode.TwoColors)
                                {
                                    Color minCol = (baseGradMin != null && baseGradMin.colorKeys.Length > 0) ? baseGradMin.colorKeys[0].color : Color.white;
                                    Color maxCol = (baseGradMax != null && baseGradMax.colorKeys.Length > 0) ? baseGradMax.colorKeys[0].color : Color.white;
                                    if (group == PresetGroup.Alpha)
                                    {
                                        float minA = grad.alphaKeys.Length > 0 ? grad.alphaKeys[0].alpha : 1f;
                                        float maxA = grad.alphaKeys.Length > 0 ? grad.alphaKeys[grad.alphaKeys.Length - 1].alpha : 1f;
                                        minCol.a = minA;
                                        maxCol.a = maxA;
                                    }
                                    else
                                    {
                                        minCol = grad.colorKeys.Length > 0 ? grad.colorKeys[0].color : minCol;
                                        maxCol = grad.colorKeys.Length > 1 ? grad.colorKeys[1].color : minCol;
                                    }
                                    main.startColor = new ParticleSystem.MinMaxGradient(minCol, maxCol);
                                }
                                else
                                {
                                    if (group == PresetGroup.Alpha)
                                    {
                                        UnityEngine.Gradient merged = ApplyAlphaOnly((baseGrad != null) ? baseGrad : grad, preset);
                                        main.startColor = new ParticleSystem.MinMaxGradient(merged);
                                    }
                                    else
                                    {
                                        UnityEngine.Gradient merged = ApplyColorOnly((baseGrad != null) ? baseGrad : grad, preset);
                                        main.startColor = new ParticleSystem.MinMaxGradient(merged);
                                    }
                                }
                                EditorUtility.SetDirty(ps);
                            }
                        }
                    }
                }
            }
        }
    }
}
#endif