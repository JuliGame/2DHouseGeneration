using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Shared.ProceduralGeneration;
using Shared.ProceduralGeneration.Util;

namespace HouseGeneration.HouseGenerator;

public class HouseGeneratorRenderer : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    public HouseGeneratorRenderer()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
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
        
        
        // map = new Map(50, 40);
        // map = new Map(20, 20);

        // map = new Map(25, 18);
        map = new Map(17, 12);
        // map = new Map(12, 8);
        Thread mapGeneratorThread = new Thread(() => {
            map.Generate(seed);
        });
        mapGeneratorThread.Start();

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
        

        zoom = Mouse.GetState().ScrollWheelValue / 1000f + 1;

        
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

        base.Update(gameTime);
    }

    public Color fromSysColor(System.Drawing.Color color) {
        return new Color(color.R, color.G, color.B, color.A);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        Texture2D singlePixelTexture = new Texture2D(GraphicsDevice, 1, 1);
        singlePixelTexture.SetData(new[] { Color.White }); // white pixel
        
        _spriteBatch.Begin();
        for (int x = 0; x < map.x; x++)
        {
            for (int y = 0; y < map.y; y++)
            {
                // Here you can set whatever color you like
                _spriteBatch.Draw(
                    singlePixelTexture,
                    new Rectangle(x * squareSize + cameraX, y * squareSize + cameraY, squareSize, squareSize),
                    fromSysColor(map.GetTile(x, y).Texture.Color));
            }
        }

        // Draw grid lines
        for (int ix = 0; ix < map.x*2+1; ix++) {
            for (int iy = 0; iy < map.y*2+1; iy++) {
                
                int x = ix / 2;
                int y = iy / 2;
                
                Wall wall = map.GetWall(ix, iy);
                Color color =  fromSysColor(wall.Texture.Color);
                if (ix % 2 == 1) {
                    if (iy % 2 == 1) {
                        SpriteFont font = Content.Load<SpriteFont>("Arial");
                        // string text = x + ";" + y;
                        
                        string text = map.GetTile(x, y).Text;
                        if (string.IsNullOrEmpty(text))
                            continue;
                        
                        Vector2 textSize = font.MeasureString(text);
                        float scale = 0.7f; // Adjust this value to something that suits your needs.
                        textSize *= scale;
                        Vector2 position = new Vector2(x * squareSize + squareSize / 2f - textSize.X / 2f + cameraX,
                            y * squareSize + squareSize / 2f - textSize.Y / 2f + cameraY);
                        
                        _spriteBatch.DrawString(font, text, position, Color.Black, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
                        continue;
                    }

                    if (wall.isHalf) {
                        if (wall.isTopOrLeft) {
                            _spriteBatch.Draw(singlePixelTexture, new Rectangle(x * squareSize + cameraX, y * squareSize + cameraY, squareSize / 2, (int)
                                (wallWidth * wall.Thickness)), color); 
                        }
                        else {
                            _spriteBatch.Draw(singlePixelTexture, new Rectangle(x * squareSize + cameraX + squareSize / 2, y * squareSize + cameraY, squareSize / 2, (int)
                                (wallWidth * wall.Thickness)), color);
                        }
                    }
                    else {
                        _spriteBatch.Draw(singlePixelTexture, new Rectangle(x * squareSize + cameraX, y * squareSize + cameraY, squareSize, (int)
                            (wallWidth * wall.Thickness)), color);
                    }

                } else {
                    if (iy == map.y*2)
                        continue;
                    
                    if (wall.isHalf) {
                        if (wall.isTopOrLeft) {
                            _spriteBatch.Draw(singlePixelTexture, new Rectangle(x * squareSize + cameraX, y * squareSize + cameraY, (int)
                                (wallWidth * wall.Thickness), squareSize / 2), color); 
                        }
                        else {
                            _spriteBatch.Draw(singlePixelTexture, new Rectangle(x * squareSize + cameraX, y * squareSize + cameraY + squareSize / 2, (int)
                                (wallWidth * wall.Thickness), squareSize / 2), color);
                        }
                    }
                    else {
                        _spriteBatch.Draw(singlePixelTexture,
                            new Rectangle(x * squareSize + cameraX, y * squareSize + cameraY, (int)
                                (wallWidth * wall.Thickness), squareSize),
                            color);
                    }
                }
            }
        }
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}