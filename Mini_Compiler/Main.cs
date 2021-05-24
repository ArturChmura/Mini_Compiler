using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QUT.Gppg;

namespace Mini_Compiler
{
    public class Corrector
    {
        private readonly Scanner scanner;

        public Corrector(Scanner scanner)
        {
            this.scanner = scanner;
        }
        public int ErrorsCount { get; private set; }

        private readonly HashSet<string> declaredIdentifiers = new HashSet<string>();
        public void ReportError(string message)
        {
            ErrorsCount++;
            Console.WriteLine($"Error on line {scanner.LineNumber}: {message}");
        }
        public void MakeDeclaration(List<string> identifiers)
        {
            foreach (var identifier in identifiers)
            {
                if(declaredIdentifiers.Contains(identifier))
                {
                    ReportError($"Identifier \"{identifier}\" already used.");
                }
                else
                {
                    declaredIdentifiers.Add(identifier);
                }
            }
        }
    }
    public interface INode
    {
        void GenCode();
    }


    public class RootNode : INode
    {
        public List<DeclarationNode> Declarations { get; }
        public RootNode(List<DeclarationNode> declarations)
        {
            Declarations = declarations;
        }

        public void GenCode()
        {
            foreach (var declaration in Declarations)
            {
                declaration.GenCode();
            }
        }
    }


    public class DeclarationNode : INode
    {
        public DeclarationNode(IType type, List<string> identifiers)
        {
            Type = type;
            Identifiers = identifiers;
        }
        public DeclarationNode(IType type, string identifier)
        {
            Type = type;
            Identifiers = new List<string> { identifier};
        }
        public IType Type { get; }
        public List<string> Identifiers { get; }

        public void GenCode()
        {
            string ret = Type.TypeName;
            foreach (var ident in Identifiers)
            {
                ret += " " + ident;
            }
            Console.WriteLine(ret);
        }
    }

    public interface IType
    {
        string TypeName { get; }
    }
    public class IntType : IType
    {
        public string TypeName => "int";
    }
    public class RealType : IType
    {
        public string TypeName => "double";
    }
    public class BoolType : IType
    {
        public string TypeName => "bool";
    }
    class Program
    {
        static int Main(string[] args)
        {
            string fileName;
            if (args.Length == 0)
            {
                //fileName = Console.ReadLine();
                fileName = "test.txt";
            }
            else
            {
                fileName = args[0];
            }
            StreamReader streamReader; 
            try
            {
                streamReader = new StreamReader(fileName);
            }
            catch (Exception)
            {
                return 1;
            }

            var scanner = new Scanner(streamReader.BaseStream);
            var parser = new Parser(scanner);

            parser.Parse();

            if (parser.ErrorsCount > 0 || scanner.ErrorsCount > 0)
            {
                Console.WriteLine("Not compiled.");
                Console.ReadLine();
                return 1;
            }
            else
            {
                parser.RootNode.GenCode();
                Console.WriteLine("Compiled.");
                Console.ReadLine();
                return 0;
            }

        }
    }
}
