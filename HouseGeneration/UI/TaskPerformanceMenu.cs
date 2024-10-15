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
        private class TaskNode
        {
            public string Name { get; set; }
            public List<long> Durations { get; set; } = new List<long>();
            public Dictionary<string, TaskNode> Children { get; set; } = new Dictionary<string, TaskNode>();
            public TaskNode Parent { get; set; }
            public Stopwatch Stopwatch { get; set; } = new Stopwatch();
        }

        private TaskNode _rootTask = new TaskNode { Name = "Root" };
        private TaskNode _currentTask;
        private Dictionary<string, TaskNode> _activeTasks = new Dictionary<string, TaskNode>();
        
        private List<TaskNode> _profiles = new List<TaskNode>();
        private int _currentProfileIndex = 0;
        private string _newProfileName = string.Empty;

        public TaskPerformanceMenu()
        {
            _currentTask = _rootTask;
        }

        public void StartTask(string taskName)
        {
            string[] taskParts = taskName.Split('-');
            TaskNode currentNode = _rootTask;

            for (int i = 0; i < taskParts.Length; i++)
            {
                string currentTaskName = string.Join("-", taskParts.Take(i + 1));
                if (!currentNode.Children.TryGetValue(taskParts[i], out TaskNode childNode))
                {
                    childNode = new TaskNode { Name = taskParts[i], Parent = currentNode };
                    currentNode.Children[taskParts[i]] = childNode;
                }
                currentNode = childNode;
            }

            currentNode.Stopwatch.Restart();
            _activeTasks[taskName] = currentNode;
        }

        public void EndTask(string taskName)
        {
            if (_activeTasks.TryGetValue(taskName, out TaskNode node))
            {
                node.Stopwatch.Stop();
                node.Durations.Add(node.Stopwatch.ElapsedMilliseconds);
                _activeTasks.Remove(taskName);
            }
            else
            {
                Console.WriteLine($"Warning: EndTask called for '{taskName}' without a corresponding StartTask.");
            }
        }

        public void Draw()
        {
            ImGui.Begin("Task Performance");

            DrawProfileManagement();

            if (_rootTask.Children.Count > 0)
            {
                if (ImGui.Button("Back") && _currentTask.Parent != null)
                {
                    _currentTask = _currentTask.Parent;
                }

                DrawPieChart(_currentTask);
                DrawTaskTable(_currentTask);
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
                _rootTask = new TaskNode { Name = "Root" };
            }
            else if (index >= 0 && index < _profiles.Count)
            {
                _currentProfileIndex = index;
                _rootTask = _profiles[index];
            }
            _currentTask = _rootTask;
        }

        private void CreateNewProfile()
        {
            if (!string.IsNullOrWhiteSpace(_newProfileName))
            {
                // Save current profile if it's not empty
                if (_rootTask.Children.Count > 0)
                {
                    _profiles.Add(_rootTask);
                }

                // Create new profile
                _rootTask = new TaskNode { Name = "Root" };
                _currentTask = _rootTask;
                _currentProfileIndex = _profiles.Count;
                _newProfileName = string.Empty;
            }
        }

        private void DrawPieChart(TaskNode node)
        {
            var taskDurations = GetAverageDurations(node);
            float total = taskDurations.Values.Sum();

            ImGui.Text($"{node.Name} Performance Breakdown");
            ImGui.Spacing();

            System.Numerics.Vector2 center = ImGui.GetCursorScreenPos() + new System.Numerics.Vector2(250, 100);
            float radius = 80;
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            float currentAngle = 0;
            foreach (var task in taskDurations)
            {
                float slice = task.Value / total;
                float nextAngle = currentAngle + slice * 2 * MathF.PI;

                ImGui.ColorConvertHSVtoRGB(currentAngle / (2 * MathF.PI), 0.7f, 0.7f, out float r, out float g, out float b);
                uint color = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(r, g, b, 1));

                drawList.PathArcTo(center, radius, currentAngle, nextAngle, 32);
                drawList.PathStroke(color, ImDrawFlags.None, 2);

                System.Numerics.Vector2 labelPos = center + new System.Numerics.Vector2(MathF.Cos((currentAngle + nextAngle) / 2), MathF.Sin((currentAngle + nextAngle) / 2)) * (radius * 1.3f);
                
                bool isLeftSide = labelPos.X < center.X;
                string labelText = $"{task.Key}: {task.Value:F0}ms";
                System.Numerics.Vector2 textSize = ImGui.CalcTextSize(labelText);

                if (isLeftSide)
                {
                    labelPos.X -= textSize.X + 5;
                }
                else
                {
                    labelPos.X += 5;
                }

                drawList.AddText(labelPos, color, labelText);

                currentAngle = nextAngle;
            }

            ImGui.Dummy(new System.Numerics.Vector2(200, 200 * 1.3f));
        }

        private void DrawTaskTable(TaskNode node)
        {
            var taskDurations = GetAverageDurations(node);
            float total = taskDurations.Values.Sum();

            if (ImGui.BeginTable($"TaskPerformanceTable_{node.Name}", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 20);
                ImGui.TableSetupColumn("Task Name", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Avg Duration", ImGuiTableColumnFlags.WidthFixed, 100);
                ImGui.TableSetupColumn("Percentage", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableSetupColumn("Samples", ImGuiTableColumnFlags.WidthFixed, 70);
                ImGui.TableHeadersRow();

                foreach (var task in taskDurations.OrderByDescending(x => x.Value))
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    
                    TaskNode childNode = node.Children[task.Key];
                    bool hasChildren = childNode.Children.Count > 0;
                    
                    if (hasChildren)
                    {
                        if (ImGui.ArrowButton($"##{childNode.Name}", ImGuiDir.Right))
                        {
                            _currentTask = childNode;
                        }
                    }
                    else
                    {
                        ImGui.Dummy(new System.Numerics.Vector2(ImGui.GetFrameHeight(), ImGui.GetFrameHeight()));
                    }

                    ImGui.TableSetColumnIndex(1);
                    ImGui.Text(task.Key);
                    
                    ImGui.TableSetColumnIndex(2);
                    ImGui.Text($"{task.Value:F1}ms");
                    
                    ImGui.TableSetColumnIndex(3);
                    float percentage = task.Value / total * 100;
                    ImGui.Text($"{percentage:F1}%");
                    
                    ImGui.TableSetColumnIndex(4);
                    ImGui.Text($"{childNode.Durations.Count}");
                }

                ImGui.EndTable();
            }
        }

        private Dictionary<string, float> GetAverageDurations(TaskNode node)
        {
            var result = new Dictionary<string, float>();
            foreach (var child in node.Children)
            {
                result[child.Key] = (float) (child.Value.Durations.Count > 0 ? child.Value.Durations.Average() : 0);
            }
            return result;
        }
    }
}