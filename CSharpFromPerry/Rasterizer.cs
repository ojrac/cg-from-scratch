using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;

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

	private readonly Color BackgroundColor = Color.White;

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
	}

	public void Update(GameTime _)
	{
		Clear(BackgroundColor);

		// DrawLine(new Point(-200, -100), new Point(240, 120), Color.White);
		// DrawLine(new Point(-50, -200), new Point(60, 240), Color.White);

		// Point p0 = new(-200, -250);
		// Point p1 = new(200, 50);
		// Point p2 = new(20, 250);

		// DrawFilledTriangle(p0, p1, p2, Color.Green);
		// DrawWireframeTriangle(p0, p1, p2, Color.White);

		// DrawShadedTriangle(new Vertex(p0, 0), new Vertex(p1, 0), new Vertex(p2, 1), Color.Green);

		DrawCube();
		
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

	public void DrawCube() {
		// Front
		Vector3 vAf = new(-2, -0.5f, 5);
		Vector3 vBf = new(-2,  0.5f, 5);
		Vector3 vCf = new(-1,  0.5f, 5);
		Vector3 vDf = new(-1, -0.5f, 5);

		// Back
		Vector3 vAb = new(-2, -0.5f, 6);
		Vector3 vBb = new(-2,  0.5f, 6);
		Vector3 vCb = new(-1,  0.5f, 6);
		Vector3 vDb = new(-1, -0.5f, 6);

		// Front face
		DrawLine(ProjectVertex(vAf), ProjectVertex(vBf), Color.Blue);
		DrawLine(ProjectVertex(vBf), ProjectVertex(vCf), Color.Blue);
		DrawLine(ProjectVertex(vCf), ProjectVertex(vDf), Color.Blue);
		DrawLine(ProjectVertex(vDf), ProjectVertex(vAf), Color.Blue);

		// Back face
		DrawLine(ProjectVertex(vAb), ProjectVertex(vBb), Color.Red);
		DrawLine(ProjectVertex(vBb), ProjectVertex(vCb), Color.Red);
		DrawLine(ProjectVertex(vCb), ProjectVertex(vDb), Color.Red);
		DrawLine(ProjectVertex(vDb), ProjectVertex(vAb), Color.Red);

		// Front to back edges
		DrawLine(ProjectVertex(vAf), ProjectVertex(vAb), Color.Green);
		DrawLine(ProjectVertex(vBf), ProjectVertex(vBb), Color.Green);
		DrawLine(ProjectVertex(vCf), ProjectVertex(vCb), Color.Green);
		DrawLine(ProjectVertex(vDf), ProjectVertex(vDb), Color.Green);
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

	public void Draw(SpriteBatch spriteBatch)
	{
		spriteBatch.Draw(Textures[TextureIndex], new Vector2(0, 0), null, Color.White);
		TextureIndex = (TextureIndex + 1) % NumTextures;
	}
}
