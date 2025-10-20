/*
@name: _PS_Change
@version: 1.0

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

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.ParticleSystemJobs;


public class _PS_Change : EditorWindow
{
    // Window ▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄
    [MenuItem("Tools/@FX_Tools/_PS_Change")]
    public static void ShowWindow()
    {
        var wnd = GetWindow<_PS_Change>();
        wnd.titleContent = new GUIContent("_PS_Change");
        wnd.minSize = new Vector2(480, 560);
        wnd.Show();
    }

    // UI_전체▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄
    public void OnGUI()
    {
        EditorGUILayout.Space();

        DrawUI_HueShift();               // Hue/S/V/A 조절
        EditorGUILayout.Space();

        DrawUI_PropertyDelta();          // (예) 선택 파티클 속성 증감 Δ
    }

    // UI_구조▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂
    private const float PropertyLabelWidth = 100f;
    private const float PropertyButtonWidth = 40f;
    private const float PropertyFloatWidth = 60f;

    // DrawUIHueShift▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂
    void DrawUI_HueShift()
    {
        GUILayout.Label("Hue Shift", EditorStyles.boldLabel);
		EditorGUILayout.BeginVertical(GUI.skin.box);

        // Hue
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Hue", GUILayout.Width(PropertyLabelWidth));
        float prevHue = hueShift;
        hueShift = EditorGUILayout.Slider(hueShift, -1f, 1f, GUILayout.Width(180));
        if (GUILayout.Button("✔️", GUILayout.Width(PropertyButtonWidth)))
        {
            SnapshotOriginalColors();
            hueShift = 0f;
            GUI.FocusControl(null);
        }
        EditorGUILayout.EndHorizontal();

        if (Mathf.Abs(hueShift - prevHue) > 0.0001f)
            ApplyHueShift(selectionParticleSystems, hueShift);

        // Hue bar
        if (hueBarTexture == null || hueBarTexture.width != hueBarWidth)
            CreateHueBarTexture();
        Rect rect = GUILayoutUtility.GetRect(hueBarWidth, hueBarHeight, GUILayout.ExpandWidth(false));
        EditorGUI.DrawPreviewTexture(rect, hueBarTexture);

        // S
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("S (채도)", GUILayout.Width(PropertyLabelWidth));
        float prevS = sDelta;
        sDelta = EditorGUILayout.Slider(sDelta, -1f, 1f, GUILayout.Width(180));
        if (GUILayout.Button("✔️", GUILayout.Width(PropertyButtonWidth)))
        {
            SnapshotOriginalColors();
            sDelta = 0f;
            GUI.FocusControl(null);
        }
        EditorGUILayout.EndHorizontal();

        if (Mathf.Abs(sDelta - prevS) > 0.0001f)
            ApplySVADelta(selectionParticleSystems, sDelta, vDelta, aDelta); // ⚠️ S 변경 시 V, A 누적값 전달

        // V
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("V (명도)", GUILayout.Width(PropertyLabelWidth));
        float prevV = vDelta;
        vDelta = EditorGUILayout.Slider(vDelta, -1f, 1f, GUILayout.Width(180));
        if (GUILayout.Button("✔️", GUILayout.Width(PropertyButtonWidth)))
        {
            SnapshotOriginalColors();
            vDelta = 0f;
            GUI.FocusControl(null);
        }
        EditorGUILayout.EndHorizontal();

        if (Mathf.Abs(vDelta - prevV) > 0.0001f)
            ApplySVADelta(selectionParticleSystems, sDelta, vDelta, aDelta); // ⚠️ V 변경 시 S, A 누적값 전달

        // A
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("A (알파)", GUILayout.Width(PropertyLabelWidth));
        float prevA = aDelta;
        aDelta = EditorGUILayout.Slider(aDelta, -1f, 1f, GUILayout.Width(180));
        if (GUILayout.Button("✔️", GUILayout.Width(PropertyButtonWidth)))
        {
            SnapshotOriginalColors();
            aDelta = 0f;
            GUI.FocusControl(null);
        }
        EditorGUILayout.EndHorizontal();

        if (Mathf.Abs(aDelta - prevA) > 0.0001f)
            ApplySVADelta(selectionParticleSystems, sDelta, vDelta, aDelta); // ⚠️ A 변경 시 S, V 누적값 전달
		
		EditorGUILayout.EndVertical();
    }


    // DrawUI(예: 선택 파티클 속성 증감)▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂
    void DrawUI_PropertyDelta()
    {
        GUILayout.Label("선택 파티클 속성 (Δ=증가, -Δ=감소)", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(GUI.skin.box);

        // Duration
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Duration", GUILayout.Width(PropertyLabelWidth));
        durationDelta = EditorGUILayout.FloatField(durationDelta, GUILayout.Width(PropertyFloatWidth));
        if (GUILayout.Button("✔️", GUILayout.Width(PropertyButtonWidth)))
            ApplySinglePropertyDelta(selectionParticleSystems, PropertyType.Duration, durationDelta);
        EditorGUILayout.EndHorizontal();

        // Start Delay
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Start Delay", GUILayout.Width(PropertyLabelWidth));
        delayDelta = EditorGUILayout.FloatField(delayDelta, GUILayout.Width(PropertyFloatWidth));
        if (GUILayout.Button("✔️", GUILayout.Width(PropertyButtonWidth)))
            ApplySinglePropertyDelta(selectionParticleSystems, PropertyType.StartDelay, delayDelta);
        EditorGUILayout.EndHorizontal();

        // Start Lifetime
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Start Lifetime", GUILayout.Width(PropertyLabelWidth));
        lifetimeDelta = EditorGUILayout.FloatField(lifetimeDelta, GUILayout.Width(PropertyFloatWidth));
        if (GUILayout.Button("✔️", GUILayout.Width(PropertyButtonWidth)))
            ApplySinglePropertyDelta(selectionParticleSystems, PropertyType.StartLifetime, lifetimeDelta);
        EditorGUILayout.EndHorizontal();

        // Start Speed
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Start Speed", GUILayout.Width(PropertyLabelWidth));
        speedDelta = EditorGUILayout.FloatField(speedDelta, GUILayout.Width(PropertyFloatWidth));
        if (GUILayout.Button("✔️", GUILayout.Width(PropertyButtonWidth)))
            ApplySinglePropertyDelta(selectionParticleSystems, PropertyType.StartSpeed, speedDelta);
        EditorGUILayout.EndHorizontal();

        // Start Size
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Start Size", GUILayout.Width(PropertyLabelWidth));
        sizeDelta = EditorGUILayout.FloatField(sizeDelta, GUILayout.Width(PropertyFloatWidth));
        if (GUILayout.Button("✔️", GUILayout.Width(PropertyButtonWidth)))
            ApplySinglePropertyDelta(selectionParticleSystems, PropertyType.StartSize, sizeDelta);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    // ────────────────────────────────────────────────────────────────────────────────────────────
    // Function_전체 ▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄
    // Function(예: 현재 선택된 단일/투컬러 파티클)▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂

    // 선택/색 스냅샷 관리
    private Dictionary<ParticleSystem, ParticleColorSnapshot> originalColors = new Dictionary<ParticleSystem, ParticleColorSnapshot>();
    private List<ParticleSystem> selectionParticleSystems = new List<ParticleSystem>();

    private void OnEnable()
    {
        RefreshSelection();
        CreateHueBarTexture();
    }
    private void OnFocus() => RefreshSelection();
    private void OnSelectionChange() { RefreshSelection(); Repaint(); }

    private void RefreshSelection()
    {
        selectionParticleSystems.Clear();
        originalColors.Clear();

        foreach (var obj in Selection.gameObjects)
        {
            var psList = obj.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in psList)
            {
                if (!selectionParticleSystems.Contains(ps))
                {
                    selectionParticleSystems.Add(ps);
                    SnapshotParticleColors(ps); // 신규/업데이트된 스냅샷 함수 사용
                }
            }
        }
    }

    // FunctionHueShift▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂
    private float hueShift = 0f, sDelta = 0f, vDelta = 0f, aDelta = 0f;
    private Texture2D hueBarTexture;
    private int hueBarWidth = 256, hueBarHeight = 16;

    void CreateHueBarTexture()
    {
        hueBarTexture = new Texture2D(hueBarWidth, hueBarHeight, TextureFormat.RGBA32, false);
        hueBarTexture.wrapMode = TextureWrapMode.Clamp;
        for (int x = 0; x < hueBarWidth; x++)
        {
            float h = (float)x / (hueBarWidth - 1);
            Color color = Color.HSVToRGB(h, 1f, 1f);
            for (int y = 0; y < hueBarHeight; y++) hueBarTexture.SetPixel(x, y, color);
        }
        hueBarTexture.Apply();
    }

    void SnapshotOriginalColors()
    {
        foreach (var ps in selectionParticleSystems)
        {
            if (!ps) continue;
            SnapshotParticleColors(ps);
        }
    }
    
    // 파티클 시스템의 색상 스냅샷을 찍는 분리된 함수
    void SnapshotParticleColors(ParticleSystem ps)
    {
        var main = ps.main;
        var col = ps.colorOverLifetime;

        // StartColor 스냅샷
        ParticleColorSnapshot snapshot;
        if (IsSingleColor(ps))
            snapshot = new ParticleColorSnapshot(main.startColor.color, Color.clear, Color.clear, main.startColor.gradient, null, null, null);
        else if (IsTwoColors(ps))
            snapshot = new ParticleColorSnapshot(Color.clear, main.startColor.colorMin, main.startColor.colorMax, null, null, null, null);
        else if (IsGradient(ps) || IsRandomColor(ps)) // RandomColor도 Gradient 처리
            snapshot = new ParticleColorSnapshot(Color.clear, Color.clear, Color.clear, main.startColor.gradient, null, null, null);
        else if (IsTwoGradients(ps))
            snapshot = new ParticleColorSnapshot(Color.clear, Color.clear, Color.clear, null, main.startColor.gradientMin, main.startColor.gradientMax, null);
        else
            snapshot = new ParticleColorSnapshot(Color.clear, Color.clear, Color.clear, null, null, null, null); // 지원되지 않는 모드

        // Color Over Lifetime 스냅샷 추가 (활성화된 경우)
        if (col.enabled)
        {
            snapshot.lifetimeSnapshot = new ParticleColorLifetimeSnapshot(col.color.mode, col.color.gradient, col.color.gradientMin, col.color.gradientMax);
        }

        originalColors[ps] = snapshot;
    }

    void ApplyHueShift(List<ParticleSystem> targets, float shift)
    {
        foreach (var ps in targets)
        {
            if (ps == null || !originalColors.ContainsKey(ps)) continue;
            Undo.RecordObject(ps, "Particle Hue Change");
            var orig = originalColors[ps];

            // 1. StartColor 변경
            var main = ps.main;
            ApplyStartColorHueShift(main, orig, shift);

            // 2. Color Over Lifetime 변경 (추가된 기능)
            var col = ps.colorOverLifetime;
            if (col.enabled && orig.lifetimeSnapshot != null)
            {
                ApplyLifetimeColorHueShift(col, orig.lifetimeSnapshot, shift);
            }
            
            EditorUtility.SetDirty(ps);
        }
    }
    
    // StartColor HueShift 적용 헬퍼
    void ApplyStartColorHueShift(ParticleSystem.MainModule main, ParticleColorSnapshot orig, float shift)
    {
        if (IsSingleColor(main))
        {
            Color.RGBToHSV(orig.origColor, out float h, out float s, out float v);
            h = Mathf.Repeat(h + shift, 1f);
            var rgb = Color.HSVToRGB(h, s, v); rgb.a = orig.origColor.a;
            main.startColor = rgb;
        }
        else if (IsTwoColors(main))
        {
            Color.RGBToHSV(orig.origColorMin, out float hMin, out float sMin, out float vMin);
            Color.RGBToHSV(orig.origColorMax, out float hMax, out float sMax, out float vMax);
            hMin = Mathf.Repeat(hMin + shift, 1f);
            hMax = Mathf.Repeat(hMax + shift, 1f);
            var rgbMin = Color.HSVToRGB(hMin, sMin, vMin); rgbMin.a = orig.origColorMin.a;
            var rgbMax = Color.HSVToRGB(hMax, sMax, vMax); rgbMax.a = orig.origColorMax.a;
            main.startColor = new ParticleSystem.MinMaxGradient(rgbMin, rgbMax);
        }
        else if ((IsGradient(main) || IsRandomColor(main)) && orig.gradient != null)
        {
            UnityEngine.Gradient g = ShiftGradientHue(orig.gradient, shift);
            main.startColor = new ParticleSystem.MinMaxGradient(g);
        }
        else if (IsTwoGradients(main) && orig.gradientMin != null && orig.gradientMax != null)
        {
            UnityEngine.Gradient gMin = ShiftGradientHue(orig.gradientMin, shift);
            UnityEngine.Gradient gMax = ShiftGradientHue(orig.gradientMax, shift);
            main.startColor = new ParticleSystem.MinMaxGradient(gMin, gMax);
        }
    }
    
    // Color Over Lifetime HueShift 적용 헬퍼 (추가됨)
    void ApplyLifetimeColorHueShift(ParticleSystem.ColorOverLifetimeModule col, ParticleColorLifetimeSnapshot orig, float shift)
    {
        if (orig.mode == ParticleSystemGradientMode.Gradient && orig.gradient != null)
        {
            UnityEngine.Gradient g = ShiftGradientHue(orig.gradient, shift);
            col.color = new ParticleSystem.MinMaxGradient(g);
        }
        else if (orig.mode == ParticleSystemGradientMode.TwoGradients && orig.gradientMin != null && orig.gradientMax != null)
        {
            UnityEngine.Gradient gMin = ShiftGradientHue(orig.gradientMin, shift);
            UnityEngine.Gradient gMax = ShiftGradientHue(orig.gradientMax, shift);
            col.color = new ParticleSystem.MinMaxGradient(gMin, gMax);
        }
    }


    // ⚠️ Gradient 타입을 UnityEngine.Gradient로 명시
    UnityEngine.Gradient ShiftGradientHue(UnityEngine.Gradient src, float shift)
    {
        UnityEngine.GradientColorKey[] ck = src.colorKeys;
        UnityEngine.GradientAlphaKey[] ak = src.alphaKeys;
        for (int i = 0; i < ck.Length; i++)
        {
            Color.RGBToHSV(ck[i].color, out float h, out float s, out float v);
            h = Mathf.Repeat(h + shift, 1f);
            Color c = Color.HSVToRGB(h, s, v); c.a = ck[i].color.a;
            ck[i].color = c;
        }
        UnityEngine.Gradient g = new UnityEngine.Gradient();
        g.SetKeys(ck, ak);
        g.mode = src.mode;
        return g;
    }

    // Function(예: 선택 파티클 속성 증감)▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂
    enum PropertyType { Duration, StartDelay, StartLifetime, StartSpeed, StartSize }

    void ApplySinglePropertyDelta(List<ParticleSystem> targets, PropertyType property, float delta)
    {
        foreach (var ps in targets)
        {
            if (!ps) continue;
            Undo.RecordObject(ps, "Particle Property Change");
            var main = ps.main;

            switch (property)
            {
                case PropertyType.Duration:
                    main.duration = Mathf.Max(0f, main.duration + delta);
                    break;

                case PropertyType.StartDelay:
                    {
                        var curve = main.startDelay;
                        if (curve.mode == ParticleSystemCurveMode.TwoConstants)
                            main.startDelay = new ParticleSystem.MinMaxCurve(
                                Mathf.Max(0f, curve.constantMin + delta),
                                Mathf.Max(0f, curve.constantMax + delta));
                        else
                            main.startDelay = new ParticleSystem.MinMaxCurve(Mathf.Max(0f, curve.constant + delta));
                        break;
                    }

                case PropertyType.StartLifetime:
                    {
                        var curve = main.startLifetime;
                        if (curve.mode == ParticleSystemCurveMode.TwoConstants)
                            main.startLifetime = new ParticleSystem.MinMaxCurve(
                                Mathf.Max(0f, curve.constantMin + delta),
                                Mathf.Max(0f, curve.constantMax + delta));
                        else
                            main.startLifetime = new ParticleSystem.MinMaxCurve(Mathf.Max(0f, curve.constant + delta));
                        break;
                    }

                case PropertyType.StartSpeed:
                    {
                        var curve = main.startSpeed;
                        if (curve.mode == ParticleSystemCurveMode.TwoConstants)
                            main.startSpeed = new ParticleSystem.MinMaxCurve(
                                Mathf.Max(0f, curve.constantMin + delta),
                                Mathf.Max(0f, curve.constantMax + delta));
                        else
                            main.startSpeed = new ParticleSystem.MinMaxCurve(Mathf.Max(0f, curve.constant + delta));
                        break;
                    }

                case PropertyType.StartSize:
                    {
                        var curve = main.startSize;
                        if (curve.mode == ParticleSystemCurveMode.TwoConstants)
                            main.startSize = new ParticleSystem.MinMaxCurve(
                                Mathf.Max(0f, curve.constantMin + delta),
                                Mathf.Max(0f, curve.constantMax + delta));
                        else
                            main.startSize = new ParticleSystem.MinMaxCurve(Mathf.Max(0f, curve.constant + delta));
                        break;
                    }
            }
            EditorUtility.SetDirty(ps);
        }
    }

    void ApplySVADelta(List<ParticleSystem> targets, float sDelta, float vDelta, float aDelta)
    {
        foreach (var ps in targets)
        {
            if (ps == null || !originalColors.ContainsKey(ps)) continue;
            Undo.RecordObject(ps, "Particle SVA Change");
            var orig = originalColors[ps];

            // 1. StartColor 변경
            var main = ps.main;
            ApplyStartColorSVADelta(main, orig, sDelta, vDelta, aDelta);

            // 2. Color Over Lifetime 변경 (수정: aDelta를 0으로 전달하여 알파 컨트롤 제외)
            var col = ps.colorOverLifetime;
            if (col.enabled && orig.lifetimeSnapshot != null)
            {
                ApplyLifetimeColorSVADelta(col, orig.lifetimeSnapshot, sDelta, vDelta, 0f);
            }
            
            EditorUtility.SetDirty(ps);
        }
    }

    // StartColor SVA Delta 적용 헬퍼 (수정: 원래 채도 S가 0일 때 sDelta 무시 - 회색/검은색/흰색 모두 해당)
    void ApplyStartColorSVADelta(ParticleSystem.MainModule main, ParticleColorSnapshot orig, float sDelta, float vDelta, float aDelta)
    {
        if (IsSingleColor(main))
        {
            Color.RGBToHSV(orig.origColor, out float h, out float s, out float v);
            float a = orig.origColor.a;
            
            // 💡 수정된 로직: 원래 채도가 0이면 (회색 계열) sDelta 무시
            if (s > 0.0001f) // s가 0이 아니면 (색상이 있으면) 채도 적용
                s = Mathf.Clamp01(s + sDelta);
            // else s는 0으로 유지됨

            v = Mathf.Clamp01(v + vDelta);
            a = Mathf.Clamp01(a + aDelta);
            var rgb = Color.HSVToRGB(h, s, v); rgb.a = a;
            main.startColor = rgb;
        }
        else if (IsTwoColors(main))
        {
            // ColorMin
            Color.RGBToHSV(orig.origColorMin, out float hMin, out float sMin, out float vMin);
            // ColorMax
            Color.RGBToHSV(orig.origColorMax, out float hMax, out float sMax, out float vMax);

            float aMin = orig.origColorMin.a, aMax = orig.origColorMax.a;
            
            // 💡 ColorMin: 원래 채도가 0이면 sDelta 무시
            if (sMin > 0.0001f)
                sMin = Mathf.Clamp01(sMin + sDelta); 
            // 💡 ColorMax: 원래 채도가 0이면 sDelta 무시
            if (sMax > 0.0001f)
                sMax = Mathf.Clamp01(sMax + sDelta);
            
            vMin = Mathf.Clamp01(vMin + vDelta); vMax = Mathf.Clamp01(vMax + vDelta);
            aMin = Mathf.Clamp01(aMin + aDelta); aMax = Mathf.Clamp01(aMax + aDelta);

            var rgbMin = Color.HSVToRGB(hMin, sMin, vMin); rgbMin.a = aMin;
            var rgbMax = Color.HSVToRGB(hMax, sMax, vMax); rgbMax.a = aMax;
            main.startColor = new ParticleSystem.MinMaxGradient(rgbMin, rgbMax);
        }
        else if ((IsGradient(main) || IsRandomColor(main)) && orig.gradient != null)
        {
            UnityEngine.Gradient g = ShiftGradientSVA(orig.gradient, sDelta, vDelta, aDelta);
            main.startColor = new ParticleSystem.MinMaxGradient(g);
        }
        else if (IsTwoGradients(main) && orig.gradientMin != null && orig.gradientMax != null)
        {
            UnityEngine.Gradient gMin = ShiftGradientSVA(orig.gradientMin, sDelta, vDelta, aDelta);
            UnityEngine.Gradient gMax = ShiftGradientSVA(orig.gradientMax, sDelta, vDelta, aDelta);
            main.startColor = new ParticleSystem.MinMaxGradient(gMin, gMax);
        }
    }
    
    // Color Over Lifetime SVA Delta 적용 헬퍼 (수정: aDelta 무시)
    void ApplyLifetimeColorSVADelta(ParticleSystem.ColorOverLifetimeModule col, ParticleColorLifetimeSnapshot orig, float sDelta, float vDelta, float aDelta)
    {
        // aDelta는 무시됩니다 (Color Over Lifetime 알파 컨트롤 제외 요청)
        if (orig.mode == ParticleSystemGradientMode.Gradient && orig.gradient != null)
        {
            UnityEngine.Gradient g = ShiftGradientSVA(orig.gradient, sDelta, vDelta, 0f); 
            col.color = new ParticleSystem.MinMaxGradient(g);
        }
        else if (orig.mode == ParticleSystemGradientMode.TwoGradients && orig.gradientMin != null && orig.gradientMax != null)
        {
            UnityEngine.Gradient gMin = ShiftGradientSVA(orig.gradientMin, sDelta, vDelta, 0f);
            UnityEngine.Gradient gMax = ShiftGradientSVA(orig.gradientMax, sDelta, vDelta, 0f);
            col.color = new ParticleSystem.MinMaxGradient(gMin, gMax);
        }
    }

    // ⚠️ Gradient 타입을 UnityEngine.Gradient로 명시
    // (수정: 원래 채도 S가 0일 때 sDelta 무시 & Alpha Key에 대한 aDelta 적용 로직 제거)
    UnityEngine.Gradient ShiftGradientSVA(UnityEngine.Gradient src, float sDelta, float vDelta, float aDelta)
    {
        UnityEngine.GradientColorKey[] ck = src.colorKeys;
        UnityEngine.GradientAlphaKey[] ak = src.alphaKeys;

        for (int i = 0; i < ck.Length; i++)
        {
            Color c = ck[i].color;
            Color.RGBToHSV(c, out float h, out float s, out float v);
            
            // 💡 수정된 로직: 원래 채도가 0이면 sDelta 무시
            if (s > 0.0001f) // s가 0이 아니면 (색상이 있으면) 채도 적용
                s = Mathf.Clamp01(s + sDelta);
            // else s는 0으로 유지됨
            
            v = Mathf.Clamp01(v + vDelta);
            
            Color nc = Color.HSVToRGB(h, s, v); 
            nc.a = c.a; // 기존 알파값 유지
            
            ck[i].color = nc;
        }
        
        // ❌ Color Over Lifetime 알파 컨트롤 제외 요청에 따라 aDelta 적용 로직 제거
        /*
        for (int i = 0; i < ak.Length; i++)
            ak[i].alpha = Mathf.Clamp01(ak[i].alpha + aDelta); 
        */

        // ⚠️ UnityEngine.Gradient 명시
        UnityEngine.Gradient g = new UnityEngine.Gradient();
        g.SetKeys(ck, ak); 
        g.mode = src.mode;
        return g;
    }


    // 판별 헬퍼 (ParticleSystem.MainModule을 인수로 받도록 오버로드)
    bool IsSingleColor(ParticleSystem.MainModule main)   => main.startColor.mode == ParticleSystemGradientMode.Color;
    bool IsTwoColors(ParticleSystem.MainModule main)     => main.startColor.mode == ParticleSystemGradientMode.TwoColors;
    bool IsGradient(ParticleSystem.MainModule main)      => main.startColor.mode == ParticleSystemGradientMode.Gradient;
    bool IsTwoGradients(ParticleSystem.MainModule main)  => main.startColor.mode == ParticleSystemGradientMode.TwoGradients;
    bool IsRandomColor(ParticleSystem.MainModule main)   => main.startColor.mode == ParticleSystemGradientMode.RandomColor;
    
    // 기존 함수 유지
    bool IsSingleColor(ParticleSystem ps)   => ps.main.startColor.mode == ParticleSystemGradientMode.Color;
    bool IsTwoColors(ParticleSystem ps)     => ps.main.startColor.mode == ParticleSystemGradientMode.TwoColors;
    bool IsGradient(ParticleSystem ps)      => ps.main.startColor.mode == ParticleSystemGradientMode.Gradient;
    bool IsTwoGradients(ParticleSystem ps)  => ps.main.startColor.mode == ParticleSystemGradientMode.TwoGradients;
    bool IsRandomColor(ParticleSystem ps)   => ps.main.startColor.mode == ParticleSystemGradientMode.RandomColor;


    // Δ 값 보관
    private float durationDelta = 0f, delayDelta = 0f, lifetimeDelta = 0f, speedDelta = 0f, sizeDelta = 0f;

    // Color Over Lifetime 스냅샷 구조체 (추가됨)
    class ParticleColorLifetimeSnapshot
    {
        public ParticleSystemGradientMode mode;
        public UnityEngine.Gradient gradient, gradientMin, gradientMax;

        public ParticleColorLifetimeSnapshot(ParticleSystemGradientMode mode, UnityEngine.Gradient g, UnityEngine.Gradient gmin, UnityEngine.Gradient gmax)
        { this.mode = mode; gradient = g; gradientMin = gmin; gradientMax = gmax; }
    }

    // 색 스냅샷 구조체
    class ParticleColorSnapshot
    {
        public Color origColor, origColorMin, origColorMax;
        // ⚠️ Gradient 타입을 UnityEngine.Gradient로 명시
        public UnityEngine.Gradient gradient, gradientMin, gradientMax, extra;
        public ParticleColorLifetimeSnapshot lifetimeSnapshot; // Color Over Lifetime 스냅샷 추가

        public ParticleColorSnapshot(Color c, Color cmin, Color cmax, UnityEngine.Gradient g, UnityEngine.Gradient gmin, UnityEngine.Gradient gmax, UnityEngine.Gradient extra)
        { 
            origColor = c; origColorMin = cmin; origColorMax = cmax; 
            gradient = g; gradientMin = gmin; gradientMax = gmax; this.extra = extra;
            lifetimeSnapshot = null; // 초기화
        }
    }
}