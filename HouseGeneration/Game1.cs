using HouseGeneration.Logic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace HouseGeneration;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    public Game1()
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
    
    int squareSize;
    int wallMargin;
    int wallWidth;
    
    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        squareSize = 35;
        wallWidth = 2;
        wallMargin = wallWidth + 1;
        // TODO: use this.Content to load your game content here
    }
    
    Tile selectedTile;
    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();
        
        if (Keyboard.GetState().IsKeyDown(Keys.R))
            map.Generate();

        // TODO: Add your update logic here
        
        // Detect if clicked
        
        MouseState mouseState = Mouse.GetState();
        if (mouseState.LeftButton == ButtonState.Pressed)
        {
            // Get the position of the mouse
            Point mousePosition = mouseState.Position;
            
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
                map.Paint(Color.Red, tileX, tileY, Side.Top);
                System.Console.WriteLine("Painting top");
            }
            else if (isDown) {
                map.Paint(Color.Red, tileX, tileY, Side.Bottom);
            }
            else if (isLeft) {
                map.Paint(Color.Red, tileX, tileY, Side.Left);
            }
            else if (isRight) {
                map.Paint(Color.Red, tileX, tileY, Side.Right);
            }
            else {
                map.Paint(Color.Aqua, tileX, tileY);
            }
        }

        base.Update(gameTime);
    }

    
    Map map = new Map();
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
                    new Rectangle(x * squareSize, y * squareSize, squareSize, squareSize),
                    map.GetTile(x, y).Color);
            }
        }

        // Draw grid lines
        for (int ix = 0; ix < map.x*2+1; ix++) {
            for (int iy = 0; iy < map.y*2+1; iy++) {
                
                int x = ix / 2;
                int y = iy / 2;
                
                Color color = map.GetWall(ix, iy).Color;
                if (ix % 2 == 1) {
                    if (iy % 2 == 1) {
                        // Draw text saying "wall"
                        SpriteFont font = Content.Load<SpriteFont>("Arial");
                        string text = x + ";" + y;
                        Vector2 textSize = font.MeasureString(text);
                        float scale = 0.7f; // Adjust this value to something that suits your needs.
                        textSize *= scale;
                        Vector2 position = new Vector2(x * squareSize + squareSize / 2f - textSize.X / 2f,
                                                       y * squareSize + squareSize / 2f - textSize.Y / 2f);

                        _spriteBatch.DrawString(font, text, position, Color.Black, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
                        continue;
                    }
                    
                    _spriteBatch.Draw(singlePixelTexture, new Rectangle(x * squareSize, y * squareSize, squareSize, wallWidth), color);
                } else {
                    if (iy == map.y*2)
                        continue;
                    
                    _spriteBatch.Draw(singlePixelTexture, new Rectangle(x * squareSize, y * squareSize, wallWidth, squareSize), color);
                }
            }
        }
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}