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
            Console.WriteLine("PARSER: Starting to tokenize data");
            _eggTokens = new EggEntryTokenizer().Scan(eggData).GetEnumerator();
            Console.WriteLine("PARSER: Data tokenized!");
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

            Console.WriteLine("PARSER: Starting to parse data");
            _eggTokens.Reset();
            while(_eggTokens.MoveNext())
            {
                Console.WriteLine("PARSER: Parsing data...");
                var entry = ParseCurrentEntry();
                entries.Add(entry);
                egg.Data.Add(ParseEntry(entry));
            }

            EggEntry ParseCurrentEntry()
            {
                EggEntry currentEntry = default;

                if (_eggTokens.Current is not EntryTypeToken) return currentEntry;
                Console.WriteLine($"PARSER: Parsing entry type <{((EntryTypeToken)_eggTokens.Current).Value}>");
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
                    foreach(var member in entry.Content.Where(e => e.Type != "Dart" && e.Type != "ObjectType"))
                    {
                        group.Members.Add(ParseEntry(member));
                    }
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
                    }
                    return polygon;
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
                    Console.WriteLine("TOKENIZER: Tokenizing entry type");
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
                    Console.WriteLine($"TOKENIZER: {entryType}");
                    if (_entriesWithoutNames.Contains(entryType.ToLower())) continue;
                    Console.WriteLine("TOKENIZER: Tokenizing entry name");
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
                    Console.WriteLine($"TOKENIZER: {entryName}");
                } else if(c == '{') // entry content
                {
                    _reader.Read();
                    MovePastWhitespaceAndNewlines();
                    Console.WriteLine("TOKENIZER: Tokenizing entry content");
                    string filepath = string.Empty;
                    if((char)_reader.Peek() == '"')
                    {
                        if(filepath == string.Empty)
                        {
                            _reader.Read();
                            Console.WriteLine("TOKENIZER: Tokenizing filepath");
                            while ((char)_reader.Peek() != '"')
                            {
                                if ((char)_reader.Peek() == '"') break;
                                filepath += (char)_reader.Read();
                            }
                            tokens.Add(new FilepathToken(filepath));
                            Console.WriteLine("FILEPATH: " + filepath);
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
                    Console.WriteLine("TOKENIZER: Tokenizing entry exit");
                    tokens.Add(new ExitEntryToken());
                    _reader.Read();
                }
                else if(c == '>') { continue; }
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
    }
}
