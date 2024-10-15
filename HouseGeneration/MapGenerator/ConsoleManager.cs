using System;
using System.Collections.Generic;
using Accord.IO;
using ImGuiNET;

namespace HouseGeneration.MapGeneratorRenderer
{
    public class ConsoleManager
    {
        private List<string> _consoleMessages = new List<string>();
        private const int MaxMessages = 100;

        public void AddMessage(string message)
        {
            _consoleMessages.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            if (_consoleMessages.Count > MaxMessages)
            {
                _consoleMessages.RemoveAt(0);
            }
        }

        public void Draw()
        {
            ImGui.Begin("Console");

            foreach (var message in _consoleMessages.DeepClone())
            {
                ImGui.Text(message);
            }

            if (ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
            {
                ImGui.SetScrollHereY(1.0f);
            }

            ImGui.End();
        }
    }
}