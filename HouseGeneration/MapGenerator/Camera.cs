using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HouseGeneration.MapGeneratorRenderer
{
    public class Camera
    {
        public Vector2 Position { get; set; }
        public float Zoom { get; private set; }
        public float Rotation { get; private set; }

        private int _viewportWidth;
        private int _viewportHeight;

        public Camera(Viewport viewport)
        {
            _viewportWidth = viewport.Width;
            _viewportHeight = viewport.Height;
            Position = Vector2.Zero;
            Zoom = 1f;
            Rotation = 0f;
        }

        public void Move(Vector2 delta)
        {
            Position += delta * 1/Zoom;
        }

        public void AdjustZoom(float zoomDelta)
        {
            float prevZoom = Zoom;
            Zoom = MathHelper.Clamp(Zoom + zoomDelta, 0.1f, 10f);

            // Adjust position to keep the center point fixed
            Vector2 screenCenter = new Vector2(_viewportWidth * 0.5f, _viewportHeight * 0.5f);
            Vector2 prevWorldCenter = ScreenToWorld(screenCenter);
            Vector2 newWorldCenter = Vector2.Transform(screenCenter - new Vector2(_viewportWidth * 0.5f, _viewportHeight * 0.5f),
                Matrix.Invert(
                    Matrix.CreateTranslation(new Vector3(_viewportWidth * 0.5f, _viewportHeight * 0.5f, 0)) *
                    Matrix.CreateScale(Zoom) *
                    Matrix.CreateRotationZ(-Rotation))) + Position;

            Position += prevWorldCenter - newWorldCenter;
        }

        public void SetZoom(float zoom)
        {
            float prevZoom = Zoom;
            Zoom = MathHelper.Clamp(zoom, 0.1f, 10f);

            // Adjust position to keep the center point fixed
            Vector2 screenCenter = new Vector2(_viewportWidth * 0.5f, _viewportHeight * 0.5f);
            Vector2 prevWorldCenter = ScreenToWorld(screenCenter);
            Vector2 newWorldCenter = Vector2.Transform(screenCenter - new Vector2(_viewportWidth * 0.5f, _viewportHeight * 0.5f),
                Matrix.Invert(
                    Matrix.CreateTranslation(new Vector3(_viewportWidth * 0.5f, _viewportHeight * 0.5f, 0)) *
                    Matrix.CreateScale(Zoom) *
                    Matrix.CreateRotationZ(-Rotation))) + Position;

            Position += prevWorldCenter - newWorldCenter;
        }

        public void Rotate(float rotationDelta)
        {
            Rotation += rotationDelta;
        }

        public void UpdateViewport(Viewport viewport)
        {
            _viewportWidth = viewport.Width;
            _viewportHeight = viewport.Height;
        }

        public Vector2 WorldToScreen(Vector2 worldPosition)
        {
            return Vector2.Transform(worldPosition - Position,
                Matrix.CreateRotationZ(-Rotation) *
                Matrix.CreateScale(Zoom) *
                Matrix.CreateTranslation(new Vector3(_viewportWidth * 0.5f, _viewportHeight * 0.5f, 0)));
        }

        public Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            return Vector2.Transform(screenPosition - new Vector2(_viewportWidth * 0.5f, _viewportHeight * 0.5f),
                Matrix.Invert(
                    Matrix.CreateTranslation(new Vector3(_viewportWidth * 0.5f, _viewportHeight * 0.5f, 0)) *
                    Matrix.CreateScale(Zoom) *
                    Matrix.CreateRotationZ(Rotation))) + Position;
        }

        public Matrix GetViewMatrix()
        {
            return Matrix.CreateTranslation(new Vector3(-Position, 0)) *
                   Matrix.CreateRotationZ(-Rotation) *
                   Matrix.CreateScale(new Vector3(Zoom, Zoom, 1)) *
                   Matrix.CreateTranslation(new Vector3(_viewportWidth * 0.5f, _viewportHeight * 0.5f, 0));
        }

        public void SetRotation(float rotation)
        {
            Rotation = rotation;
        }
    }
}
