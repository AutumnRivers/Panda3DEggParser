# Panda3D Egg Parser for C#/.NET
A very simple, very UNOFFICIAL, and not-at-all-polished parser for [Panda3D's egg files](https://docs.panda3d.org/1.10/python/pipeline/egg-files/index).

Mainly made for Toontown files. Tested with files from OpenToontown, Toontown Rewritten, and Toontown: Corporate Clash. Anything else is untested and might break.

**Requires .NET 8.0 or later!**

## Usage
1. Add `Panda3DEggParser.dll` as a reference in your project
2. Parse
3. ???
4. Profit

### Parsing from a file
```csharp
using Panda3DEggParser;

public class SomeClass {
    Panda3DEgg egg;

    public void SomeFunction() {
        var parser = new EggParser("path/to/file.egg", true);
        egg = parser.Parse();
    }
}
```

# Questions/Answers
**Q: I'm looking at the code... this is an incredibly inefficient way of doing this.**  
A: Correct!

**Q: (X) feature from egg files don't work**  
A: If Toontown servers don't use 'em, neither do I!

**Q: Can I use this?**  
A: Yeah, mate, [it's public domain](./LICENSE).

**Q: This doesn't work on (OS other than Windows)**  
A: ¯\\\_(ツ)\_/¯

**Q: I found a bug!**  
A: Open an issue, but I can't guarantee I'll fix it. I kinda got my miles worth out of this already.