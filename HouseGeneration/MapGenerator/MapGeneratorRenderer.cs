using System;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Shared.ProceduralGeneration;
using Shared.ProceduralGeneration.Util;
using System.Linq;
using System.Collections.Generic;

namespace HouseGeneration.MapGeneratorRenderer;

public class MapGeneratorRenderer : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private RenderTarget2D _mapSprite;
    private bool _needsRedraw = true;
    private Vector2 _spritePosition;
    private float _spriteScale = 1f;

    private RenderTarget2D _renderTarget;
    private bool _needsRerender = true;
    private Vector2 _lastCameraPosition;
    private Point _lastWindowSize;

    private const int ChunkSize = 1024; // or 2048, depending on your hardware limits
    private Dictionary<Point, RenderTarget2D> _mapChunks;

    public MapGeneratorRenderer()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.ClientSizeChanged += OnWindowSizeChanged;

        // Remove these lines from here
        // _graphics.HardwareModeSwitch = false;
        // _graphics.IsFullScreen = false;
        // _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
        // _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
        // Window.IsBorderless = true;
    }

    private void OnWindowSizeChanged(object sender, EventArgs e)
    {
        if (GraphicsDevice.Viewport.Width != _lastWindowSize.X || GraphicsDevice.Viewport.Height != _lastWindowSize.Y)
        {
            _renderTarget?.Dispose();
            _renderTarget = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            _lastWindowSize = new Point(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            _needsRerender = true;
        }
    }

    protected override void Initialize()
    {
        // TODO: Add your initialization logic here
        Window.AllowUserResizing = true;
        
        base.Initialize();

        // Move the window setup here
        SetWindowOnSecondMonitor();
    }

    private void SetWindowOnSecondMonitor()
    {
        // Get all screen bounds
        var screens = System.Windows.Forms.Screen.AllScreens;

        // Debug information
        Console.WriteLine($"Number of screens: {screens.Length}");
        for (int i = 0; i < screens.Length; i++)
        {
            Console.WriteLine($"Screen {i}: Bounds = {screens[i].Bounds}, Primary = {screens[i].Primary}");
        }

        // Find the first non-primary screen
        var secondScreen = screens.FirstOrDefault(s => !s.Primary);

        if (secondScreen != null)
        {
            Console.WriteLine($"Setting window to second screen: {secondScreen.Bounds}");

            // Set window properties
            Window.IsBorderless = true;
            _graphics.PreferredBackBufferWidth = secondScreen.Bounds.Width;
            _graphics.PreferredBackBufferHeight = secondScreen.Bounds.Height;
            _graphics.ApplyChanges();

            // Position the window on the second screen
            Window.Position = new Point(secondScreen.Bounds.X, secondScreen.Bounds.Y);
        }
        else
        {
            Console.WriteLine("No second screen found. Using primary screen.");
        }
    }

    int IsquareSize;
    int IwallMargin;
    int IwallWidth;
    
    int squareSize;
    int wallMargin;
    int wallWidth;
    
    Map map;
    int seed = 1;
    protected override void LoadContent() {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        IsquareSize = 1;
        IwallWidth = 8;
        IwallMargin = IwallWidth + 1;
        
        squareSize = IsquareSize;
        wallWidth = IwallWidth;
        wallMargin = IwallMargin;
        
        
        map = new Map(3000, 3000); // huge 6gb of ram
        // map = new Map(1000, 1000); // nice, 1gb
        // map = new Map(100, 100); // ok, 0.2gb

        Thread mapGeneratorThread = new Thread(() => {
            map.Generate(seed);
        });
        mapGeneratorThread.Start();

        _mapChunks = new Dictionary<Point, RenderTarget2D>();
        
        // Remove the single _mapSprite creation
        // _mapSprite = new RenderTarget2D(GraphicsDevice, map.x * IsquareSize, map.y * IsquareSize);

        // Instead, create chunks
        for (int x = 0; x < map.x * IsquareSize; x += ChunkSize)
        {
            for (int y = 0; y < map.y * IsquareSize; y += ChunkSize)
            {
                int width = Math.Min(ChunkSize, map.x * IsquareSize - x);
                int height = Math.Min(ChunkSize, map.y * IsquareSize - y);
                _mapChunks[new Point(x, y)] = new RenderTarget2D(GraphicsDevice, width, height);
            }
        }

        _renderTarget = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
        _lastCameraPosition = new Vector2(cameraX, cameraY);
        _lastWindowSize = new Point(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

        // TODO: use this.Content to load your game content here
    }
    
    Tile selectedTile;
    private bool pressR;
    private bool pressT;
    public bool pressK;
    private int cameraX;
    private int cameraY;
    private float zoom = 1f;
    private float start_zoom = 1f;
    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        Thread mapGeneratorThread = null;
        if (Keyboard.GetState().IsKeyDown(Keys.R)) {
            if (!pressR) {
                if (mapGeneratorThread == null || !mapGeneratorThread.IsAlive) {
                    mapGeneratorThread = new Thread(() => { map.Generate(seed); });
                    mapGeneratorThread.Start();
                }
            }
            pressR = true;
        } else
            pressR = false;
        
        if (Keyboard.GetState().IsKeyDown(Keys.T)) {
            if (!pressT) {
                if (mapGeneratorThread == null || !mapGeneratorThread.IsAlive) {
                    seed++;
                    mapGeneratorThread = new Thread(() => { map.Generate(seed); });
                    mapGeneratorThread.Start();
                }
            }
            pressT = true;
        } else
            pressT = false;

        if (Keyboard.GetState().IsKeyDown(Keys.K)) {
            if (!pressK) {
                global::HouseGenerator.HouseBuilder.Locked = false;
            }
            pressK = true;
        } else
            pressK = false;
        
        if (Keyboard.GetState().IsKeyDown(Keys.Space)) {
            if (mapGeneratorThread == null || !mapGeneratorThread.IsAlive) {
                seed++;
                mapGeneratorThread = new Thread(() => { map.Generate(seed); });
                mapGeneratorThread.Start();
            }
        }

        if (Keyboard.GetState().IsKeyDown(Keys.LeftControl) && Keyboard.GetState().IsKeyDown(Keys.C))
        {
            CopySpriteToPng();    
        }


        float speed = 5;
        if (Keyboard.GetState().IsKeyDown(Keys.LeftShift)) {
            speed *= 5;
        } else if (Keyboard.GetState().IsKeyDown(Keys.LeftControl)) {
            speed /= 5;
        }
        

        zoom = Mouse.GetState().ScrollWheelValue / 10000f + start_zoom;

        
        squareSize = (int)(IsquareSize * zoom);
        wallWidth = (int)(IwallWidth * zoom);
        wallMargin = (int)(IwallMargin * zoom);
        
        
        if (Keyboard.GetState().IsKeyDown(Keys.W)) {
            cameraY += (int)(1 * speed);
        }
        if (Keyboard.GetState().IsKeyDown(Keys.S)) {
            cameraY -= (int)(1 * speed);
        }
        if (Keyboard.GetState().IsKeyDown(Keys.A)) {
            cameraX += (int)(1 * speed);
        }
        if (Keyboard.GetState().IsKeyDown(Keys.D)) {
            cameraX -= (int)(1 * speed);
        }
        
        
        MouseState mouseState = Mouse.GetState();
        if (mouseState.LeftButton == ButtonState.Pressed)
        {
            // Get the position of the mouse
            Point mousePosition = mouseState.Position;
            mousePosition.X -= cameraX;
            mousePosition.Y -= cameraY;
            
            // Prevent division by zero
            if (squareSize > 0)
            {
                int tileX = mousePosition.X / squareSize;
                int tileY = mousePosition.Y / squareSize;
                
                if (tileX >= 0 && tileX < map.x && tileY >= 0 && tileY < map.y)
                {
                    bool isUp = mousePosition.Y % squareSize < wallMargin;
                    bool isDown = mousePosition.Y % squareSize > squareSize - wallMargin;
                    bool isLeft = mousePosition.X % squareSize < wallMargin;
                    bool isRight = mousePosition.X % squareSize > squareSize - wallMargin;

                    if (isUp) {
                        map.Paint(new Shared.ProceduralGeneration.Util.Texture("", System.Drawing.Color.Red), tileX, tileY,  Side.Top);
                        System.Console.WriteLine("Clicked on wall " + (tileX * 2 + 1) + ";" + (tileY * 2 + 2));
                    }
                    else if (isDown) {
                        map.Paint(new Shared.ProceduralGeneration.Util.Texture("", System.Drawing.Color.Red), tileX, tileY,  Side.Bottom);
                        System.Console.WriteLine("Clicked on wall " + (tileX * 2 + 1) + ";" + (tileY * 2));
                    }
                    else if (isLeft) {
                        map.Paint(new Shared.ProceduralGeneration.Util.Texture("", System.Drawing.Color.Red), tileX, tileY,  Side.Left);
                        System.Console.WriteLine("Clicked on wall " + (tileX * 2) + ";" + (tileY * 2 + 1));
                    }
                    else if (isRight) {
                        map.Paint(new Shared.ProceduralGeneration.Util.Texture("", System.Drawing.Color.Red), tileX, tileY,  Side.Bottom);
                        System.Console.WriteLine("Clicked on wall " + (tileX * 2 + 2) + ";" + (tileY * 2));
                    }
                    else {
                        // map.Paint(System.Drawing.Color.Aqua, tileX, tileY);
                        System.Console.WriteLine("Clicked on tile " + tileX + ";" + tileY);
                    }
                }
            }
        }

        if (map.MapChanged || 
            _lastCameraPosition.X != cameraX || 
            _lastCameraPosition.Y != cameraY ||
            GraphicsDevice.Viewport.Width != _lastWindowSize.X ||
            GraphicsDevice.Viewport.Height != _lastWindowSize.Y)
        {
            _needsRerender = true;
            _lastCameraPosition = new Vector2(cameraX, cameraY);
            _lastWindowSize = new Point(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
        }

        if (map.MapChanged)
        {
            _needsRedraw = true;
            map.MapChanged = false;
        }

        _spritePosition = new Vector2(cameraX, cameraY);
        _spriteScale = zoom;

        base.Update(gameTime);
    }

    public Color fromSysColor(System.Drawing.Color color) {
        return new Color(color.R, color.G, color.B, color.A);
    }

    protected override void Draw(GameTime gameTime)
    {
        if (_needsRedraw)
        {
            foreach (var chunk in _mapChunks)
            {
                GraphicsDevice.SetRenderTarget(chunk.Value);
                GraphicsDevice.Clear(Color.CornflowerBlue);

                _spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp);

                Texture2D singlePixelTexture = new Texture2D(GraphicsDevice, 1, 1);
                singlePixelTexture.SetData(new[] { Color.White });

                int startX = chunk.Key.X / IsquareSize;
                int startY = chunk.Key.Y / IsquareSize;
                int endX = Math.Min(startX + ChunkSize / IsquareSize, map.x);
                int endY = Math.Min(startY + ChunkSize / IsquareSize, map.y);

                for (int x = startX; x < endX; x++)
                {
                    for (int y = startY; y < endY; y++)
                    {
                        _spriteBatch.Draw(
                            singlePixelTexture,
                            new Rectangle((x - startX) * IsquareSize, (y - startY) * IsquareSize, IsquareSize, IsquareSize),
                            fromSysColor(map.TextureTypes[map.GetTile(x, y).TextureIndex].Color));
                    }
                }

                _spriteBatch.End();
            }

            GraphicsDevice.SetRenderTarget(null);
            _needsRedraw = false;
        }

        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp, null, null, null, Matrix.CreateScale(zoom));
        
        Vector2 screenCenter = new Vector2(GraphicsDevice.Viewport.Width / 2f, GraphicsDevice.Viewport.Height / 2f);
        Vector2 zoomedSpritePosition = screenCenter - (screenCenter - _spritePosition) * zoom;

        foreach (var chunk in _mapChunks)
        {
            Vector2 chunkPosition = new Vector2(chunk.Key.X, chunk.Key.Y) + zoomedSpritePosition;
            _spriteBatch.Draw(chunk.Value, chunkPosition, null, Color.White);
        }
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        foreach (var chunk in _mapChunks.Values)
        {
            chunk.Dispose();
        }
        _mapChunks.Clear();
        // Remove: _mapSprite?.Dispose();
        _renderTarget?.Dispose();
        base.UnloadContent();
    }

    private void CopySpriteToPng()
    {
        // Create a new texture with dimensions equal to the map size
        Texture2D simplifiedTexture = new Texture2D(GraphicsDevice, map.x, map.y);
        Color[] colorData = new Color[map.x * map.y];

        // Fill the color data array with tile colors
        for (int y = 0; y < map.y; y++)
        {
            for (int x = 0; x < map.x; x++)
            {
                Tile tile = map.GetTile(x, y);
                // Only copy the main texture, ignoring walls
                colorData[y * map.x + x] = fromSysColor(map.TextureTypes[tile.TextureIndex].Color);
            }
        }

        // Set the color data to the simplified texture
        simplifiedTexture.SetData(colorData);

        // Save the texture to a memory stream as PNG
        using (MemoryStream stream = new MemoryStream())
        {
            simplifiedTexture.SaveAsPng(stream, map.x, map.y);
            stream.Seek(0, SeekOrigin.Begin);

            // Copy the PNG data to the clipboard
            Thread thread = new Thread(() => System.Windows.Forms.Clipboard.SetImage(System.Drawing.Image.FromStream(stream)));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        // Dispose of the temporary texture
        simplifiedTexture.Dispose();
    }
}