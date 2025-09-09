using System;
using System.Collections.Generic;
using System.Linq;
using Nanover.Core;
using NanoverImd;
using UnityEditor;
using UnityEngine;

namespace NanoverImd.Editor
{
    /// <summary>
    /// Editor Window to display current multiplayer shared state.
    /// </summary>
    public class SharedStateWindow : EditorWindow
    {
        [MenuItem("Nanover/Windows/Multiplayer Shared State")]
        [MenuItem("Window/Nanover/Multiplayer Shared State")]
        private static void Init()
        {
            var window = (SharedStateWindow) GetWindow(typeof(SharedStateWindow));
            window.titleContent = new GUIContent("Multiplayer State");
            window.Show();
        }

        private Vector2 scrollPos;
        private HashSet<string> expanded = new HashSet<string>();

        private NanoverImdSimulation simulation;

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.LabelField("Application not running.");
                return;
            }

            if (simulation == null)
            {
                simulation = FindFirstObjectByType<NanoverImdSimulation>();
                if (simulation != null)
                    OnConnectToSession();
            }

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            if (simulation != null)
            {
                if (simulation.Multiplayer.IsOpen)
                    DrawSharedState(simulation.Multiplayer.SharedStateDictionary);
                else
                    EditorGUILayout.HelpBox("Session not connected", MessageType.Error);
            }

            EditorGUILayout.EndScrollView();
        }

        private void Update()
        {
            if (EditorApplication.isPlaying && !EditorApplication.isPaused)
            {
                Repaint();
            }
        }

        private void OnConnectToSession()
        {
            simulation.Multiplayer.SharedStateDictionaryKeyUpdated += KeyUpdated;
        }

        private Dictionary<string, DateTime> lastUpdate = new Dictionary<string, DateTime>();

        private void KeyUpdated(string key, object value)
        {
            lastUpdate[key] = DateTime.Now;
        }

        private void DrawSharedState(Dictionary<string, object> state)
        {
            foreach (var (key, value) in state.OrderBy(kvp => kvp.Key))
            {
                var isExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(expanded.Contains(key),
                                                        key,
                                                        null,
                                                        ShowHeaderContextMenu(
                                                            () => RemoveKey(key)));
                if(isExpanded)
                    expanded.Add(key);
                else
                    expanded.Remove(key);
                
                if (isExpanded)
                {
                    if (lastUpdate.ContainsKey(key))
                    {
                        var time = DateTime.Now - lastUpdate[key];
                        EditorGUILayout.LabelField($"Last updated {(int) time.TotalSeconds} second(s) ago.", EditorStyles.miniLabel);
                    }
                    DrawObject(value);
                }

                EditorGUILayout.EndFoldoutHeaderGroup();
            }
        }

        /// <summary>
        /// Remove a given key from the shared state.
        /// </summary>
        /// <param name="key"></param>
        private void RemoveKey(string key)
        {
            simulation.Multiplayer.RemoveSharedStateKey(key);
        }

        private static Action<Rect> ShowHeaderContextMenu(Action deleteAction)
        {
            return (position) =>
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Delete"), false, () => deleteAction());
                menu.DropDown(position);
            };
        }

        /// <summary>
        /// Draw a tree structure describing an item of the shared tate
        /// </summary>
        private static void DrawObject(object value, string key = null)
        {
            void DrawDictionary<TKey>(IDictionary<TKey, object> dictionary)
            {
                if (key != null)
                    EditorGUILayout.LabelField($"{key}:");
                else
                    EditorGUILayout.LabelField("[Dictionary]:");
                
                EditorGUI.indentLevel += 2;
                foreach (var field in dictionary)
                {
                    DrawObject(field.Value, field.Key.ToString());
                }
                EditorGUI.indentLevel -= 2;
            }

            switch (value)
            {
                case IDictionary<string, object> dict:
                    DrawDictionary(dict);
                    break;
                case IDictionary<object, object> dict2:
                    DrawDictionary(dict2);
                    break;
                case IList<object> list:
                    if (key != null)
                        EditorGUILayout.LabelField($"{key}:");
                    else
                        EditorGUILayout.LabelField("[List]:");

                    EditorGUI.indentLevel += 2;
                    if (list.Count > 8)
                    {
                        EditorGUILayout.LabelField(string.Join(", ", list), new GUIStyle(EditorStyles.label) { wordWrap = true });
                    }
                    else
                    {
                        foreach (var field in list)
                        {
                            DrawObject(field);
                        }
                    }
                    EditorGUI.indentLevel -= 2;
                    break;
                default:
                    if (key != null)
                        EditorGUILayout.LabelField($"{key}:", $"{value}");
                    else
                        EditorGUILayout.LabelField($"{value}");
                    break;
            }
        }
    }
}