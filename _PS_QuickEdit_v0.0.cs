/*
@name: _PS_QuickEdit
@version: 0.0

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

// Window ▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄
public class _PS_QuickEdit : EditorWindow
{
	// 버튼 툴팁용 GUIContent (GC_*) — 한 번만 생성해 GC 감소
static readonly GUIContent GC_Parent = new GUIContent(
    "Parent",
    "Particle System 부모 세팅"
);
static readonly GUIContent GC_Child = new GUIContent(
    "Child",
    "Particle System 자식 세팅"
);
static readonly GUIContent GC_Main = new GUIContent(
    "Main",
    "• Rect/Transform Z=0 고정\n" +
    "• Delta Time: Scaled\n" +
    "• ScalingMode: Hierarchy\n" +
    "• Emitter Velocity: Transform\n" +
    "• Stop Action: None\n" +
    "• Culling: Automatic\n" +
    "• Ring Buffer: Disabled"
);
static readonly GUIContent GC_UIChild = new GUIContent(
    "UI Child",
    "• Convert to RectTransform\n" +
    "• Add Particle System and disable Renderer\n" +
    "• Add Canvas Renderer\n" +
    "• Set Cull Transparent Mesh to false\n" +
    "• Add UI Particles script"
);



    // === UI 상태 (Foldout) ===
    bool showAdd = true, showLoop = true, showMirror = true, showEmission = true,
         showShape = true, showVelocity = true, showSize = true,
         showRotation = true, showAlign = true, showHelper = true;

    // === 스타일 ===
GUIStyle _titleStyle, _sectionTitleStyle, _foldoutNoBg;

// 지연 초기화 (필요할 때만 생성)
void EnsureStyles()
{
    if (_titleStyle == null)
    {
        _titleStyle = new GUIStyle(EditorStyles.foldoutHeader)
        {
            fontSize  = 13,
            fontStyle = FontStyle.Bold
        };
        _titleStyle.normal.textColor   = Color.white;
        _titleStyle.onNormal.textColor = Color.white;
    }

    if (_sectionTitleStyle == null)
    {
        _sectionTitleStyle = new GUIStyle(EditorStyles.boldLabel);
        _sectionTitleStyle.normal.textColor = Color.white;
    }

    // 배경 없는 폴드아웃 (헤더 배경을 우리가 칠할 때 라벨 영역이 다시 덮이지 않도록)
    if (_foldoutNoBg == null)
    {
        _foldoutNoBg = new GUIStyle(EditorStyles.foldout)
        {
            fontSize  = 13,
            fontStyle = FontStyle.Bold
        };
        _foldoutNoBg.normal.textColor   = Color.white;
        _foldoutNoBg.onNormal.textColor = Color.white;
    }
}

// 선택 파티클 추출 ▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄
    private List<ParticleSystem> GetCurrentSelection()
    {
        List<ParticleSystem> selectionParticleSystems = new List<ParticleSystem>();
        foreach (var obj in Selection.gameObjects)
        {
            var psList = obj.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in psList)
            {
                if (!selectionParticleSystems.Contains(ps))
                    selectionParticleSystems.Add(ps);
            }
        }
        return selectionParticleSystems;
    }

// UI_전체 ▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄
public void OnGUI()
{
    EnsureStyles(); // ★ 스타일 보장
    GUILayout.Space(4);

    // 각 섹션을 박스 + Foldout으로 감싸기
    Section("ParticleSystem 추가", ref showAdd,        () => DrawUIParticleSystem());
    Section("선택파티클 Loop",     ref showLoop,       () => DrawUILoop());
    Section("Mirror (Velocity/Force/Gravity)", ref showMirror, () => DrawUIMirror());
    Section("Emission",            ref showEmission,   () => DrawUIEmission());
    Section("Shape",               ref showShape,      () => DrawUIShape());
    Section("Velocity",            ref showVelocity,   () => DrawUIVelocity());
    Section("Size",                ref showSize,       () => DrawUISize());
    Section("Rotation",             ref showRotation,   () => DrawUIRotation());
    Section("Align",                ref showAlign,       () => DrawUIAlign());
	Section("Helper",                ref showHelper,       () => DrawUIHelper());
}

// 버튼 배경색 다크 테마 시작/복원 헬퍼 ▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂
Color BeginDarkButtons(float gray = 0.30f)
{
    Color prev = GUI.backgroundColor;
    GUI.backgroundColor = new Color(gray, gray, gray);
    return prev;
}

void EndDarkButtons(Color prev)
{
    GUI.backgroundColor = prev;
}

void Section(string title, ref bool foldout, System.Action body)
{
    EnsureStyles();
    GUILayout.Space(6);
    EditorGUILayout.BeginVertical("box");
    GUILayout.Space(2);

    // 헤더 배경
    Rect headerRect = GUILayoutUtility.GetRect(1f, 20f, GUILayout.ExpandWidth(true));
    EditorGUI.DrawRect(headerRect, new Color(0.2f, 0.2f, 0.2f));

    // ▼ 여기서 흰색 라인 추가 (아래쪽 1픽셀)
    Rect lineRect = new Rect(headerRect.x, headerRect.yMax - 1f, headerRect.width, 1f);
    EditorGUI.DrawRect(lineRect, new Color(1f, 0.5f, 0.5f, 0.7f)); // 마지막 값이 투명도

    // Foldout
    foldout = EditorGUI.Foldout(headerRect, foldout, title, true, _foldoutNoBg);

    if (foldout)
    {
        GUILayout.Space(1);
        Color _prev = BeginDarkButtons(0.4f);
        try { body?.Invoke(); }
        finally { EndDarkButtons(_prev); }
    }

    GUILayout.Space(2);
    EditorGUILayout.EndVertical();
}


// UI_구조 ▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂
const float BUTTON_MARGIN = 20f; // 여유여백(스크롤바 등, 한 군데에서 관리)
float GetContentWidth()
{
    return EditorGUIUtility.currentViewWidth - BUTTON_MARGIN;
}
	// DrawUIParticleSystem ▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂
    void DrawUIParticleSystem()
{
    float totalWidth = GetContentWidth();
    float btnWidth = totalWidth / 4f;

    EditorGUILayout.BeginHorizontal();
	
    if (GUILayout.Button(GC_Parent, GUILayout.Width(btnWidth)))
    FunctionParent();

	if (GUILayout.Button(GC_Child, GUILayout.Width(btnWidth)))
    FunctionChild();

	if (GUILayout.Button(GC_Main, GUILayout.Width(btnWidth)))
    FunctionMain();

	if (GUILayout.Button(GC_UIChild, GUILayout.Width(btnWidth)))
    FunctionUIChild();
		
    EditorGUILayout.EndHorizontal();
}


    // DrawUILoop ▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂
    void DrawUILoop()
{
    float totalWidth = GetContentWidth();
    float btnWidth = totalWidth / 3f;

    EditorGUILayout.BeginHorizontal();

    if (GUILayout.Button("OneShot", GUILayout.Width(btnWidth)))
        FunctionLoop(false, false);

    if (GUILayout.Button("Loop", GUILayout.Width(btnWidth)))
        FunctionLoop(true, false);

    if (GUILayout.Button("Loop_Prewarm", GUILayout.Width(btnWidth)))
        FunctionLoop(true, true);

    EditorGUILayout.EndHorizontal();
}


    // DrawUIMirror ▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂
    void DrawUIMirror()
{
    float totalWidth = GetContentWidth();
    float btnWidth = totalWidth / 3f;

    EditorGUILayout.BeginHorizontal();

    if (GUILayout.Button("←   X   →", GUILayout.Width(btnWidth)))
        FunctionMirror(0);

    if (GUILayout.Button("↓   Y   ↑", GUILayout.Width(btnWidth)))
        FunctionMirror(1);

    if (GUILayout.Button("Z", GUILayout.Width(btnWidth)))
        FunctionMirror(2);

    EditorGUILayout.EndHorizontal();
}


    // DrawUIEmission ▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂
	void DrawUIEmission()
{
    float totalWidth = GetContentWidth();
    float btnWidth = totalWidth / 3f;

    EditorGUILayout.BeginHorizontal();

    if (GUILayout.Button("Burst1", GUILayout.Width(btnWidth)))
        FunctionEmission(1);
    if (GUILayout.Button("BurstMulti", GUILayout.Width(btnWidth)))
        FunctionEmission(2);
    if (GUILayout.Button("BurstSlow", GUILayout.Width(btnWidth)))
        FunctionEmission(3);
    
	
    EditorGUILayout.EndHorizontal();

    // 두 번째 줄 버튼 (OneLoop)
    EditorGUILayout.BeginHorizontal();

    if (GUILayout.Button("Loop1", GUILayout.Width(btnWidth)))
        FunctionEmission(4);
	if (GUILayout.Button("LoopMulti", GUILayout.Width(btnWidth)))
        FunctionEmission(5);

    EditorGUILayout.EndHorizontal();
}

    // DrawUIShape ▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂
	void DrawUIShape()
{
    float totalWidth = GetContentWidth();
    float btnWidth = totalWidth / 3f;

    EditorGUILayout.BeginHorizontal();

    if (GUILayout.Button("PresetBtn1", GUILayout.Width(btnWidth)))
        FunctionShape(1);

    if (GUILayout.Button("PresetBtn2", GUILayout.Width(btnWidth)))
        FunctionShape(2);

    if (GUILayout.Button("PresetBtn3", GUILayout.Width(btnWidth)))
        FunctionShape(3);

    EditorGUILayout.EndHorizontal();
}

    // DrawUIVelocity ▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂
	void DrawUIVelocity()
{
    float totalWidth = GetContentWidth();
    float btnWidth = totalWidth / 3f;

    EditorGUILayout.BeginHorizontal();

    if (GUILayout.Button(" 날라가다멈추기", GUILayout.Width(btnWidth)))
        FunctionVelocity(1);

    if (GUILayout.Button("날라가다빠르게", GUILayout.Width(btnWidth)))
        FunctionVelocity(2);

    if (GUILayout.Button("PresetBtn3", GUILayout.Width(btnWidth)))
        FunctionVelocity(3);

    EditorGUILayout.EndHorizontal();
}

	// DrawUISize ▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂
	void DrawUISize()
{
    float totalWidth = GetContentWidth();
    float btnWidth = totalWidth / 3f;

    EditorGUILayout.BeginHorizontal();

    if (GUILayout.Button("크기 점점 작게", GUILayout.Width(btnWidth)))
        FunctionSize(1);

    if (GUILayout.Button("크기 점점 크게", GUILayout.Width(btnWidth)))
        FunctionSize(2);

    if (GUILayout.Button("PresetBtn3", GUILayout.Width(btnWidth)))
        FunctionSize(3);

    EditorGUILayout.EndHorizontal();
}

	// DrawUIRotation ▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂
	void DrawUIRotation()
{
    float totalWidth = GetContentWidth();
    float btnWidth = totalWidth / 3f;

    EditorGUILayout.BeginHorizontal();

    if (GUILayout.Button("회전하다 천천히", GUILayout.Width(btnWidth)))
        FunctionRotation(1);

    if (GUILayout.Button("회전하다 빠르게", GUILayout.Width(btnWidth)))
        FunctionRotation(2);

    if (GUILayout.Button("PresetBtn3", GUILayout.Width(btnWidth)))
        FunctionRotation(3);

    EditorGUILayout.EndHorizontal();
}

	// DrawUIAlign ▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂
	void DrawUIAlign()
{
    float totalWidth = GetContentWidth();
    float btnWidth = totalWidth / 3f;

    EditorGUILayout.BeginHorizontal();

    if (GUILayout.Button("Velocity 방향 정렬", GUILayout.Width(btnWidth)))
        FunctionAlign(1);

    if (GUILayout.Button("바닥정렬_Horizontal", GUILayout.Width(btnWidth)))
        FunctionAlign(2);

    if (GUILayout.Button("바닥정렬_Mesh", GUILayout.Width(btnWidth)))
        FunctionAlign(3);

    EditorGUILayout.EndHorizontal();
}

	// DrawUIHelper ▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂
	void DrawUIHelper()
{
    float totalWidth = GetContentWidth();
    float btnWidth = totalWidth / 2f;

    EditorGUILayout.BeginHorizontal();

    if (GUILayout.Button("오브젝트따라 이동 Local", GUILayout.Width(btnWidth)))
        FunctionHelper(ParticleSystemSimulationSpace.Local);

    if (GUILayout.Button("독립적으로 존재 World", GUILayout.Width(btnWidth)))
        FunctionHelper(ParticleSystemSimulationSpace.World);

    EditorGUILayout.EndHorizontal();
}



// Function_전체 ▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄

	// FunctionParent ▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂
    void FunctionParent()
{
    foreach (var obj in Selection.gameObjects)
    {
        var existingPs = obj.GetComponent<ParticleSystem>();
        if (existingPs != null)
        {
            // 값 변경을 Undo에 기록
            Undo.RecordObject(existingPs, "Configure Particle System");

            // 1) 재생 중지 (비직렬, Undo 영향 X) – 유지해도 됨
            existingPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // 2) 설정 변경
            var main = existingPs.main;
            main.duration = 1f;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            var emission = existingPs.emission; emission.enabled = false;
            var shape    = existingPs.shape;    shape.enabled    = false;

            // Renderer 토글도 Undo에 기록
            var psr = obj.GetComponent<ParticleSystemRenderer>();
            if (psr != null)
            {
                Undo.RecordObject(psr, "Toggle ParticleSystemRenderer");
                psr.enabled = false;
                EditorUtility.SetDirty(psr);
            }

            EditorUtility.SetDirty(existingPs);
            continue;
        }

        // 새 컴포넌트는 AddComponent 대신 Undo.AddComponent 사용!
        var ps = Undo.AddComponent<ParticleSystem>(obj);

        var main2 = ps.main;
        main2.duration = 1f;
        main2.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var emission2 = ps.emission; emission2.enabled = false;
        var shape2    = ps.shape;    shape2.enabled    = false;

        var psr2 = obj.GetComponent<ParticleSystemRenderer>();
        if (psr2 != null)
        {
            Undo.RecordObject(psr2, "Toggle ParticleSystemRenderer");
            psr2.enabled = false;
            EditorUtility.SetDirty(psr2);
        }

        EditorUtility.SetDirty(ps);
    }
}

    void FunctionChild()
{
    foreach (var obj in Selection.gameObjects)
    {
        var existingPs = obj.GetComponent<ParticleSystem>();
        if (existingPs != null)
        {
            Undo.RecordObject(existingPs, "Configure Particle System");
            existingPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = existingPs.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            EditorUtility.SetDirty(existingPs);
            continue;
        }

        var ps = Undo.AddComponent<ParticleSystem>(obj);

        var main2 = ps.main;
        main2.scalingMode = ParticleSystemScalingMode.Hierarchy;

        EditorUtility.SetDirty(ps);
    }
}

    void FunctionMain()
{
    // 1) 트랜스폼 Z 정리
    foreach (var go in Selection.gameObjects)
    {
        if (!go) continue;

        // RectTransform이면 local Z, 아니면 world Z
        var rect = go.GetComponent<RectTransform>();
        if (rect != null)
        {
            Undo.RecordObject(rect, "RectTransform Z to 0");
            var lp = rect.localPosition;
            lp.z = 0f;
            rect.localPosition = lp;
            EditorUtility.SetDirty(rect);
        }
        else
        {
            var tf = go.transform;
            Undo.RecordObject(tf, "Transform Z to 0");
            var p = tf.position;
            p.z = 0f;
            tf.position = p;
            EditorUtility.SetDirty(tf);
        }
    }

    // 2) 파티클 시스템 공통 세팅
    foreach (var go in Selection.gameObjects)
    {
        var systems = go.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems)
        {
            if (!ps) continue;

            Undo.RecordObject(ps, "Apply Main Preset");

            var main = ps.main;

            // Scaling Mode → Hierarchy
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            // Ring Buffer Mode → Disabled
#if UNITY_2020_2_OR_NEWER
            main.ringBufferMode = ParticleSystemRingBufferMode.Disabled;
#endif

            // Culling Mode → Automatic
#if UNITY_2019_3_OR_NEWER
            main.cullingMode = ParticleSystemCullingMode.Automatic;
#endif

            // Stop Action → None
            main.stopAction = ParticleSystemStopAction.None;

            // Emitter Velocity Mode → Transform
#if UNITY_2019_3_OR_NEWER
            main.emitterVelocityMode = ParticleSystemEmitterVelocityMode.Transform;
#endif

            // Delta Time → Scaled (UnscaledTime 사용 안 함)
#if UNITY_2019_3_OR_NEWER
            main.useUnscaledTime = false;
#endif

            EditorUtility.SetDirty(ps);
        }
    }
}

	
void FunctionUIChild()
{
    var selectedObjects = Selection.gameObjects;
    if (selectedObjects.Length == 0)
    {
        Debug.LogWarning("Please select one or more GameObjects in the Hierarchy.");
        return;
    }

    foreach (var go in selectedObjects)
    {
        Undo.SetCurrentGroupName("Set UI Particle System");

        // 1. Layer를 'UI'로 설정
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer != -1)
        {
            if (go.layer != uiLayer)
            {
                Undo.RecordObject(go, "Set Layer to UI");
                go.layer = uiLayer;
            }
        }
        else
        {
            Debug.LogWarning("UI Layer not found. Please create a 'UI' Layer in the Layers settings.");
        }

        // 2. Transform을 RectTransform으로 변경 (RectTransform이 없다면 추가)
        var rt = go.GetComponent<RectTransform>();
        if (rt == null)
        {
            rt = Undo.AddComponent<RectTransform>(go);
        }

        // 3. Particle System 스크립트 추가 및 Renderer 비활성화
        var ps = go.GetComponent<ParticleSystem>();
        if (ps == null)
        {
            ps = Undo.AddComponent<ParticleSystem>(go);
        }
        var psr = ps.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            Undo.RecordObject(psr, "Disable ParticleSystemRenderer");
            psr.enabled = false;
        }

        // 4. Canvas Renderer 스크립트 추가
        var cr = go.GetComponent<CanvasRenderer>();
        if (cr == null)
        {
            cr = Undo.AddComponent<CanvasRenderer>(go);
        }

        // 5. Cull Transparent Mesh 체크 해제
        if (cr != null)
        {
            Undo.RecordObject(cr, "Disable Cull Transparent Mesh");
            cr.cullTransparentMesh = false;
        }

        // 6. UI Particles 스크립트 추가 및 Raycast Target 비활성화
        try
        {
            var uiParticlesType = System.Type.GetType("UIParticles, Assembly-CSharp");
            if (uiParticlesType != null)
            {
                var uiParticles = go.GetComponent(uiParticlesType);
                if (uiParticles == null)
                {
                    uiParticles = Undo.AddComponent(go, uiParticlesType);
                }

                if (uiParticles != null)
                {
                    var uiGraphic = uiParticles as UnityEngine.UI.Graphic;
                    if (uiGraphic != null)
                    {
                        Undo.RecordObject(uiGraphic, "Disable Raycast Target");
                        uiGraphic.raycastTarget = false;
                        EditorUtility.SetDirty(uiGraphic);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Could not add UIParticles script. Make sure the script exists and is named 'UIParticles'. Error: " + e.Message);
        }
    }
}


    // FunctionLoop ▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂
    void FunctionLoop(bool loop, bool prewarm)
    {
        var selectionParticleSystems = GetCurrentSelection();
        foreach (var ps in selectionParticleSystems)
        {
            if (ps == null) continue;
            var main = ps.main;
            Undo.RecordObject(ps, loop ? (prewarm ? "Set Loop+Prewarm" : "Set Loop") : "Set OneShot");
            main.loop = loop;
            main.prewarm = prewarm;
            EditorUtility.SetDirty(ps);
        }
    }

    // FunctionMirror ▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂
    void FunctionMirror(int axis)
    {
        var targets = GetCurrentSelection();
        foreach (var ps in targets)
        {
            if (ps == null) continue;
            Undo.RecordObject(ps, "Particle Mirror");

            var vel = ps.velocityOverLifetime;
            switch (axis)
            {
                case 0: vel.xMultiplier *= -1; break;
                case 1: vel.yMultiplier *= -1; break;
                case 2: vel.zMultiplier *= -1; break;
            }

            var force = ps.forceOverLifetime;
            switch (axis)
            {
                case 0: force.xMultiplier *= -1; break;
                case 1: force.yMultiplier *= -1; break;
                case 2: force.zMultiplier *= -1; break;
            }

            var main = ps.main;
            if (axis == 1)
                main.gravityModifierMultiplier *= -1;

            var speedCurve = main.startSpeed;
            if (speedCurve.mode == ParticleSystemCurveMode.TwoConstants)
            {
                float newMin = -speedCurve.constantMin;
                float newMax = -speedCurve.constantMax;
                main.startSpeed = new ParticleSystem.MinMaxCurve(newMin, newMax);
            }
            else
            {
                float newValue = -speedCurve.constant;
                main.startSpeed = new ParticleSystem.MinMaxCurve(newValue);
            }
            EditorUtility.SetDirty(ps);
        }
    }
	
	// FunctionEmission ▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂
void FunctionEmission(int idx)
{
    var selectionParticleSystems = GetCurrentSelection();
    foreach (var ps in selectionParticleSystems)
    {
        if (ps == null) continue;
        Undo.RecordObject(ps, "Set Emission Preset");

        var emission = ps.emission;
        emission.enabled = true;

        // 공통 초기화
        emission.rateOverTime = 0f;
        emission.rateOverDistance = 0f;
        emission.SetBursts(new ParticleSystem.Burst[0]);

        switch (idx)
        {
            case 1: // Burst1
            {
                var burst = new ParticleSystem.Burst(0f, 1);
                burst.repeatInterval = 0.01f;
                emission.SetBursts(new[] { burst });
                break;
            }
            case 2: // BurstMulti
            {
                var burst = new ParticleSystem.Burst(0f, 9, 15);
                burst.repeatInterval = 0.01f;
                emission.SetBursts(new[] { burst });
                break;
            }
            case 3: // BurstSlow (커브 + Burst 0)
            {
                var burst = new ParticleSystem.Burst(0f, 0);
                burst.repeatInterval = 0.01f;
                emission.SetBursts(new[] { burst });

                AnimationCurve curve = new AnimationCurve(
                    new Keyframe(0.0f, 0.5f),
                    new Keyframe(0.1f, 1.0f),
                    new Keyframe(0.5f, 0.0f)
                );
                curve.SmoothTangents(0, 0);
                emission.rateOverTime = new ParticleSystem.MinMaxCurve(1f, curve);
                break;
            }
            case 4: // OneLoop (Rate over Time 30, Burst 없음)
            {
				emission.rateOverTime = new ParticleSystem.MinMaxCurve(30f);

				ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

				var main = ps.main;
				main.duration = 1f;
				main.loop = true;
				main.startLifetime = 1f;
				main.maxParticles = 1;
				break;
            }
        case 5: // LoopMulti (상하한, Burst 없음)
            {
                var burst = new ParticleSystem.Burst(0f, 0);
                burst.repeatInterval = 0.01f;
                emission.SetBursts(new[] { burst });
                emission.rateOverTime = new ParticleSystem.MinMaxCurve(5f, 15f);
                break;
            }
        }

        EditorUtility.SetDirty(ps);
    }
}


	
	// FunctionShape ▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂
	void FunctionShape(int idx)
{
    EditorUtility.DisplayDialog("Shape", $"Preset 버튼 {idx}이(가) 작동합니다", "확인");
}
	
	
	// FunctionVelocity ▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂
	void FunctionVelocity(int idx)
{
    var targets = GetCurrentSelection();
    if (targets.Count == 0) return; // 팝업 없이 조용히 종료

    switch (idx)
    {
        case 1: // PresetBtn1 → Limit ON + Speed 커브 + Dampen=0.1
        {
            foreach (var ps in targets)
            {
                if (ps == null) continue;
                Undo.RecordObject(ps, "Velocity Preset 1");

                var limit = ps.limitVelocityOverLifetime;
                limit.enabled = true;
                limit.separateAxes = false;

                var curve = new AnimationCurve(
                    new Keyframe(0f, 1f),
                    new Keyframe(1f, 0f)
                );
                limit.limit = new ParticleSystem.MinMaxCurve(1f, curve);
                limit.dampen = 0.1f;

                EditorUtility.SetDirty(ps);
            }
            break;
        }

        case 2: // PresetBtn2 → Force over Lifetime ON
        {
            foreach (var ps in targets)
            {
                if (ps == null) continue;
                Undo.RecordObject(ps, "Velocity Preset 2");
                var fol = ps.forceOverLifetime;
                fol.enabled = true;
                EditorUtility.SetDirty(ps);
            }
            break;
        }

        case 3: // PresetBtn3 → Velocity over Lifetime ON
        {
            foreach (var ps in targets)
            {
                if (ps == null) continue;
                Undo.RecordObject(ps, "Velocity Preset 3");
                var vol = ps.velocityOverLifetime;
                vol.enabled = true;
                EditorUtility.SetDirty(ps);
            }
            break;
        }
    }
}
	
	// FunctionSize ▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂
	void FunctionSize(int idx)
{
    var targets = GetCurrentSelection();
    if (targets.Count == 0) return; // 팝업 없이 종료

    switch (idx)
    {
        case 1:
        {
            foreach (var ps in targets)
            {
                if (!ps) continue;
                Undo.RecordObject(ps, "Size Preset 1");

                var sol = ps.sizeOverLifetime;
                sol.enabled = true;          // 모듈 켜기
                sol.separateAxes = false;    // 한 축으로만

                // ─ Size 커브 (예시: 그림처럼 1 → 점점 감소 → 0)
                //   0s:1.0  0.2s:0.5  0.4s:0.25  1.0s:0.0
                var curve = new AnimationCurve(
                    new Keyframe(0.00f, 1.00f),
                    new Keyframe(0.20f, 0.50f),
                    new Keyframe(1.00f, 0.00f)
                );
                // 부드럽게
                for (int i = 0; i < curve.length; i++) curve.SmoothTangents(i, 0f);

                sol.size = new ParticleSystem.MinMaxCurve(1f, curve); // multiplier=1, 단일 커브

                EditorUtility.SetDirty(ps);
            }
            break;
        }

        case 2:
        {
            foreach (var ps in targets)
            {
                if (!ps) continue;
                Undo.RecordObject(ps, "Size Preset 1");

                var sol = ps.sizeOverLifetime;
                sol.enabled = true;          // 모듈 켜기
                sol.separateAxes = false;    // 한 축으로만

                var curve = new AnimationCurve(
				new Keyframe(0.00f, 0.25f, 0f, 6f),   // 시작: 낮게, 급하게 올라가도록 outTangent↑
				new Keyframe(0.15f, 0.65f, 0.5f, 0.2f),   // 무릎 지점
				new Keyframe(1.00f, 1.00f, 0f, 0f)    // 끝: 평평하게(inTangent=0)
				);
                // 부드럽게
                for (int i = 0; i < curve.length; i++) curve.SmoothTangents(i, 0f);

                sol.size = new ParticleSystem.MinMaxCurve(1f, curve); // multiplier=1, 단일 커브

                EditorUtility.SetDirty(ps);
            }
            break;
        }

        case 3:
        {
            foreach (var ps in targets)
            {
                if (!ps) continue;
                Undo.RecordObject(ps, "Size Preset 3");

                var sbs = ps.sizeBySpeed;
                sbs.enabled = true;          // 모듈 켜기만
                // sbs.range = new Vector2(0f, 1f); // 필요시

                EditorUtility.SetDirty(ps);
            }
            break;
        }
    
	}
}

	// FunctionRotation ▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂
	void FunctionRotation(int idx)
{
    var targets = GetCurrentSelection();
    if (targets.Count == 0) return;

    switch (idx)
    {
        case 1:
{
    foreach (var ps in targets)
    {
        if (!ps) continue;

        Undo.RecordObject(ps, "Rotation Slow Preset");

        var rol = ps.rotationOverLifetime;
        rol.enabled = true;
        rol.separateAxes = false;       // 단일 축 사용

        // Linear: 시작 45 → 끝 0
        var curve = new AnimationCurve(
            new Keyframe(0f, 1f),
			new Keyframe(0.15f, 0.4f),
            new Keyframe(1f, 0f)
        );
#if UNITY_EDITOR
        AnimationUtility.SetKeyLeftTangentMode (curve, 0, AnimationUtility.TangentMode.Linear);
        AnimationUtility.SetKeyRightTangentMode(curve, 0, AnimationUtility.TangentMode.Linear);
        AnimationUtility.SetKeyLeftTangentMode (curve, 1, AnimationUtility.TangentMode.Linear);
        AnimationUtility.SetKeyRightTangentMode(curve, 1, AnimationUtility.TangentMode.Linear);
#endif

        // ★ multiplier(배율) 리셋 필수! (안 하면 45가 600 등으로 증폭되어 보일 수 있음)
        rol.zMultiplier = 1f;

        // 커브 적용
        rol.z = new ParticleSystem.MinMaxCurve(1f, curve);

        EditorUtility.SetDirty(ps);
    }
    break;
}

        case 2:
{
    foreach (var ps in targets)
    {
        if (!ps) continue;

        Undo.RecordObject(ps, "Rotation Slow Preset");

        var rol = ps.rotationOverLifetime;
        rol.enabled = true;
        rol.separateAxes = false;       // 단일 축 사용

        // Linear: 시작 45 → 끝 0
        var curve = new AnimationCurve(
    new Keyframe(0.00f, 0.00f),
    new Keyframe(0.12f, 0.70f),
    new Keyframe(1.00f, 1.00f)
);
#if UNITY_EDITOR
        AnimationUtility.SetKeyLeftTangentMode (curve, 0, AnimationUtility.TangentMode.Linear);
        AnimationUtility.SetKeyRightTangentMode(curve, 0, AnimationUtility.TangentMode.Linear);
        AnimationUtility.SetKeyLeftTangentMode (curve, 1, AnimationUtility.TangentMode.Linear);
        AnimationUtility.SetKeyRightTangentMode(curve, 1, AnimationUtility.TangentMode.Linear);
#endif

        // ★ multiplier(배율) 리셋 필수! (안 하면 45가 600 등으로 증폭되어 보일 수 있음)
        rol.zMultiplier = 1f;

        // 커브 적용
        rol.z = new ParticleSystem.MinMaxCurve(1f, curve);

        EditorUtility.SetDirty(ps);
    }
    break;
}

        case 3:
{
    foreach (var ps in targets)
    {
        if (!ps) continue;

        Undo.RecordObject(ps, "Rotation Slow Preset");

        var rol = ps.rotationOverLifetime;
        rol.enabled = true;
        rol.separateAxes = false;       // 단일 축 사용

        // Linear: 시작 45 → 끝 0
        var curve = new AnimationCurve(
            new Keyframe(0f, 1f),
			new Keyframe(0.15f, 0.4f),
            new Keyframe(1f, 0f)
        );
#if UNITY_EDITOR
        AnimationUtility.SetKeyLeftTangentMode (curve, 0, AnimationUtility.TangentMode.Linear);
        AnimationUtility.SetKeyRightTangentMode(curve, 0, AnimationUtility.TangentMode.Linear);
        AnimationUtility.SetKeyLeftTangentMode (curve, 1, AnimationUtility.TangentMode.Linear);
        AnimationUtility.SetKeyRightTangentMode(curve, 1, AnimationUtility.TangentMode.Linear);
#endif

        // ★ multiplier(배율) 리셋 필수! (안 하면 45가 600 등으로 증폭되어 보일 수 있음)
        rol.zMultiplier = 1f;

        // 커브 적용
        rol.z = new ParticleSystem.MinMaxCurve(1f, curve);

        EditorUtility.SetDirty(ps);
    }
    break;
}

		
    }
}

	// FunctionAlign ▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂
	void FunctionAlign(int idx)
{
    var targets = GetCurrentSelection();
    if (targets.Count == 0) return;

    foreach (var ps in targets)
    {
        if (!ps) continue;

        switch (idx)
        {
            case 1: // Renderer ON + Stretched Billboard + SpeedScale 0.5 + Length 1
{
    var psr = ps.GetComponent<ParticleSystemRenderer>();
    if (psr == null) break;

    Undo.RecordObject(psr, "Renderer Preset 1");

    if (!psr.enabled) psr.enabled = true;                 // Renderer 활성화
    psr.renderMode    = ParticleSystemRenderMode.Stretch; // ← 여기!
    psr.velocityScale = 0.5f;                             // Speed Scale
    psr.lengthScale   = 1.0f;                             // Length Scale

    EditorUtility.SetDirty(psr);
    break;
}

            case 2: // Color over Lifetime 활성화만
            {
    var psr = ps.GetComponent<ParticleSystemRenderer>();
    if (psr == null) break;

    Undo.RecordObject(psr, "Renderer Preset 2");

    if (!psr.enabled) psr.enabled = true;  // Renderer 활성화
    psr.renderMode = ParticleSystemRenderMode.HorizontalBillboard;

    EditorUtility.SetDirty(psr);
    break;
}

            case 3: // Trails 활성화만
            {
    var psr = ps.GetComponent<ParticleSystemRenderer>();
    if (psr == null) break;

    // ─ Renderer 쪽 변경 (Undo: Renderer)
    Undo.RecordObject(psr, "Renderer Preset 3");

    if (!psr.enabled) psr.enabled = true;                 // Renderer 활성화
    psr.renderMode = ParticleSystemRenderMode.Mesh;       // Render Mode → Mesh

    // Meshes → Quad (내장 리소스)
    var quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
    if (quad != null)
        psr.SetMeshes(new[] { quad });

    // Pivot → Y = 0.45
    psr.pivot = new Vector3(0f, 0.45f, 0f);

    // Render Alignment → World
    psr.alignment = ParticleSystemRenderSpace.World;

    EditorUtility.SetDirty(psr);

    // ─ Main 모듈의 3D Start Rotation (Undo: ParticleSystem)
    Undo.RecordObject(ps, "Set 3D Start Rotation");
    var main = ps.main;
    main.startRotation3D = true;

    // X: 90° (고정)
    main.startRotationX = new ParticleSystem.MinMaxCurve(90f * Mathf.Deg2Rad);

    EditorUtility.SetDirty(ps);
    break;
}
        }
    }
}
	
	// FunctionHelper ▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂
	void FunctionHelper(ParticleSystemSimulationSpace space)
{
    foreach (var go in Selection.gameObjects)
    {
        if (!go) continue;

        var systems = go.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems)
        {
            if (!ps) continue;

            Undo.RecordObject(ps, "Set Simulation Space");
            var main = ps.main;
            main.simulationSpace = space;
            EditorUtility.SetDirty(ps);
        }
    }

    // (선택) 에디터 갱신
    SceneView.RepaintAll();
    Repaint();
}
	
	
}