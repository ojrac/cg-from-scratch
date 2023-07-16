using Microsoft.Xna.Framework;

struct Vertex {
    public int X;
    public int Y;
    public float H;

    public Vertex(int x, int y, float h) {
        X = x;
        Y = y;
        H = h;
    }

    public Vertex(Point p, float h) {
        X = p.X;
        Y = p.Y;
        H = h;
    }
}