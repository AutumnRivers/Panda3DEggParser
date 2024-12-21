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

        // todo: RETURN DATA
        public List<EggEntry> Parse()
        {
            List<EggEntry> entries = new();

            Console.WriteLine("PARSER: Starting to parse data");
            _eggTokens.Reset();
            while(_eggTokens.MoveNext())
            {
                Console.WriteLine("PARSER: Parsing data...");
                entries.Add(ParseCurrentEntry());
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

            return entries;
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
            Console.WriteLine(unprocessedValue);
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
                    Console.WriteLine("TOKENIZER: Tokenizing entry content");
                    string toBeProcessed = string.Empty;
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
                else if(c == '"')
                {
                    Console.WriteLine("TOKENIZER: Tokenizing filepath");
                    string filepath = string.Empty;
                    _reader.Read();
                    while((char)_reader.Read() != '"')
                    {
                        filepath += (char)_reader.Peek();
                    }
                    tokens.Add(new FilepathToken(filepath));
                } else if(c == '>') { continue; }
                else
                {
                    throw new FormatException("Egg is malformed -- unrecognized token " + c);
                }
            }

            _reader.Close();
            return tokens;
        }
    }
}
