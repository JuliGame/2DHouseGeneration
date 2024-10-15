using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ImGuiNET;
using System;
using MonoGame.ImGuiNet;
using Shared.ProceduralGeneration;
using System.Windows.Forms;
using System.Threading;
using System.ComponentModel.Design;
using HouseGeneration.UI;

namespace HouseGeneration.MapGeneratorRenderer
{
    public class MapGeneratorRenderer : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private ImGuiRenderer _imGuiRenderer;
        
        private Camera _camera;
        private InputHandler _inputHandler;
        private MapRenderer _mapRenderer;
        private ConsoleManager _consoleManager;
        private TaskPerformanceMenu _taskPerformanceMenu;

        private Map _map;
        private bool _isGeneratingMap = false;
        private Random _random = new Random();

        public MapGeneratorRenderer()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _imGuiRenderer = new ImGuiRenderer(this);

            // Get the bounds of the second screen
            System.Drawing.Rectangle bounds = Screen.AllScreens[1].Bounds;
            
            // Set the window to match the second screen's resolution
            _graphics.PreferredBackBufferWidth = bounds.Width;
            _graphics.PreferredBackBufferHeight = bounds.Height;

            // Set the window position to the second screen
            Window.Position = new Point(bounds.X, bounds.Y);

            // Enable full screen mode
            _graphics.IsFullScreen = false;

            _graphics.ApplyChanges();

            _camera = new Camera(GraphicsDevice.Viewport);
            _inputHandler = new InputHandler(this);
            _mapRenderer = new MapRenderer(GraphicsDevice, Content);
            _consoleManager = new ConsoleManager();
            _taskPerformanceMenu = new TaskPerformanceMenu();

            Console.SetOut(new CustomConsoleWriter(_consoleManager));
            Console.WriteLine("Console initialized");

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _imGuiRenderer.RebuildFontAtlas();
        }


        protected override void Update(GameTime gameTime)
        {
            _inputHandler.HandleInput(gameTime);
            _camera.UpdateViewport(GraphicsDevice.Viewport);
            


            _mapRenderer.Update(_map, _camera);

            

            if (_isGeneratingMap)
            {
                GenerateMap();
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, _camera.GetViewMatrix());
            _mapRenderer.Draw(_spriteBatch, _camera);
            _spriteBatch.End();

            _imGuiRenderer.BeginLayout(gameTime);

            _consoleManager.Draw();
            DrawUI();
            _taskPerformanceMenu.Draw();

            _imGuiRenderer.EndLayout();

            base.Draw(gameTime);
        }

        private void DrawUI()
        {
            ImGuiWindowFlags window_flags = ImGuiWindowFlags.None;
            
            ImGui.Begin("Map Generator", window_flags);
            if (ImGui.Button("Generate New Map"))
            {
                _isGeneratingMap = true;
            }
            ImGui.SameLine();
            if (ImGui.Button("Force re-render"))
            {
                _map.MapChanged = true;
            }
            ImGui.SameLine();
            if (ImGui.Button("\uf2ed")) // Unicode for trash can icon
            {
                _map.MapChanged = true;
                _mapRenderer.ClearChunks();
            }
            
            DrawCameraGizmo();
            DrawCameraPositionGizmo();

            ImGui.End();
        }

        private void DrawCameraGizmo()
        {
            ImGui.Text("Camera Controls");
            
            // Camera position
            System.Numerics.Vector2 cameraPos = new System.Numerics.Vector2(_camera.Position.X, _camera.Position.Y);
            if (ImGui.DragFloat2("Position", ref cameraPos, 1f, float.MinValue, float.MaxValue, "%.1f"))
            {
                _camera.Move(cameraPos - _camera.Position);
            }

            // Camera zoom
            float zoom = _camera.Zoom;
            if (ImGui.SliderFloat("Zoom", ref zoom, 0.1f, 10f, "%.2f"))
            {
                _camera.SetZoom(zoom);
            }

            // Camera rotation
            float rotation = _camera.Rotation;
            if (ImGui.SliderAngle("Rotation", ref rotation, -180f, 180f))
            {
                _camera.Rotate(rotation - _camera.Rotation);
            }

            // Reset camera button
            if (ImGui.Button("Reset Camera"))
            {
                _camera.Position = Vector2.Zero;
                _camera.SetZoom(1f);
                _camera.Rotate(-_camera.Rotation);
            }
        }

        private void DrawCameraPositionGizmo()
        {
            ImGui.Text("Camera Position Gizmo");
            
            Vector2 gizmoCenter = new Vector2(100, 100);
            float gizmoSize = 80;
            float zoomFactor = 0.1f;

            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector2 screenPos = ImGui.GetCursorScreenPos();

            // Draw background circle
            drawList.AddCircleFilled(new System.Numerics.Vector2(screenPos.X + gizmoCenter.X, screenPos.Y + gizmoCenter.Y), gizmoSize, ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.2f, 0.2f, 0.2f, 1f)));

            // Draw camera position
            System.Numerics.Vector2 cameraPos = new System.Numerics.Vector2(_camera.Position.X, _camera.Position.Y) * zoomFactor;
            System.Numerics.Vector2 gizmoPos = new System.Numerics.Vector2(screenPos.X + gizmoCenter.X, screenPos.Y + gizmoCenter.Y) + cameraPos;
            drawList.AddCircleFilled(gizmoPos, 5, ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(1f, 0.5f, 0, 1f)));

            // Draw camera direction
            System.Numerics.Vector2 directionEnd = gizmoPos + new System.Numerics.Vector2(MathF.Cos(_camera.Rotation), MathF.Sin(_camera.Rotation)) * 20;
            drawList.AddLine(gizmoPos, directionEnd, ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0, 1f, 0, 1f)), 2);

            // Draw zoom indicator
            float zoomIndicatorSize = gizmoSize * (_camera.Zoom / 5f); // Adjust the divisor to change the scale
            drawList.AddCircle(new System.Numerics.Vector2(screenPos.X + gizmoCenter.X, screenPos.Y + gizmoCenter.Y), zoomIndicatorSize, ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0, 0.5f, 1f, 1f)));

            ImGui.Dummy(new System.Numerics.Vector2(gizmoSize * 2, gizmoSize * 2)); // Make space for the gizmo
        }

        private int _seed = 0;
        Thread mapGenerationThread = null;
        private void GenerateMap()
        {
            if (mapGenerationThread != null && mapGenerationThread.IsAlive)            
                return;           

            _isGeneratingMap = false;
            mapGenerationThread = new Thread(() =>
            {
                try 
                {
                    _map = new Map(1024 * 3, 1024 * 3); // Example size
                    _map.Generate(_seed, (string taskName, bool end) => {
                        if (end) {
                            _taskPerformanceMenu.EndTask(taskName);
                        } else {
                            _taskPerformanceMenu.StartTask(taskName);
                        }
                    });
                    
                    mapGenerationThread = null;
                    Console.WriteLine("New map generated!");
                }
                catch (Exception e)
                {
                    mapGenerationThread = null;
                    Console.WriteLine("Map generation thread error: " + e.Message);
                }

                _seed++;
            });

            mapGenerationThread.Start();
            Console.WriteLine("Map generation thread started!");
        }

        public void MoveCamera(Vector2 delta)
        {
            _camera.Move(delta);
        }

        public void ZoomCamera(float zoomDelta)
        {
            _camera.AdjustZoom(zoomDelta);
        }
    }
}
