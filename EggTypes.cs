using System;

namespace Panda3DEggParser
{
    /// <summary>
    /// A class representing all the info held by an EGG file.
    /// </summary>
    /// <value>Property <c>Data</c> holds all of the egg file's data.</value>
    /// <seealso cref="EggGroup"/>
    public class Panda3DEgg
    {
        public string CoordinateSystem = "Z-Up";
        public string Comment;

        public List<EggGroup> Data = new();
    }

    /// <summary>
    /// A generic abstract class for basic group data.
    /// </summary>
    public abstract class EggGroup
    {
        public string Value = string.Empty;
        public string Name = string.Empty;
    }

    public class GenericEggGroup : EggGroup { }

    /// <summary>
    /// A group that holds data about texture references.
    /// </summary>
    public class TextureGroup : EggGroup
    {
        /// <summary>
        /// A string to the path of the texture file, relative to Panda3D installation.
        /// </summary>
        public string Filepath;
        /// <summary>
        /// A collection of scalars, affecting things such as color space.
        /// </summary>
        public List<EggGroup> Scalars = new();

        public TextureGroup(string filepath)
        {
            Filepath = filepath;
        }
    }

    #region VERTEX GROUPS
    /// <summary>
    /// Holds many different <c><seealso cref="Vertex"/></c>es
    /// </summary>
    public class VertexPool : EggGroup
    {
        public List<Vertex> References = new();
    }

    /// <summary>
    /// A class representing a <c>vertex</c> in an egg file.
    /// </summary>
    public class Vertex : EggGroup
    {
        public int Index;
        public float X;
        public float Y;
        public float Z;
        public float W;
        public VertexUV UV;
        public VertexRGBA RGBA;
        public VertexNormal Normal;

        public Vertex(int index, float x, float y, float z)
        {
            Index = index;
            X = x;
            Y = y;
            Z = z;
        }

        public Vertex(string index, string x, string y, string z)
        {
            Index = int.Parse(index);
            X = float.Parse(x);
            Y = float.Parse(y);
            Z = float.Parse(z);
        }
    }

    public class VertexUV : EggGroup
    {
        public float U;
        public float V;
        public float W;

        public VertexUV(float u, float v)
        {
            U = u;
            V = v;
        }

        public VertexUV(string u, string v)
        {
            U = float.Parse(u);
            V = float.Parse(v);
        }
    }

    public class VertexRGBA
    {
        public float R = 1.0f;
        public float G = 1.0f;
        public float B = 1.0f;
        public float A = 1.0f;

        public VertexRGBA(float r, float g, float b, float a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public VertexRGBA(string r, string g, string b, string a)
        {
            R = float.Parse(r);
            G = float.Parse(g);
            B = float.Parse(b);
            A = float.Parse(a);
        }
    }

    public class VertexNormal
    {
        public float X;
        public float Y;
        public float Z;
    }
    #endregion VERTEX GROUPS

    public class EntityGroup : EggGroup
    {
        public string Dart = "structured";
        public string ObjectType = string.Empty;
        public List<EggGroup> Members = new();
        public bool IsCollision = false;
    }

    public class Polygon : EggGroup
    {
        public string TRef;
        public TextureGroup Texture;

        public VertexReference VertexRef;
        public List<Vertex> Vertices = new();

        public TextureGroup FindTextureInEgg(Panda3DEgg eggData)
        {
            var tex = eggData.Data.FirstOrDefault(e => e is TextureGroup);
            if (tex == default) return null;
            TextureGroup t = (TextureGroup)tex;
            Texture = t;
            return t;
        }
    }

    public class VertexReference : EggGroup
    {
        public int[] Indices;
        public string Pool;
    }

    public class Table : EggGroup
    {
        public List<Table> Tables = new();
        public List<Bundle> Bundles = new();
        public List<BaseAnimation> Animations = new();
    }

    public class Bundle : EggGroup
    {
        public List<Table> Tables = new();
    }

    public class AnimationGroup : EggGroup
    {
        public char Type;
    }

    public abstract class BaseAnimation : EggGroup
    {
        public string AnimationType = "S$Anim";
    }

    // Xfm$Anim_S$
    public class XfmAnimationS : BaseAnimation
    {
        public int FPS = 24;

        public List<SAnimation> Animations = new();
    }

    // S$Anim
    public class SAnimation : BaseAnimation
    {
        // xyz = translation (X Y Z)
        // ijk = scale (???)
        // prh = rotation (pitch roll heading)
        public char Variable;

        // Each value per frame
        public List<float> Values = new();
    }

    // Xfm$Anim
    public class XfmAnimation : BaseAnimation
    {
        public int FPS = 24;

        // s = scale
        // p = pitch
        // h = heading
        // r = roll
        // t = translation
        public char[] Order = new char[] { 's', 'p', 'r', 'h', 't' };
        public char[] Contents = new char[]
            { 'i', 'j', 'k', 'p', 'r', 'h', 'x', 'y', 'z' };

        // Each row is a frame
        public float[,] Animations;

        public XfmAnimation() { }

        public XfmAnimation(string order, string contents)
        {
            Order = order.ToCharArray();
            Contents = order.ToCharArray();
        }
    }

    public class Joint : EggGroup
    {
        public List<Joint> Joints = new();
        public Transform Transform;
        public Transform DefaultPose;
    }

    public class Transform : EggGroup
    {
        public float[,] Matrix4 = new float[4,4];

        public Transform(List<string> matricies)
        {
            if (matricies.Count != 16) return;

            int index = 0;
            for(int i = 0; i < 4; i++)
            {
                for(int j = 0; j < 4; j++)
                {
                    Matrix4[i, j] = float.Parse(matricies[index]);
                    index++;
                }
            }
        }
    }
}
