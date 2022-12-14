using RadCalc.Grammar;
using System.Text;

namespace RadCalc;

class Program
{
    static void Main(string[] args)
    {
        for (; ; )
        {
            Console.WriteLine("Enter the expression to calculate:");

            string? line;

            for (; ; )
            {
                Console.Write("> ");

                line = Console.ReadLine();
                if (line == null)
                    return;

                if (line.Length != 0)
                    break;
            }

            var parser = new Parser(new Scanner(new MemoryStream(Encoding.UTF8.GetBytes(line))));
            try
            {
                parser.Parse();
                if (parser.errors.count == 0)
                    Console.WriteLine(parser.Result);
            }
            catch (Exception e)
            {
                Console.Write("Error: ");
                Console.WriteLine(e.Message);
            }

            Console.WriteLine();
        }
    }
}
