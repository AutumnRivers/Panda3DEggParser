namespace Panda3DEggParser
{
    public class EggParser
    {
        private IEnumerator<EggToken> _eggTokens;

        public List<EggToken> EggTokens
        {
            get { return (List<EggToken>)_eggTokens; }
            set { _eggTokens = value.GetEnumerator(); }
        }

        public EggParser(string eggData)
        {
            _eggTokens = new EggEntryTokenizer().Scan(eggData).GetEnumerator();
        }

        public EggParser(string filepathOrEggData, bool isFile)
        {
            if(!isFile)
            {
                _eggTokens = new EggEntryTokenizer().Scan(filepathOrEggData).GetEnumerator();
                return;
            }

            FileStream eggFileStream = File.OpenRead(filepathOrEggData);
            StreamReader eggStream = new StreamReader(eggFileStream);
            _eggTokens = new EggEntryTokenizer().Scan(eggStream.ReadToEnd()).GetEnumerator();
            eggStream.Close();
        }

        public EggParser(FileStream eggFile)
        {
            StreamReader eggStream = new StreamReader(eggFile);
            _eggTokens = new EggEntryTokenizer().Scan(eggStream.ReadToEnd()).GetEnumerator();
            eggStream.Close();
        }

        // todo: RETURN DATA
        public Panda3DEgg Parse()
        {
            List<EggEntry> entries = new();
            Panda3DEgg egg = new Panda3DEgg();

            _eggTokens.Reset();
            while(_eggTokens.MoveNext())
            {
                var entry = ParseCurrentEntry();
                entries.Add(entry);
                egg.Data.Add(ParseEntry(entry));
            }

            EggEntry ParseCurrentEntry()
            {
                EggEntry currentEntry = default;

                if (_eggTokens.Current is not EntryTypeToken) return currentEntry;
                currentEntry = new()
                {
                    Type = ((EntryTypeToken)_eggTokens.Current).Value
                };
                while(_eggTokens.MoveNext())
                {
                    if (_eggTokens.Current is ExitEntryToken) break;

                    if(_eggTokens.Current is EntryNameToken nameToken)
                    {
                        currentEntry.Name = nameToken.Value;
                    } else if(_eggTokens.Current is EntryContentToken contentToken)
                    {
                        currentEntry.Values = contentToken.Values;
                    } else if(_eggTokens.Current is EntryTypeToken entry)
                    {
                        currentEntry.Content.Add(ParseCurrentEntry());
                    } else if(_eggTokens.Current is FilepathToken filepathToken)
                    {
                        currentEntry.Filepath = filepathToken.Value;
                    } else
                    {
                        throw new InvalidDataException($"Unrecognized token {_eggTokens.Current.GetType().Name}");
                    }
                }

                return currentEntry;
            }

            return egg;
        }

        public EggGroup ParseEntry(EggEntry entry)
        {
            switch(entry.Type)
            {
                case "Group":
                    var group = new EntityGroup();
                    var dart = entry.Content.FirstOrDefault(e => e.Type == "Dart");
                    if(dart != default) { group.Dart = dart.Values[0]; }
                    var otype = entry.Content.FirstOrDefault(e => e.Type == "ObjectType");
                    if(otype != default) { group.ObjectType = otype.Values[0]; }
                    var col = entry.Content.FirstOrDefault(e => e.Type == "Collide");
                    if(col != default)
                    {
                        group.IsCollision = true;
                        group.CollisionType = col.Values[0];
                    }
                    foreach(var member in entry.Content.Where(e => e.Type != "Dart" && e.Type != "ObjectType"))
                    {
                        group.Members.Add(ParseEntry(member));
                    }
                    group.Name = entry.Name;
                    return group;
                case "Texture":
                    var tex = new TextureGroup(entry.Filepath);
                    tex.Name = entry.Name;
                    List<EggGroup> scalars = new();
                    foreach(var c in entry.Content) { scalars.Add(ParseEntry(c)); }
                    tex.Scalars = scalars;
                    return tex;
                case "VertexPool":
                    var pool = new VertexPool();
                    foreach (var v in entry.Content.Where(e => e.Type == "Vertex"))
                    {
                        pool.References.Add((Vertex)ParseEntry(v));
                    }
                    pool.Name = entry.Name;
                    return pool;
                case "Vertex":
                    var vertex = new Vertex(entry.Name, entry.Values[0],
                        entry.Values[1], entry.Values[2]);
                    var rgba = entry.Content.FirstOrDefault(e => e.Type == "RGBA");
                    if (rgba != default)
                    {
                        vertex.RGBA = new VertexRGBA(rgba.Values[0], rgba.Values[1],
                            rgba.Values[2], rgba.Values[3]);
                    }
                    var uv = entry.Content.FirstOrDefault(e => e.Type == "UV");
                    if(uv != default)
                    {
                        vertex.UV = new VertexUV(uv.Values[0], uv.Values[1]);
                    }
                    return vertex;
                case "Polygon":
                    var polygon = new Polygon();
                    var tref = entry.Content.FirstOrDefault(e => e.Type == "TRef");
                    if(tref != default)
                    {
                        polygon.TRef = tref.Values[0];
                    }
                    var vref = entry.Content.FirstOrDefault(e => e.Type == "VertexRef");
                    if(vref != default)
                    {
                        var vr = new VertexReference();
                        vr.Indices = new int[vref.Values.Count];
                        for(int i = 0; i < vref.Values.Count; i++)
                        {
                            int indice = int.Parse(vref.Values[i]);
                            vr.Indices[i] = indice;
                        }
                        vr.Pool = vref.Content[0].Values[0];
                        polygon.VertexRef = vr;
                    }
                    return polygon;
                case "Table":
                    var table = new Table()
                    {
                        Name = entry.Name
                    };
                    var bundles = entry.Content.Where(e => e.Type == "Bundle");
                    foreach(var bundle in bundles)
                    {
                        table.Bundles.Add((Bundle)ParseEntry(bundle));
                    }
                    var tbls = entry.Content.Where(e => e.Type == "Table");
                    foreach(var t in tbls)
                    {
                        table.Tables.Add((Table)ParseEntry(t));
                    }
                    var anims = entry.Content.Where(e => e.Type == "Xfm$Anim_S$" ||
                    e.Type == "S$Anim" || e.Type == "Xfm$Anim");
                    foreach(var a in anims)
                    {
                        table.Animations.Add((BaseAnimation)ParseEntry(a));
                    }
                    return table;
                case "Bundle":
                    var bndl = new Bundle();
                    var tables = entry.Content.Where(e => e.Type == "Table");
                    foreach(var t in tables)
                    {
                        bndl.Tables.Add((Table)ParseEntry(t));
                    }
                    return bndl;
                case "S$Anim":
                    var sanim = new SAnimation();
                    sanim.Variable = entry.Name[0];
                    foreach(var val in entry.Content.First(e => e.Type == "V").Values)
                    {
                        sanim.Values.Add(float.Parse(val));
                    }
                    return sanim;
                case "Xfm$Anim_S$":
                    var xfmanims = new XfmAnimationS();
                    var fps = entry.Content.FirstOrDefault(e => e.Type == "Scalar" && e.Name == "fps");
                    if(fps != default) { xfmanims.FPS = int.Parse(fps.Values[0]); }
                    foreach (var sanimation in entry.Content.Where(e => e.Type == "S$Anim"))
                    {
                        xfmanims.Animations.Add((SAnimation)ParseEntry(sanimation));
                    }
                    return xfmanims;
                case "Xfm$Anim":
                    var xfmanim = new XfmAnimation(entry.Values);
                    // It's assumed animations are always in xform
#if DEBUG
                    Console.WriteLine("Panda3DEggParser WARN: " +
                        "<Xfm$Anim> entries are completely untested and might error out or be " +
                        "formatted/parsed incorrectly. Please report any bugs you find, " +
                        "along with an example file.");
#endif
                    var order = entry.Content.FirstOrDefault(e => e.Type == "Scalar" && e.Name == "order");
                    if(order != default) { xfmanim.Order = order.Values[0].ToCharArray(); }
                    var xfmfps = entry.Content.FirstOrDefault(e => e.Type == "Scalar" && e.Name == "fps");
                    if(xfmfps != default) { xfmanim.FPS = int.Parse(xfmfps.Values[0]); }
                    var contents = entry.Content.FirstOrDefault(e => e.Type == "Scalar" && e.Name == "contents");
                    return xfmanim;
                case "Joint":
                    var joint = new Joint();
                    joint.Name = entry.Name;
                    var joints = entry.Content.Where(e => e.Type == "Joint");
                    foreach(var j in joints)
                    {
                        joint.Joints.Add((Joint)ParseEntry(j));
                    }
                    var transform = entry.Content.FirstOrDefault(e => e.Type == "Transform");
                    if(transform != default)
                    {
                        var m4 = transform.Content.FirstOrDefault(e => e.Type == "Matrix4");
                        if(m4 != default)
                        {
                            joint.Transform = new Transform(m4.Values);
                        }
                    }
                    var defaultPose = entry.Content.FirstOrDefault(e => e.Type == "DefaultPose");
                    if(defaultPose != default)
                    {
                        var m4 = defaultPose.Content.FirstOrDefault(e => e.Type == "Matrix4");
                        if(m4 != default)
                        {
                            joint.DefaultPose = new Transform(m4.Values);
                        }
                    }
                    return joint;
                default:
                    var g = new GenericEggGroup() {
                        Name = entry.Name
                    };
                    if(entry.Values.Count > 0) { g.Value = entry.Values[0]; }
                    return g;
            }
        }
    }

    public class EggEntry
    {
        public string Type;
        public string Name = string.Empty;
        public string Filepath = string.Empty;
        public List<string> Values = new();
        public List<EggEntry> Content = new();
    }

    public abstract class EggToken { }

    public class EntryTypeToken : EggToken
    {
        private readonly string _value;

        public EntryTypeToken(string value)
        {
            _value = value;
        }

        public string Value
        {
            get { return _value; }
        }
    }

    public class EntryNameToken : EggToken
    {
        private readonly string _value;

        public EntryNameToken(string value)
        {
            _value = value;
        }

        public string Value
        {
            get { return _value; }
        }
    }

    public class EntryContentToken : EggToken
    {
        private readonly List<string> _values;

        public EntryContentToken(string unprocessedValue)
        {
            StringReader reader = new(unprocessedValue);
            List<string> values = new();
            string currentValue = string.Empty;
            while(reader.Peek() != -1)
            {
                var c = (char)reader.Peek();
                if (c == '<') break;
                if (c == '{')
                {
                    reader.Read();
                    continue;
                }
                if(char.IsWhiteSpace(c) || c.Equals('\r'))
                {
                    if(currentValue != string.Empty)
                    {
                        values.Add(currentValue);
                    }
                    currentValue = string.Empty;
                    reader.Read();
                    continue;
                }

                currentValue += (char)reader.Read();
            }
            reader.Close();
            _values = values;
        }

        public EntryContentToken(List<string> values)
        {
            _values = values;
        }

        public List<string> Values
        {
            get { return _values; }
        }
    }

    public class FilepathToken : EggToken
    {
        private readonly string _value;

        public FilepathToken(string value)
        {
            _value = value;
        }

        public string Value
        {
            get { return _value; }
        }
    }

    public class ExitEntryToken : EggToken { }

    public class EggEntryTokenizer
    {
        private StringReader _reader;

        private readonly string[] _entriesWithoutNames = new string[]
        {
            "CoordinateSystem".ToLower(),
            "Comment".ToLower()
        };

        private char GetNextChar()
        {
            return (char)_reader.Read();
        }

        public IEnumerable<EggToken> Scan(string eggContent)
        {
            _reader = new(eggContent);

            var tokens = new List<EggToken>();
            while(_reader.Peek() != -1)
            {
                var c = (char)_reader.Peek();
                if(char.IsWhiteSpace(c))
                {
                    _reader.Read();
                    continue;
                }

                if(c == '<')
                {
                    string entryType = string.Empty;
                    while((char)_reader.Read() != '>')
                    {
                        if ((char)_reader.Peek() == '>')
                        {
                            _reader.Read();
                            break;
                        }
                        entryType += (char)_reader.Peek();
                    }
                    tokens.Add(new EntryTypeToken(entryType));
                    if (_entriesWithoutNames.Contains(entryType.ToLower())) continue;
                    string entryName = string.Empty;
                    while((char)_reader.Read() != '{')
                    {
                        if((char)_reader.Peek() == '{')
                        {
                            break;
                        }
                        if (char.IsWhiteSpace((char)_reader.Peek())) continue;
                        entryName += (char)_reader.Peek();
                    }
                    tokens.Add(new EntryNameToken(entryName));
                } else if(c == '{') // entry content
                {
                    _reader.Read();
                    MovePastWhitespaceAndNewlines();
                    string filepath = string.Empty;
                    if((char)_reader.Peek() == '"')
                    {
                        if(filepath == string.Empty)
                        {
                            _reader.Read();
                            while ((char)_reader.Peek() != '"')
                            {
                                if ((char)_reader.Peek() == '"') break;
                                filepath += (char)_reader.Read();
                            }
                            tokens.Add(new FilepathToken(filepath));
                        }
                    }
                    string toBeProcessed = string.Empty;
                    if((char)_reader.Peek() == '"') { _reader.Read(); }
                    while((char)_reader.Peek() != '<' && (char)_reader.Peek() != '}')
                    {
                        toBeProcessed += (char)_reader.Read();
                    }
                    tokens.Add(new EntryContentToken(toBeProcessed));
                } else if(c == '}')
                {
                    tokens.Add(new ExitEntryToken());
                    _reader.Read();
                }
                else if(c == '>') { continue; }
                else if(c == '/')
                {
                    // Probably a comment. Skip to next line
                    MovePastComment();
                }
                else
                {
                    throw new FormatException("Egg is malformed -- unrecognized token " + c);
                }
            }

            _reader.Close();
            return tokens;
        }

        private void MovePastWhitespaceAndNewlines()
        {
            if(char.IsWhiteSpace((char)_reader.Peek()) ||
                (char)_reader.Peek() == '\r' ||
                (char)_reader.Peek() == '\n')
            {
                _reader.Read();
                MovePastWhitespaceAndNewlines();
            }

            return;
        }

        private void MovePastComment()
        {
            if((char)_reader.Peek() != '\n' &&
                (char)_reader.Peek() != '\r')
            {
                _reader.Read();
                MovePastComment();
            }

            return;
        }
    }
}
