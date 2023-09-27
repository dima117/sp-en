using CSharpMinifier;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Builder
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var path = "C:\\Users\\dima117a\\source\\repos\\sp-en\\SpaceEngineers\\Program.cs";

            var source = File.ReadAllText(path);

            var minifiedSource = Minifier.Minify(source);

            Console.Write(string.Join(string.Empty, minifiedSource));
            Console.ReadKey();
        }
    }
}
