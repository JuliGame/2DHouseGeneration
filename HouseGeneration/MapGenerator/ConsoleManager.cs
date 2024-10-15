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
                    ImGui.Text($"[{timestamp:yyyy-MM-dd HH:mm:ss}] {message}");
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
