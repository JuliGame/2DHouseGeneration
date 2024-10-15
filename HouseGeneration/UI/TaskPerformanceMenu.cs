using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace HouseGeneration.UI
{
    public class TaskPerformanceMenu
    {
        private Dictionary<string, List<long>> _taskDurations = new Dictionary<string, List<long>>();
        private Dictionary<string, Stopwatch> _taskStopwatches = new Dictionary<string, Stopwatch>();
        
        // Add these new fields
        private List<Dictionary<string, List<long>>> _profiles = new List<Dictionary<string, List<long>>>();
        private int _currentProfileIndex = 0;
        private string _newProfileName = string.Empty;

        public void StartTask(string taskName)
        {
            if (!_taskStopwatches.ContainsKey(taskName))
            {
                _taskStopwatches[taskName] = new Stopwatch();
            }
            _taskStopwatches[taskName].Restart();
        }

        public void EndTask(string taskName)
        {
            if (_taskStopwatches.TryGetValue(taskName, out var stopwatch))
            {
                stopwatch.Stop();
                long duration = stopwatch.ElapsedMilliseconds;

                if (!_taskDurations.ContainsKey(taskName))
                {
                    _taskDurations[taskName] = new List<long>();
                }
                _taskDurations[taskName].Add(duration);
            }
            else
            {
                // Handle the case where EndTask is called without a corresponding StartTask
                Console.WriteLine($"Warning: EndTask called for '{taskName}' without a corresponding StartTask.");
            }
        }

        public void Draw()
        {
            ImGui.Begin("Task Performance");

            // Add profile management UI
            DrawProfileManagement();

            if (_taskDurations.Count > 0)
            {
                DrawPieChart();
            }
            else
            {
                ImGui.Text("No tasks recorded for the current profile.");
            }

            ImGui.End();
        }

        private void DrawProfileManagement()
        {
            // Display current profile
            ImGui.Text($"Current Profile: {_currentProfileIndex + 1} / {_profiles.Count + 1}");

            // Previous profile button
            if (ImGui.Button("Previous Profile") && _currentProfileIndex > 0)
            {
                SwitchToProfile(_currentProfileIndex - 1);
            }
            ImGui.SameLine();

            // Next profile button
            if (ImGui.Button("Next Profile") && _currentProfileIndex < _profiles.Count)
            {
                SwitchToProfile(_currentProfileIndex + 1);
            }
            ImGui.SameLine();

            // New profile input and button
            ImGui.SetNextItemWidth(150);
            ImGui.InputText("##NewProfileName", ref _newProfileName, 100);
            ImGui.SameLine();
            if (ImGui.Button("Create New Profile"))
            {
                CreateNewProfile();
            }
        }

        private void SwitchToProfile(int index)
        {
            if (index == _profiles.Count)
            {
                // Switching to a new profile
                _currentProfileIndex = index;
                _taskDurations = new Dictionary<string, List<long>>();
            }
            else if (index >= 0 && index < _profiles.Count)
            {
                _currentProfileIndex = index;
                _taskDurations = _profiles[index];
            }
        }

        private void CreateNewProfile()
        {
            if (!string.IsNullOrWhiteSpace(_newProfileName))
            {
                // Save current profile if it's not empty
                if (_taskDurations.Count > 0)
                {
                    _profiles.Add(new Dictionary<string, List<long>>(_taskDurations));
                }

                // Create new profile
                _taskDurations = new Dictionary<string, List<long>>();
                _currentProfileIndex = _profiles.Count;
                _newProfileName = string.Empty;
            }
        }

        private void DrawPieChart()
        {
            var averageDurations = _taskDurations.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Average()
            );

            float total = (float) averageDurations.Values.Sum();

            ImGui.Text("Task Performance Breakdown");
            ImGui.Spacing();

            System.Numerics.Vector2 center = ImGui.GetCursorScreenPos() + new System.Numerics.Vector2(250, 100);
            float radius = 80;
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            float currentAngle = 0;
            foreach (var task in averageDurations)
            {
                float slice = (float)task.Value / total;
                float nextAngle = currentAngle + slice * 2 * MathF.PI;

                ImGui.ColorConvertHSVtoRGB(currentAngle / (2 * MathF.PI), 0.7f, 0.7f, out float r, out float g, out float b);
                uint color = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(r, g, b, 1));

                drawList.PathArcTo(center, radius, currentAngle, nextAngle, 32);
                drawList.PathStroke(color, ImDrawFlags.None, 2);

                System.Numerics.Vector2 labelPos = center + new System.Numerics.Vector2(MathF.Cos((currentAngle + nextAngle) / 2), MathF.Sin((currentAngle + nextAngle) / 2)) * (radius * 1.3f);
                
                // Determine if the label is on the left side of the circle
                bool isLeftSide = labelPos.X < center.X;

                string labelText = $"{task.Key}: {(float)task.Value:F0}ms";
                System.Numerics.Vector2 textSize = ImGui.CalcTextSize(labelText);

                // Adjust label position based on which side it's on
                if (isLeftSide)
                {
                    labelPos.X -= textSize.X + 5; // Move text to the left of the point
                }
                else
                {
                    labelPos.X += 5; // Add a small offset to the right
                }

                drawList.AddText(labelPos, color, labelText);

                currentAngle = nextAngle;
            }

            ImGui.Dummy(new System.Numerics.Vector2(200, 200 * 1.3f));

            // Sort tasks by average duration (descending order)
            var sortedTasks = averageDurations.OrderByDescending(x => x.Value);

            if (ImGui.BeginTable("TaskPerformanceTable", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Task Name", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Avg Duration", ImGuiTableColumnFlags.WidthFixed, 100);
                ImGui.TableSetupColumn("Percentage", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableSetupColumn("Samples", ImGuiTableColumnFlags.WidthFixed, 70);
                ImGui.TableHeadersRow();

                foreach (var task in sortedTasks)
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text(task.Key);
                    
                    ImGui.TableSetColumnIndex(1);
                    ImGui.Text($"{task.Value:F1}ms");
                    
                    ImGui.TableSetColumnIndex(2);
                    float percentage = (float)task.Value / total * 100;
                    ImGui.Text($"{percentage:F1}%");
                    
                    ImGui.TableSetColumnIndex(3);
                    ImGui.Text($"{_taskDurations[task.Key].Count}");
                }

                ImGui.EndTable();
            }
        }
    }
}
