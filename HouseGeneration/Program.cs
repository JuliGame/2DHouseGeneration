using System;
using System.Windows.Forms;
using HouseGeneration.HouseGenerator;
using HouseGeneration.ItemEditor;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        try
        {
            //using var game = new HouseGeneratorRenderer();
            // using var game = new ItemEditorMain();
            
            using (var game = new HouseGeneratorRenderer())
            {
                game.Run();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"An error occurred: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}