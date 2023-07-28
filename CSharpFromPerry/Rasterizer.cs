using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Microsoft.Xna.Framework.Input;

namespace ComputerGraphicsFromScratch;

internal sealed class Rasterizer
{
	private const int NumTextures = 2;
	private int TextureIndex;
	private readonly Texture2D[] Textures;
	private readonly uint[] TextureData;

	private readonly int CanvasW;
	private readonly int CanvasH;
	private readonly float ViewportW;
	private readonly float ViewportH;
	private readonly float ViewportZ = 1.0f;

	private readonly Color BackgroundColor = Color.Black;
	private readonly Model CubeModel;

	private readonly Camera Camera = new();
	private readonly List<ModelInstance> ModelInstances;

	// For debugging
	private bool MoveModeCamera = true;
	private KeyboardState LastKeyboardState;

	public Rasterizer(GraphicsDevice graphicsDevice, int w, int h)
	{
		CanvasW = w;
		CanvasH = h;

		ViewportW = (float)w / h;
		ViewportH = 1.0f;

		// Set up the textures.
		Textures = new Texture2D[NumTextures];
		for (var i = 0; i < NumTextures; i++)
		{
			Textures[i] = new Texture2D(graphicsDevice, w, h, false, SurfaceFormat.Color);
		}

		TextureData = new uint[CanvasW * CanvasH];

		CubeModel = new Model(
			new Vector3[] {
				new( 1,  1,  1),
				new(-1,  1,  1),
				new(-1, -1,  1),
				new( 1, -1,  1),
				new( 1,  1, -1),
				new(-1,  1, -1),
				new(-1, -1, -1),
				new( 1, -1, -1),
			},
			new Triangle[] {
				new(0, 1, 2, Color.Red),
				new(0, 2, 3, Color.Red),
				new(4, 0, 3, Color.Green),
				new(4, 3, 7, Color.Green),
				new(5, 4, 7, Color.Blue),
				new(5, 7, 6, Color.Blue),
				new(1, 5, 6, Color.Yellow),
				new(1, 6, 2, Color.Yellow),
				new(4, 5, 1, Color.Purple),
				new(4, 1, 0, Color.Purple),
				new(2, 6, 7, Color.Cyan),
				new(2, 7, 3, Color.Cyan),
			}
		);

		Camera.Position = new(-3, 1, 2);
		Camera.YawDegrees = 30;

		ModelInstances = new List<ModelInstance> {
			//new(CubeModel, new Transform(new Vector3(0, 0, 10), Quaternion.Identity, Vector3.One)),
			new(CubeModel, new Transform(new Vector3(-1.5f, 0, 7), Quaternion.Identity, Vector3.One * 0.75f)),
			new(CubeModel, new Transform(new Vector3(1.25f, 2.5f, 7.5f), Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), 195), Vector3.One)),
		};
	}

	float GetAxis(GameTime time, Keys negative, Keys positive) {
		float total = 0;
		var ks = Keyboard.GetState();
		if (ks.IsKeyDown(negative)) {
			total -= 1;
		}
		if (ks.IsKeyDown(positive)) {
			total += 1;
		}
		return (float)(time.ElapsedGameTime.TotalSeconds * total);
	}

	public void Update(GameTime time)
	{
		var kb = Keyboard.GetState();
		if (kb.IsKeyDown(Keys.Tab) && LastKeyboardState.IsKeyUp(Keys.Tab))
		{
			// Toggle mode
			MoveModeCamera = !MoveModeCamera;
		}
		LastKeyboardState = kb;

		// Camera wsadqe and rotation
		if (MoveModeCamera)
		{
			Camera.Pitch += GetAxis(time, Keys.Up, Keys.Down);
			Camera.Yaw += GetAxis(time, Keys.Left, Keys.Right);
			Camera.Position.X += GetAxis(time, Keys.A, Keys.D);
			Camera.Position.Z += GetAxis(time, Keys.S, Keys.W);
			Camera.Position.Y += GetAxis(time, Keys.E, Keys.Q);
		} else {
			if (ModelInstances.Count > 0)
			{
				var transform = ModelInstances[0].Transform;

				transform.Scale.X += GetAxis(time, Keys.OemMinus, Keys.OemPlus);
				transform.Scale.Y += GetAxis(time, Keys.OemMinus, Keys.OemPlus);
				transform.Scale.Z += GetAxis(time, Keys.OemMinus, Keys.OemPlus);

				transform.Rotation *= Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), GetAxis(time, Keys.Up, Keys.Down));
				transform.Rotation *= Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), GetAxis(time, Keys.Left, Keys.Right));
				transform.Translation.X += GetAxis(time, Keys.A, Keys.D);
				transform.Translation.Z += GetAxis(time, Keys.S, Keys.W);
				transform.Translation.Y += GetAxis(time, Keys.E, Keys.Q);
				
				ModelInstances[0].Transform = transform;
			}
		}

		Clear(BackgroundColor);

		// DrawLine(new Point(-200, -100), new Point(240, 120), Color.White);
		// DrawLine(new Point(-50, -200), new Point(60, 240), Color.White);

		// Point p0 = new(-200, -250);
		// Point p1 = new(200, 50);
		// Point p2 = new(20, 250);

		// DrawFilledTriangle(p0, p1, p2, Color.Green);
		// DrawWireframeTriangle(p0, p1, p2, Color.White);

		// DrawShadedTriangle(new Vertex(p0, 0), new Vertex(p1, 0), new Vertex(p2, 1), Color.Green);

		var CameraMatrix = Camera.GetView();

		for (int i = 0; i < ModelInstances.Count; ++i) {
			var instance = ModelInstances[i];
			var matrix = instance.Transform.AsMatrix() * CameraMatrix;
			RenderModel(instance.Model, ref matrix);
		}
		
		var texture = Textures[TextureIndex];
		texture.SetData(TextureData);
	}

	public void DrawShadedTriangle(Vertex v0, Vertex v1, Vertex v2, Color color) {
		// Sort so y0 <= y1 <= y2
		if (v1.Y < v0.Y) {
			(v0, v1) = (v1, v0);
		}
		if (v2.Y < v0.Y) {
			(v0, v2) = (v2, v0);
		}
		if (v2.Y < v1.Y) {
			(v1, v2) = (v2, v1);
		}

		// Build lists of x and h coordinates of each edge
		var x01 = Interpolate(v0.Y, v0.X, v1.Y, v1.X);
		var h01 = Interpolate(v0.Y, v0.H, v1.Y, v1.H);

		var x12 = Interpolate(v1.Y, v1.X, v2.Y, v2.X);
		var h12 = Interpolate(v1.Y, v1.H, v2.Y, v2.H);

		var x02 = Interpolate(v0.Y, v0.X, v2.Y, v2.X);
		var h02 = Interpolate(v0.Y, v0.H, v2.Y, v2.H);

		// Concatenate the short sides
		var x012 = JoinInterpolated(x01, x12);
		var h012 = JoinInterpolated(h01, h12);

		// Mark the left and right sides
		int[] xLeft, xRight;
		float[] hLeft, hRight;
		var midIdx = (int)(x012.Length / 2.0);
		if (x02[midIdx] < x012[midIdx]) {
			xLeft = x02;
			hLeft = h02;
			xRight = x012;
			hRight = h012;
		} else {
			xLeft = x012;
			hLeft = h012;
			xRight = x02;
			hRight = h02;
		}

		// Draw the horizontal segments
		for (int y = v0.Y; y <= v2.Y; ++y) {
			var idx = y - v0.Y;
			var xL = xLeft[idx];
			var xR = xRight[idx];
			var hSegment = Interpolate(xL, hLeft[idx], xR, hRight[idx]);

			for (int x = xL; x <= xR; ++x) {
				var h = hSegment[x - xL];
				var shadedColor = new Color((byte)(h * color.R), (byte)(h * color.G), (byte)(h * color.B));
				PutPixel(x, y, shadedColor);
			}
		}
	}

	public void RenderModel(Model model, ref Matrix transform) {
		var vertices = model.Vertices;
		Point[] projectedVertices = new Point[vertices.Length];
		for (int i = 0; i < vertices.Length; ++i) {
			var transformed = Vector3.Transform(vertices[i], transform);
			projectedVertices[i] = ProjectVertex(transformed);
		}

		var triangles = model.Triangles;
		for (int i = 0; i < triangles.Length; ++i)
		{
			RenderTriangle(triangles[i], projectedVertices);
		}
	}

	public void RenderTriangle(Triangle triangle, Point[] projectedVertices) {
		DrawWireframeTriangle(
			projectedVertices[triangle.I0],
			projectedVertices[triangle.I1],
			projectedVertices[triangle.I2],
			triangle.Color);
	}

	public void DrawFilledTriangle(Point p0, Point p1, Point p2, Color color) {
		// Sort so y0 <= y1 <= y2
		if (p1.Y < p0.Y) {
			(p0, p1) = (p1, p0);
		}
		if (p2.Y < p0.Y) {
			(p0, p2) = (p2, p0);
		}
		if (p2.Y < p1.Y) {
			(p1, p2) = (p2, p1);
		}

		// Build lists of x coordinates of each edge
		var x01 = Interpolate(p0.Y, p0.X, p1.Y, p1.X);
		var x12 = Interpolate(p1.Y, p1.X, p2.Y, p2.X);
		var x02 = Interpolate(p0.Y, p0.X, p2.Y, p2.X);

		// Concatenate the short sides
		var x012 = JoinInterpolated(x01, x12);

		// Mark the left and right sides
		int[] xLeft, xRight;
		var midIdx = (int)(x012.Length / 2.0);
		if (x02[midIdx] < x012[midIdx]) {
			xLeft = x02;
			xRight = x012;
		} else {
			xLeft = x012;
			xRight = x02;
		}

		// Draw the horizontal segments
		for (int y = p0.Y; y <= p2.Y; ++y) {
			for (int x = xLeft[y - p0.Y]; x <= xRight[y - p0.Y]; ++x) {
				PutPixel(x, y, color);
			}
		}
	}

	public void DrawWireframeTriangle(Point p0, Point p1, Point p2, Color color) {
		DrawLine(p0, p1, color);
		DrawLine(p1, p2, color);
		DrawLine(p2, p0, color);
	}

	public void DrawLine(Point p0, Point p1, Color color) {
		if (Math.Abs(p1.X - p0.X) > Math.Abs(p1.Y - p0.Y)) {
			// More horizontal
			if (p0.X > p1.X) {
				(p0, p1) = (p1, p0);
			}
			var yVals = Interpolate(p0.X, p0.Y, p1.X, p1.Y);
			for (int x = p0.X; x <= p1.X; ++x) {
				PutPixel(x, yVals[x - p0.X], color);
			}
		
		} else {
			// More vertical
			if (p0.Y > p1.Y) {
				(p0, p1) = (p1, p0);
			}
			var xVals = Interpolate(p0.Y, p0.X, p1.Y, p1.X);
			for (int y = p0.Y; y <= p1.Y; ++y) {
				PutPixel(xVals[y - p0.Y], y, color);
			}
		}
	}

	public Point ViewportToCanvas(Point point) {
		return ViewportToCanvas(point.X, point.Y);
	}

	public Point ViewportToCanvas(float x, float y) {
		return new Point(
			(int)(x * CanvasW / ViewportW),
			(int)(y * CanvasH / ViewportH));
	}


	public Point ProjectVertex(Vector3 v) {
		return ViewportToCanvas(v.X * ViewportZ / v.Z, v.Y * ViewportZ / v.Z);
	}

	private static T[] JoinInterpolated<T>(T[] a, T[] b) {
		var result = new T[a.Length + b.Length - 1];
		Array.ConstrainedCopy(a, 0, result, 0, a.Length - 1);
		Array.ConstrainedCopy(b, 0, result, a.Length - 1, b.Length);
		return result;
	}

	private static int[] Interpolate(int i0, int d0, int i1, int d1) {
		if (i0 == i1) {
			return new int[]{ d0 };
		}

		int count = i1 - i0 + 1;
		var values = new int[count];
		float a = (d1 - d0) / (float)(i1 - i0);
		float d = d0;

		for (int i = 0; i < count; i++) {
			values[i] = (int)d;
			d += a;
		}

		return values;
	}
	private static float[] Interpolate(int i0, float d0, int i1, float d1) {
		if (i0 == i1) {
			return new float[]{ d0 };
		}

		int count = i1 - i0 + 1;
		var values = new float[count];
		float a = (d1 - d0) / (i1 - i0);
		float d = d0;

		for (int i = 0; i < count; i++) {
			values[i] = (float)d;
			d += a;
		}

		return values;
	}

	private void PutPixel(int canvasX, int canvasY, Color color)
	{
		var x = canvasX + (CanvasW >> 1);
		var y = (CanvasH >> 1) - canvasY - 1;
		var idx = y * CanvasW + x;
		if (idx < 0 || idx >= TextureData.Length) {
			return;
		}
		TextureData[idx] = ToRgba(color);
	}

	private void Clear(Color color) {
		Array.Fill(TextureData, ToRgba(color));
	}
	private static uint ToRgba(Color color)
	{
		return color.R
		       | ((uint)color.G << 8)
		       | ((uint)color.B << 16)
		       | ((uint)color.A << 24);
	}

	public void Draw(SpriteBatch spriteBatch, SpriteFont monospaceFont)
	{
		spriteBatch.Draw(Textures[TextureIndex], new Vector2(0, 0), null, Color.White);
		TextureIndex = (TextureIndex + 1) % NumTextures;

		// Camera
		if (MoveModeCamera)
		{
			spriteBatch.DrawString(monospaceFont, $"Camera at:  {Camera.Position}\nCamera rot: X={Camera.PitchDegrees}, Y={Camera.YawDegrees}", Vector2.Zero, Color.White);
		}
		else
		{
			// First model
			if (ModelInstances.Count > 0)
			{
				var t = ModelInstances[0].Transform;
				spriteBatch.DrawString(
					monospaceFont,
					$"P: {t.Translation}\nS: {t.Scale}\nR: {t.Rotation}", Vector2.Zero, Color.White);
			}
		}
	}
}
