using System;

namespace Panda3DEggParser
{
    /// <summary>
    /// A class representing all the info held by an EGG file.
    /// 
    /// <example>
    /// Example using the path to an EGG file:
    /// <code>
    /// Panda3DEgg egg = new Panda3DEgg();
    /// egg = new EggParser("path/to/file.egg", true).Parse();
    /// </code>
    /// </example>
    /// 
    /// While <c>Panda3DEgg</c>s can be made from scratch, it is not recommended to do so.
    /// </summary>
    public class Panda3DEgg
    {
        public string CoordinateSystem = "Z-Up";
        public string Comment;

        /// <value>Property <c>Data</c> holds all of the <c>Panda3DEgg</c>'s groups.</value>
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

    /// <summary>
    /// A generic group for unidentified groups in EGG files.
    /// If it's not a recognized group by the parser, but is valid syntax, it will be in one of these.
    /// </summary>
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
    /// Holds several different <c><seealso cref="Vertex"/></c> references, to be used in <seealso cref="EntityGroup"/>s.
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
        /// <summary>
        /// A positive integer representing the index of the vertex.
        /// Used in <seealso cref="VertexReference"/>s.
        /// </summary>
        public int Index;

        public float X;
        public float Y;
        public float Z;

        /// <summary>
        /// Unused in the parser.
        /// </summary>
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

    /// <summary>
    /// A class used to represent the UV value of a <seealso cref="Vertex"/>.
    /// </summary>
    public class VertexUV : EggGroup
    {
        public double U;
        public double V;
        public double W;

        public VertexUV(double u, double v)
        {
            U = u;
            V = v;
        }

        public VertexUV(string u, string v)
        {
            U = double.Parse(u);
            V = double.Parse(v);
        }
    }

    /// <summary>
    /// A class used to represent the RGBA value of a <seealso cref="Vertex"/>.
    /// </summary>
    public class VertexRGBA
    {
        public double R = 1.0;
        public double G = 1.0;
        public double B = 1.0;
        public double A = 1.0;

        public VertexRGBA(double r, double g, double b, double a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public VertexRGBA(string r, string g, string b, string a)
        {
            R = double.Parse(r);
            G = double.Parse(g);
            B = double.Parse(b);
            A = double.Parse(a);
        }
    }

    public class VertexNormal
    {
        public double X;
        public double Y;
        public double Z;

        public VertexNormal(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public VertexNormal(string x, string y, string z)
        {
            X = double.Parse(x);
            Y = double.Parse(y);
            Z = double.Parse(z);
        }
    }
    #endregion VERTEX GROUPS

    /// <summary>
    /// A group which often holds several <seealso cref="Polygon"/> values,
    /// but can hold any type of <seealso cref="EggGroup"/>.
    /// </summary>
    public class EntityGroup : EggGroup
    {
        public string Dart = "structured";
        public string ObjectType = string.Empty;
        public List<EggGroup> Members = new();

        /// <summary>
        /// Determines if the <seealso cref="EntityGroup"/> is a collision.
        /// If so, <seealso cref="CollisionType"/> must also be specified.
        /// </summary>
        public bool IsCollision = false;
        /// <summary>
        /// A string representing the collision type of a group.
        /// For more information, read the Panda3D documentation:
        /// <see href="https://docs.panda3d.org/1.10/python/pipeline/egg-files/syntax#collide"/>
        /// </summary>
        public string CollisionType = string.Empty;
    }

    public class Polygon : EggGroup
    {
        /// <summary>
        /// References the <see cref="TextureGroup"/> in the EGG file by its name.
        /// </summary>
        public string? TRef;
        /// <summary>
        /// The associated <see cref="TextureGroup"/>, if applicable. Provided for convenience.
        /// </summary>
        public TextureGroup? Texture;

