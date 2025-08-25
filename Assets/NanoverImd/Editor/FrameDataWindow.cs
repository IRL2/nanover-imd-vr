using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nanover.Core;
using Nanover.Frame;
using Nanover.Frame.Event;
using NanoverImd;
using UnityEditor;
using UnityEngine;

namespace NanoverImd.Editor
{
    /// <summary>
    /// Editor Window to display current FrameData.
    /// </summary>
    public class FrameDataWindow : EditorWindow
    {
        [MenuItem("Nanover/Windows/FrameData")]
        [MenuItem("Window/Nanover/FrameData")]
        private static void Init()
        {
            var window = (FrameDataWindow) GetWindow(typeof(FrameDataWindow));
            window.titleContent = new GUIContent("FrameData");
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
                if (simulation.Trajectory.CurrentFrame is { } frame)
                    Draw(frame, simulation.Trajectory.CurrentFrameIndex);
                else
                    EditorGUILayout.HelpBox("No current frame", MessageType.Error);
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

        private FrameChanges lastChanges = FrameChanges.None;

        private void OnConnectToSession()
        {
            simulation.Trajectory.FrameChanged += (frame, changes) =>
            {
                foreach (var (key, value) in frame.Data)
                {
                    if (changes.HasChanged(key))
                        KeyUpdated(key, value);
                }

                lastChanges = changes;
            };
        }

        private Dictionary<string, DateTime> lastUpdate = new Dictionary<string, DateTime>();

        private void KeyUpdated(string key, object value)
        {
            lastUpdate[key] = DateTime.Now;
        }

        private void Draw(Frame frame, int index)
        {
            var changed = new GUIStyle(EditorStyles.foldoutHeader);
            changed.normal.textColor = Color.green;

            {
                EditorGUILayout.LabelField($"Frame Index: {index}");
                EditorGUILayout.LabelField($"Data Count: {frame.Data.Count}");
            }

            foreach (var (key, value) in frame.Data.OrderBy(kvp => kvp.Key))
            {
                var isExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(
                    expanded.Contains(key),
                    key,
                    lastChanges.HasChanged(key) ? changed : null);

                if (isExpanded)
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
        /// Draw a tree structure describing an item of the shared tate
        /// </summary>
        private static void DrawObject(object value, string key = null)
        {
            if (value.GetType().IsArray)
            {
                if (key != null)
                    EditorGUILayout.LabelField($"{key}:");
                else
                    EditorGUILayout.LabelField("[array]:");
                EditorGUI.indentLevel += 2;

                EditorStyles.label.wordWrap = true;
                DrawObject(string.Join(", ", (value as IEnumerable).Cast<object>()));

                EditorGUI.indentLevel -= 2;
            }
            else
            {
                if (key != null)
                    EditorGUILayout.LabelField($"{key}:", $"{value}");
                else
                    EditorGUILayout.LabelField($"{value}");
            }
        }
    }
}