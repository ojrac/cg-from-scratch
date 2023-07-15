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

	private readonly Color BackgroundColor = Color.Black;

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

		DrawLine(new Point(-200, -100), new Point(240, 120), Color.White);
		DrawLine(new Point(-50, -200), new Point(60, 240), Color.White);

		var texture = Textures[TextureIndex];
		texture.SetData(TextureData);
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

	private Vector3 CanvasToViewport(int canvasX, int canvasY)
	{
		return new Vector3(canvasX * ViewportW / CanvasW, canvasY * ViewportH / CanvasH, ViewportZ);
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