        public VertexReference VertexRef;
        /// <summary>
        /// <see cref="List{T}"/> of associated vertices, provided for convenience.
        /// </summary>
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
        /// <summary>
        /// An array of <see cref="int"/>s that correlate to an <see cref="Vertex.Index"/>
        /// in an egg's <see cref="VertexPool"/>.
        /// </summary>
        public int[] Indices;
        /// <summary>
        /// Specifies the <see cref="VertexPool"/> to be used as a reference in the EGG.
        /// Most, if not all, Toontown models only use one pool per file, so this is largely redundant.
        /// </summary>
        public string Pool;
    }

    /// <summary>
    /// A table of values. Can hold <see cref="Table"/>, <see cref="Bundle"/> and <see cref="BaseAnimation"/> values.
    /// </summary>
    public class Table : EggGroup
    {
        public List<Table> Tables = new();
        public List<Bundle> Bundles = new();
        public List<BaseAnimation> Animations = new();
    }

    /// <summary>
    /// A bundle. Can only hold <see cref="Table"/>s.
    /// </summary>
    public class Bundle : EggGroup
    {
        public List<Table> Tables = new();
    }

    /// <summary>
    /// Abstract class for use with <see cref="XfmAnimationS"/>, <see cref="XfmAnimation"/>, and <see cref="SAnimation"/>.
    /// </summary>
    public abstract class BaseAnimation : EggGroup
    {
        public string AnimationType = "S$Anim";
    }

    // Xfm$Anim_S$
    /// <summary>
    /// An animation group that contain's the animation's frames-per-second value,
    /// and a list of <see cref="SAnimation"/> groups.
    /// <see href="https://docs.panda3d.org/1.10/cpp/reference/panda3d.egg.EggXfmSAnim"/>
    /// </summary>
    public class XfmAnimationS : BaseAnimation
    {
        public int FPS = 24;

        public List<SAnimation> Animations = new();
    }

    // S$Anim
    /// <summary>
    /// An animation group that is required to be the child of an <see cref="XfmAnimationS"/>.
    /// <see href="https://docs.panda3d.org/1.10/cpp/reference/panda3d.egg.EggSAnimData"/>
    /// </summary>
    public class SAnimation : BaseAnimation
    {
        // xyz = translation (X Y Z)
        // ijk = scale (???)
        // prh = rotation (pitch roll heading)
        public char Variable;

        // Each value per frame
        public List<double> Values = new();
    }

    // Xfm$Anim
    /// <summary>
    /// An animation group that holds its own animation data, with each row of <see cref="Animations"/> representing
    /// a different frame.
    /// Largely unused in Toontown projects.
    /// <see href="https://docs.panda3d.org/1.10/cpp/reference/panda3d.egg.EggXfmAnimData"/>
    /// </summary>
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
            Contents = contents.ToCharArray();
        }

        public XfmAnimation(string order, string contents, List<string> values)
        {
            Order = order.ToCharArray();
            Contents = contents.ToCharArray();

            Animations = new float[order.Length, values.Count / order.Length];

            int animIndex = 0;
            for(int i = 0; i < values.Count / order.Length; i++)
            {
                for(int j = 0; j < order.Length; j++)
                {
                    Animations[i, j] = float.Parse(values[animIndex]);
                    animIndex++;
                }
            }
        }

        public XfmAnimation(List<string> values)
        {
            Animations = new float[Order.Count(), values.Count / Order.Count()];

            int animIndex = 0;
            for (int i = 0; i < values.Count / Order.Count(); i++)
            {
                for (int j = 0; j < Order.Count(); j++)
                {
                    Animations[i, j] = float.Parse(values[animIndex]);
                    animIndex++;
                }
            }
        }
    }

    /// <summary>
    /// Holds joint data for a skeleton.
    /// </summary>
    public class Joint : EggGroup
    {
        public List<Joint> Joints = new();

        /// <summary>
        /// Holds current transform data.
        /// </summary>
        public Transform Transform;
        /// <summary>
        /// Holds transform data for the default pose. (The "rest position")
        /// </summary>
        public Transform DefaultPose;
    }

    public class Transform : EggGroup
    {
        /// <summary>
        /// A 4x4 grid representing transform data.
        /// </summary>
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
