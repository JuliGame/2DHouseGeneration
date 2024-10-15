using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace HouseGeneration.MapGeneratorRenderer
{
    public class InputHandler
    {
        private MapGeneratorRenderer _game;
        private MouseState _previousMouseState;

        public InputHandler(MapGeneratorRenderer game)
        {
            _game = game;
        }

        public void HandleInput(GameTime gameTime)
        {
            KeyboardState keyboardState = Keyboard.GetState();
            MouseState mouseState = Mouse.GetState();

            // Camera movement
            Vector2 movement = Vector2.Zero;
            if (keyboardState.IsKeyDown(Keys.W)) movement.Y -= 1;
            if (keyboardState.IsKeyDown(Keys.S)) movement.Y += 1;
            if (keyboardState.IsKeyDown(Keys.A)) movement.X -= 1;
            if (keyboardState.IsKeyDown(Keys.D)) movement.X += 1;

            if (movement != Vector2.Zero)
            {
                movement.Normalize();
                _game.MoveCamera(movement * 5f);
            }

            // Camera zoom
            if (mouseState.ScrollWheelValue != _previousMouseState.ScrollWheelValue)
            {
                float zoomDelta = (mouseState.ScrollWheelValue - _previousMouseState.ScrollWheelValue) / 1000f;
                _game.ZoomCamera(zoomDelta);
            }

            _previousMouseState = mouseState;
        }
    }
}