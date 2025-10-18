/*
@name: _FolderNavigator
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

using UnityEditor;
using UnityEngine;
using System.IO;

[InitializeOnLoad]
public class _FolderNavigator : EditorWindow
{
    private string assetPath = "Assets/";
    private const string PrefKey_WindowShouldOpen = "_FolderNavigator_ShouldOpen";
    private static readonly Vector2 MIN_WINDOW_SIZE = new Vector2(250f, 40f); // 📐 세로 최소 크기를 40f로 더 줄임

    static _FolderNavigator()
    {
        EditorApplication.update += TryReopen;
    }

    static void TryReopen()
    {
        EditorApplication.update -= TryReopen;

        if (EditorPrefs.GetBool(PrefKey_WindowShouldOpen, false))
        {
            var window = GetWindow<_FolderNavigator>("_FolderNavigator");
            window.minSize = MIN_WINDOW_SIZE; // ♻️ 다시 열 때도 최소 크기를 강제로 적용
        }
    }

    [MenuItem("Tools/@FX_Tools/_FolderNavigator")]
    public static void ShowWindow()
    {
        var window = GetWindow<_FolderNavigator>("_FolderNavigator");
        window.minSize = MIN_WINDOW_SIZE; // ⚙️ 창을 열 때 최소 크기를 설정
        window.Show();
        EditorPrefs.SetBool(PrefKey_WindowShouldOpen, true);
    }

    void OnGUI()
    {
        // 1. 레이블의 세로 공간을 줄이기 위해 GUILayout.Height를 사용하거나, 
        // 아예 Style을 사용하지 않고 GUILayout.Label만 사용할 수 있지만, 
        // 여기서는 GUILayout.Space로 레이블 위아래 여백을 줄여봅니다.

        // 상단 여백 제거 (선택적)
        GUILayout.Space(2); 

        // 🏷️ 레이블을 일반 스타일로 사용하여 세로 크기를 최소화하고, 여백을 줄입니다.
        GUILayout.Label("경로 입력 (예: Assets/Bundle/UI)"); // EditorStyles.boldLabel 대신 일반 스타일 사용

        // 입력창과 Enter 버튼을 같은 줄에 나란히 배치
        GUILayout.BeginHorizontal();
        GUILayout.Label("Path:", GUILayout.Width(40));
        GUI.SetNextControlName("PathField");
        // 텍스트 필드의 세로 크기는 기본적으로 한 줄 높이입니다.
        assetPath = EditorGUILayout.TextField(assetPath, GUILayout.ExpandWidth(true));
        if (GUILayout.Button("Enter", GUILayout.Width(60), GUILayout.Height(18))) // 📏 버튼 높이를 명시적으로 줄입니다. (선택적)
        {
            MoveToPath();
        }
        GUILayout.EndHorizontal();

        // 하단 여백 제거 (선택적)
        GUILayout.Space(2);

        // Enter 키 입력 처리
        if (Event.current.isKey && 
			Event.current.keyCode == KeyCode.Return && 
			GUI.GetNameOfFocusedControl() == "PathField")
        {
            MoveToPath();
            Event.current.Use();
        }
    }

    void MoveToPath()
    {
        // ... (MoveToPath 메서드 내용은 동일)
        // 앞에 슬래시가 붙었으면 제거
        if (assetPath.StartsWith("/"))
        {
            assetPath = assetPath.TrimStart('/');
        }

        // 여전히 Assets로 시작하지 않으면 오류 처리
        if (!assetPath.StartsWith("Assets"))
        {
            EditorUtility.DisplayDialog("오류", "경로는 반드시 'Assets'로 시작해야 합니다.", "확인");
            return;
        }

        if (AssetDatabase.IsValidFolder(assetPath))
        {
            Object folder = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            Selection.activeObject = folder;
            EditorGUIUtility.PingObject(folder);
        }
        else
        {
            EditorUtility.DisplayDialog("오류", "해당 경로를 찾을 수 없습니다:\n" + assetPath, "확인");
        }
    }

    void OnDestroy()
    {
        // 창이 수동으로 닫혔을 때는 다시 자동 열리지 않도록 설정
        EditorPrefs.SetBool(PrefKey_WindowShouldOpen, false);
    }
}