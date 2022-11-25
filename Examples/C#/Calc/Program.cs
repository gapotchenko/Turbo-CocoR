using Calc.Grammar;
using System.Text;

namespace Calc;

class Program
{
    static void Main(string[] args)
    {
        for (; ; )
        {
            Console.WriteLine("Enter the expression to calculate:");

            string? line = Console.ReadLine();
            if (string.IsNullOrEmpty(line))
                break;

            var parser = new Parser(new Scanner(new MemoryStream(Encoding.UTF8.GetBytes(line))));
            parser.Parse();
            Console.WriteLine(parser.Result);

            Console.WriteLine();
        }
    }
}
