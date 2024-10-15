using System.IO;
using System.Text;

namespace HouseGeneration.MapGeneratorRenderer
{
    public class CustomConsoleWriter : TextWriter
    {
        private ConsoleManager _consoleManager;

        public CustomConsoleWriter(ConsoleManager consoleManager)
        {
            _consoleManager = consoleManager;
        }

        public override void WriteLine(string value)
        {
            _consoleManager.AddMessage(value);
        }

        public override Encoding Encoding => Encoding.UTF8;
    }
}