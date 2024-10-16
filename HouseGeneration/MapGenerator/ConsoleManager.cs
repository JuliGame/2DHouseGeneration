using System;
using System.Collections.Concurrent;
using Accord.IO;
using ImGuiNET;

namespace HouseGeneration.MapGeneratorRenderer
{
    public class ConsoleManager
    {
        private readonly ConcurrentQueue<(DateTime timestamp, string message)> _consoleMessages = new ConcurrentQueue<(DateTime, string)>();
        private const int MaxMessages = 100;

        public void AddMessage(string message)
        {
            _consoleMessages.Enqueue((DateTime.Now, message));
            
            // Remove oldest messages if we exceed the maximum
            while (_consoleMessages.Count > MaxMessages)
            {
                _consoleMessages.TryDequeue(out _);
            }
        }

        public void Draw()
        {
            ImGui.Begin("Console");

            try
            {
                foreach (var (timestamp, message) in _consoleMessages)
                {
                    string fullMessage = $"[{timestamp:yyyy-MM-dd HH:mm:ss}] {message}";
                    ImGui.Text(fullMessage);

                    // Add hover effect
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Click to copy");
                        ImGui.GetWindowDrawList().AddRectFilled(
                            ImGui.GetItemRectMin(),
                            ImGui.GetItemRectMax(),
                            ImGui.GetColorU32(ImGuiCol.HeaderHovered)
                        );
                    }

                    // Handle click to copy
                    if (ImGui.IsItemClicked())
                    {
                        ImGui.SetClipboardText(message);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            if (ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
            {
                ImGui.SetScrollHereY(1.0f);
            }

            ImGui.End();
        }
    }
}
