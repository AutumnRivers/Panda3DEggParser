using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panda3DEggParser.EggClasses
{
    public class Egg
    {

    }

    public class EggTexture
    {
        public required string Filepath;

        public EggTexture(string filepath)
        {
            Filepath = filepath;
        }

        public class EggTextureScalar
        {

        }
    }
}
