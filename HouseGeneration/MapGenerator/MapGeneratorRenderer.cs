using System;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Shared.ProceduralGeneration;
using Shared.ProceduralGeneration.Util;

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

    public MapGeneratorRenderer()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.ClientSizeChanged += OnWindowSizeChanged;
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

        IsquareSize = 30;
        IwallWidth = 8;
        IwallMargin = IwallWidth + 1;
        
        squareSize = IsquareSize;
        wallWidth = IwallWidth;
        wallMargin = IwallMargin;
        
        
        // map = new Map(3000, 3000); // huge 6gb of ram
        map = new Map(1000, 1000); // nice, 1gb

        Thread mapGeneratorThread = new Thread(() => {
            map.Generate(seed);
        });
        mapGeneratorThread.Start();

        _mapSprite = new RenderTarget2D(GraphicsDevice, map.x * IsquareSize, map.y * IsquareSize);
        _spritePosition = Vector2.Zero;

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


        float speed = 5;
        if (Keyboard.GetState().IsKeyDown(Keys.LeftShift)) {
            speed *= 5;
        } else if (Keyboard.GetState().IsKeyDown(Keys.LeftControl)) {
            speed /= 5;
        }
        

        zoom = Mouse.GetState().ScrollWheelValue / 10000f + 1;

        
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
            
            int tileX = mousePosition.X / squareSize;
            int tileY = mousePosition.Y / squareSize;
            
            if (tileX < 0 || tileX >= map.x || tileY < 0 || tileY >= map.y) {
                return;
            }
            
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
            GraphicsDevice.SetRenderTarget(_mapSprite);
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp);

            Texture2D singlePixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            singlePixelTexture.SetData(new[] { Color.White });

            for (int x = 0; x < map.x; x++)
            {
                for (int y = 0; y < map.y; y++)
                {
                    _spriteBatch.Draw(
                        singlePixelTexture,
                        new Rectangle(x * IsquareSize, y * IsquareSize, IsquareSize, IsquareSize),
                        fromSysColor(map.GetTile(x, y).Texture.Color));
                }
            }

            _spriteBatch.End();

            GraphicsDevice.SetRenderTarget(null);
            _needsRedraw = false;
        }

        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp);
        _spriteBatch.Draw(_mapSprite, _spritePosition, null, Color.White, 0f, Vector2.Zero, _spriteScale, SpriteEffects.None, 0f);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        _mapSprite?.Dispose();
        _renderTarget?.Dispose();
        base.UnloadContent();
    }
}