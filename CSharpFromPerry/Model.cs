using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace ComputerGraphicsFromScratch;

class Camera {
    public float Yaw {
        get { return yaw; }
        set {
            yaw = value % (float)(2 * Math.PI);
        }
    }
    public float Pitch {
        get { return pitch; }
        set {
            pitch = value % (float)(2 * Math.PI);
        }
    }

    private float yaw = 0;
    private float pitch = 0;


    public float YawDegrees {
        get { return (float)(yaw * 180.0 / Math.PI); }
        set { Yaw = (float)(Math.PI * value / 180.0); }
    }
    public float PitchDegrees {
        get { return (float)(pitch * 180 / Math.PI); }
        set { Pitch = (float)(Math.PI * value / 180.0); }
    }

    public Vector3 Position;

    public Matrix GetView() {
        return Matrix.Transpose(Matrix.CreateFromYawPitchRoll(Yaw, Pitch, 0)) * Matrix.CreateTranslation(-Position);
    }

}

struct Triangle {
    public int I0 { get; private set; }
    public int I1 { get; private set; }
    public int I2 { get; private set; }
    public Color Color { get; private set; }

    public Triangle(int i0, int i1, int i2, Color color) {
        I0 = i0;
        I1 = i1;
        I2 = i2;
        Color = color;
    }
}

struct Model {
    public Vector3[] Vertices { get; private set; }
    public Triangle[] Triangles { get; private set; }

    public Model(Vector3[] vertices, Triangle[] triangles) {
        Vertices = vertices;
        Triangles = triangles;
    }
}

struct Transform {
    public Vector3 Translation;
    public Quaternion Rotation;
    public Vector3 Scale;

    public Transform(Vector3 translation, Quaternion rotation, Vector3 scale) {
        Translation = translation;
        Rotation = rotation;
        Scale = scale;
    }

    public static Transform FromTranslation(Vector3 translation) {
        return new Transform(translation, Quaternion.Identity, Vector3.One);
    }

    public readonly Matrix AsMatrix() {
        return Matrix.CreateFromQuaternion(Rotation) * Matrix.CreateScale(Scale) * Matrix.CreateTranslation(Translation);
    }
}

class ModelInstance {
    public Model Model;
    public Transform Transform;

    public ModelInstance(Model model, Transform transform) {
        Model = model;
        Transform = transform;
    }
}