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
using System.IO;

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

        private Keys[] _previousPressedKeys;

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

            _map = new Map(1024 * 1, 1024 * 1); // Example size

            Thread mapGeneratorThread = new Thread(() => {             
                _map.Generate(_seed, (string taskName, bool end) => {
                    if (end) {
                        _taskPerformanceMenu.EndTask(taskName);
                    } else {
                        _taskPerformanceMenu.StartTask(taskName);
                    }
                }, _useCPU);
            });
            mapGeneratorThread.Start();


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

        private bool _useCPU = false;
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
            ImGui.SameLine();
            ImGui.Checkbox("Use CPU", ref _useCPU);

            ImGui.SetNextItemWidth(100);
            
            if (ImGui.InputInt("Seed", ref _seed))
            {
                // Ensure seed is non-negative
                _seed = Math.Max(0, _seed);
            }
            ImGui.SameLine();
            ImGui.Checkbox("Increment seed", ref _incrementSeed);

            ImGui.Spacing();
            
            // Camera position    
            ImGui.Text("Camera Controls");
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
            ImGui.End();
        }


        private int _seed = 0;
        Thread mapGenerationThread = null;
        private bool _incrementSeed;

        private void GenerateMap()
        {
            if (mapGenerationThread != null && mapGenerationThread.IsAlive)            
                return;           

            _isGeneratingMap = false;
            mapGenerationThread = new Thread(() =>
            {
                try 
                {
                    _map = new Map((int) (1024 * 1f), (int) (1024 * 1f)); // Example size
                    _map.Generate(_seed, (string taskName, bool end) => {
                        if (end) {
                            _taskPerformanceMenu.EndTask(taskName);
                        } else {
                            _taskPerformanceMenu.StartTask(taskName);
                        }
                    }, _useCPU);
                    
                    mapGenerationThread = null;
                    Console.WriteLine("New map generated!");
                    if (_incrementSeed) {
                        _seed++;
                    }
                }
                catch (Exception e)
                {
                    mapGenerationThread = null;
                    Console.WriteLine("Map generation thread error: " + e.Message);
                }
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

        public void CopyMapToClipboard()
        {
            Texture2D mapImage = _mapRenderer.CreateFullMapImage();
            if (mapImage != null)
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    mapImage.SaveAsPng(stream, mapImage.Width, mapImage.Height);
                    stream.Seek(0, SeekOrigin.Begin);
                    System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(stream);
                    System.Windows.Forms.Clipboard.SetImage(bitmap);
                }
                Console.WriteLine("Map copied to clipboard as PNG.");
            }
            else
            {
                Console.WriteLine("Failed to copy map to clipboard. No chunks loaded.");
            }
        }
    }
}
