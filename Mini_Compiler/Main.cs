using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QUT.Gppg;

namespace Mini_Compiler
{

    #region Parser
    // It's a part of Parser, so I don't have to write code in ".y" file
    public class ParserCS
    {
        private static Scanner scanner;

        public ParserCS(Scanner _scanner)
        {
            scanner = _scanner;
        }
        public static int ErrorsCount { get; private set; }

        private readonly HashSet<Identifier> declaredIdentifiers = new HashSet<Identifier>();
        public static void ReportError(string message)
        {
            ErrorsCount++;
            Console.WriteLine($"Error on line {scanner.LineNumber}: {message}");
        }
        public void MakeDeclaration(IType type, List<string> identifiersNames)
        {
            foreach (var identifierName in identifiersNames)
            {
                if (declaredIdentifiers.Any(di => di.Name == identifierName))
                {
                    ReportError($"Identifier \"{identifierName}\" already used.");
                }
                else
                {
                    declaredIdentifiers.Add(new Identifier(type, identifierName));
                }
            }
        }

        public Identifier GetIdentifier(string name)
        {
            var ident = declaredIdentifiers.FirstOrDefault(di => di.Name == name);
            if (ident == null)
            {
                ReportError($"Identifier \"{name}\" undeclared.");
                return new Identifier(new IntType(), "");
            }
            else
            {
                return ident;
            }
        }

        static int registerNumber = 0;
        public static string GetUniqeRegister()
        {
            return "%" + (++registerNumber).ToString();
        }
    }
    #endregion


    #region Type
    public interface IType
    {
        string TypeName { get; }
        string LLVMName { get; }
        bool CanAssign(IType type);
        bool CanConvertToInt();
        bool CanConvertToDouble();
        bool CanConvertToBool();
        bool Accept(ITypeVisitor visitor);
    }


    public class IntType : IType
    {
        public string TypeName => "int";

        public string LLVMName => "i32";

        public bool Accept(ITypeVisitor visitor)
        {
            return visitor.Can(this);
        }

        public bool CanAssign(IType type) => type.CanConvertToInt();

        public bool CanConvertToBool() => false;

        public bool CanConvertToDouble() => true;

        public bool CanConvertToInt() => true;
    }
    public class DoubleType : IType
    {
        public string TypeName => "double";
        public string LLVMName => "double";
        public bool Accept(ITypeVisitor visitor)
        {
            return visitor.Can(this);
        }
        public bool CanAssign(IType type) => type.CanConvertToDouble();
        public bool CanConvertToBool() => false;

        public bool CanConvertToDouble() => true;

        public bool CanConvertToInt() => false;
    }
    public class BoolType : IType
    {
        public string TypeName => "bool";
        public string LLVMName => "i1";
        public bool Accept(ITypeVisitor visitor)
        {
            return visitor.Can(this);
        }
        public bool CanAssign(IType type) => type.CanConvertToBool();
        public bool CanConvertToBool() => true;

        public bool CanConvertToDouble() => false;

        public bool CanConvertToInt() => false;
    }
    #endregion Type


    #region Nodes
    public interface INode
    {
        string GenCode();
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
            Identifiers = new List<string> { identifier };
        }
        public IType Type { get; }
        public List<string> Identifiers { get; }

        public string GenCode()
        {
            foreach (var ident in Identifiers)
            {
                Emiter.EmitCode($"%{ident} = alloca {Type.LLVMName}");
            }
            return null;
        }
    }
    public abstract class InstructionNode : INode
    {
        public abstract string GenCode();
    }
    public class BlockInstructionNode : InstructionNode
    {
        public List<DeclarationNode> Declarations { get; }
        public List<InstructionNode> Instructions { get; }

        public BlockInstructionNode(List<DeclarationNode> declarations, List<InstructionNode> instructions)
        {
            Declarations = declarations;
            Instructions = instructions;
        }

        public override string GenCode()
        {
            foreach (var declaration in Declarations)
            {
                declaration.GenCode();
            }
            foreach (var instruction in Instructions)
            {
                instruction.GenCode();
            }
            return null;
        }
    }

    public class ExpressionInstructionNode : InstructionNode
    {
        public ExpressionInstructionNode(IExpression expression)
        {
            Expression = expression;
        }

        public IExpression Expression { get; }

        public override string GenCode()
        {
            Expression.GenCode();
            return null;
        }
    }

    #endregion


    #region Identifiers
    public class Identifier : IExpression
    {
        public Identifier(IType type, string name)
        {
            Type = type;
            Name = name;
        }
        public IType Type { get; }
        public string Name { get; }

        public string GenCode()
        {
            var register = ParserCS.GetUniqeRegister();
            Emiter.EmitCode($"%{register} = load {Type.LLVMName}, {Type.LLVMName}* %{Name}");
            return register;
        }
    }

    #endregion


    #region Expressions
    public interface IExpression : INode
    {
        IType Type { get; }
    }

    #region ConstantExpressions
    public abstract class ConstantExpression : IExpression
    {
        public abstract IType Type { get; }

        public abstract string GenCode();
    }
    public class IntConstantExpression : ConstantExpression
    {
        public IntConstantExpression(string value)
        {
            Value = int.Parse(value, CultureInfo.CreateSpecificCulture("en-US"));
        }

        public int Value { get; }
        public override IType Type { get => new IntType(); }

        public override string GenCode()
        {
            return Value.ToString();
        }
    }

    public class DoubleConstantExpression : ConstantExpression
    {
        public DoubleConstantExpression(string value)
        {
            Value = double.Parse(value, CultureInfo.CreateSpecificCulture("en-US"));
        }

        public double Value { get; }
        public override IType Type { get => new DoubleType(); }

        public override string GenCode()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:0.0###############}", Value);
        }
    }

    public class BoolConstantExpression : ConstantExpression
    {
        public BoolConstantExpression(bool value)
        {
            Value = value;
        }

        public bool Value { get; }
        public override IType Type { get => new BoolType(); }

        public override string GenCode()
        {
            if(Value)
            {
                return "1";
            }
            else
            {
                return "0";
            }
            
        }
    }
    #endregion ConstantExpressions


    public interface ITypeVisitor
    {
        bool Can(IntType type);
        bool Can(DoubleType type);
        bool Can(BoolType type);
    }

    #region UnaryExpressions

    public abstract class UnaryExpression : IExpression, ITypeVisitor
    {
        public UnaryExpression(IExpression expression)
        {
            Expression = expression;
            if (expression.Type.Accept(this) == false)
            {
                ParserCS.ReportError("Wrong type on unary expression");
            }
        }
        public IType Type => Expression.Type;

        public IExpression Expression { get; }

        public abstract bool Can(IntType type);

        public abstract bool Can(DoubleType type);

        public abstract bool Can(BoolType type);

        public abstract string GenCode();
    }

    public class UnaryMinusExpression : UnaryExpression, ITypeVisitor
    {
        public UnaryMinusExpression(IExpression expression) : base(expression) { }
        public override bool Can(IntType type) => true;
        public override bool Can(DoubleType type) => true;
        public override bool Can(BoolType type) => false;

        public override string GenCode()
        {
            var registerNumber = ParserCS.GetUniqeRegister();
            var exRegisterNumber = Expression.GenCode();
            Console.WriteLine($"-{exRegisterNumber} to register {registerNumber}");
            return registerNumber;
        }
    }

    public class BitNegationExpression : UnaryExpression, ITypeVisitor
    {
        public BitNegationExpression(IExpression expression) : base(expression) { }
        public override bool Can(IntType type) => true;
        public override bool Can(DoubleType type) => false;
        public override bool Can(BoolType type) => false;

        public override string GenCode()
        {
            var registerNumber = ParserCS.GetUniqeRegister();
            var exRegisterNumber = Expression.GenCode();
            Console.WriteLine($"~{exRegisterNumber} to register {registerNumber}");
            return registerNumber;
        }
    }

    public class LogicNegationExpression : UnaryExpression, ITypeVisitor
    {
        public LogicNegationExpression(IExpression expression) : base(expression) { }
        public override bool Can(IntType type) => false;
        public override bool Can(DoubleType type) => false;
        public override bool Can(BoolType type) => true;

        public override string GenCode()
        {
            var registerNumber = ParserCS.GetUniqeRegister();
            var exRegisterNumber = Expression.GenCode();
            Console.WriteLine($"!{exRegisterNumber} to register {registerNumber}");
            return registerNumber;
        }
    }

    public class IntConversionExpression : UnaryExpression, ITypeVisitor
    {
        public IntConversionExpression(IExpression expression) : base(expression) { }
        public override bool Can(IntType type) => true;
        public override bool Can(DoubleType type) => true;
        public override bool Can(BoolType type) => true;

        public override string GenCode()
        {
            var registerNumber = ParserCS.GetUniqeRegister();
            var exRegisterNumber = Expression.GenCode();
            Console.WriteLine($"(int){exRegisterNumber} to register {registerNumber}");
            return registerNumber;
        }
    }

    public class DoubleConversionExpression : UnaryExpression, ITypeVisitor
    {
        public DoubleConversionExpression(IExpression expression) : base(expression) { }
        public override bool Can(IntType type) => true;
        public override bool Can(DoubleType type) => true;
        public override bool Can(BoolType type) => false;

        public override string GenCode()
        {
            var registerNumber = ParserCS.GetUniqeRegister();
            var exRegisterNumber = Expression.GenCode();
            Console.WriteLine($"(double){exRegisterNumber} to register {registerNumber}");
            return registerNumber;
        }
    }
    #endregion UnaryExpressions

    #region BitsExpressions
    public abstract class BitsExpression : IExpression, ITypeVisitor
    {
        public BitsExpression(IExpression leftExpression, IExpression rightExpression)
        {
            LeftExpression = leftExpression;
            RightExpression = rightExpression;
            if (leftExpression.Type.Accept(this) == false || rightExpression.Type.Accept(this) == false)
            {
                ParserCS.ReportError("Wrong type on bits expression");
            }
        }

        public IType Type => LeftExpression.Type;

        public IExpression LeftExpression { get; }
        public IExpression RightExpression { get; }

        public bool Can(IntType type) => true;

        public bool Can(DoubleType type) => false;

        public bool Can(BoolType type) => false;

        public abstract string GenCode();
    }

    public class BitsOrExpression : BitsExpression
    {
        public BitsOrExpression(IExpression leftExpression, IExpression rightExpression) : base(leftExpression, rightExpression) { }

        public override string GenCode()
        {
            var register = ParserCS.GetUniqeRegister();
            var lExpReg = LeftExpression.GenCode();
            var rExpReg = RightExpression.GenCode();
            Console.WriteLine($"{lExpReg} | {rExpReg} to register {register}");
            return register;
        }
    }
    public class BitAndExpression : BitsExpression
    {
        public BitAndExpression(IExpression leftExpression, IExpression rightExpression) : base(leftExpression, rightExpression) { }

        public override string GenCode()
        {
            var register = ParserCS.GetUniqeRegister();
            var lExpReg = LeftExpression.GenCode();
            var rExpReg = RightExpression.GenCode();
            Console.WriteLine($"{lExpReg} & {rExpReg} to register {register}");
            return register;
        }
    }
    #endregion BitsExpressions

    #region AdditivesMultiplicativeExpressions
    public abstract class AdditivesMultiplicativeExpressions : IExpression, ITypeVisitor
    {
        public AdditivesMultiplicativeExpressions(IExpression leftExpression, IExpression rightExpression)
        {
            LeftExpression = leftExpression;
            RightExpression = rightExpression;
            if (leftExpression.Type.Accept(this) == false || rightExpression.Type.Accept(this) == false)
            {
                ParserCS.ReportError("Wrong type on bits expression");
            }
        }

        private bool isDouble = false;
        public IType Type { get { if (isDouble) return new DoubleType(); return new IntType(); } }

        public IExpression LeftExpression { get; }
        public IExpression RightExpression { get; }

        public bool Can(IntType type) => true;

        public bool Can(DoubleType type)
        {
            isDouble = true;
            return true;
        }

        public bool Can(BoolType type) => false;

        public abstract string GenCode();
    }

    public class Sum : AdditivesMultiplicativeExpressions
    {
        public Sum(IExpression leftExpression, IExpression rightExpression) : base(leftExpression, rightExpression) { }

        public override string GenCode()
        {
            var register = ParserCS.GetUniqeRegister();
            var lExpReg = LeftExpression.GenCode();
            var rExpReg = RightExpression.GenCode();
            Console.WriteLine($"{lExpReg} + {rExpReg} to register {register}");
            return register;
        }
    }
    public class Subtraction : AdditivesMultiplicativeExpressions
    {
        public Subtraction(IExpression leftExpression, IExpression rightExpression) : base(leftExpression, rightExpression) { }

        public override string GenCode()
        {
            var register = ParserCS.GetUniqeRegister();
            var lExpReg = LeftExpression.GenCode();
            var rExpReg = RightExpression.GenCode();
            Console.WriteLine($"{lExpReg} - {rExpReg} to register {register}");
            return register;
        }
    }

    public class Multiplication : AdditivesMultiplicativeExpressions
    {
        public Multiplication(IExpression leftExpression, IExpression rightExpression) : base(leftExpression, rightExpression) { }

        public override string GenCode()
        {
            var register = ParserCS.GetUniqeRegister();
            var lExpReg = LeftExpression.GenCode();
            var rExpReg = RightExpression.GenCode();
            Console.WriteLine($"{lExpReg} * {rExpReg} to register {register}");
            return register;
        }
    }

    public class Division : AdditivesMultiplicativeExpressions
    {
        public Division(IExpression leftExpression, IExpression rightExpression) : base(leftExpression, rightExpression) { }

        public override string GenCode()
        {
            var register = ParserCS.GetUniqeRegister();
            var lExpReg = LeftExpression.GenCode();
            var rExpReg = RightExpression.GenCode();
            Console.WriteLine($"{lExpReg} / {rExpReg} to register {register}");
            return register;
        }
    }
    #endregion AdditivesMultiplicativeExpressions

    #region RelationsExpressions
    public abstract class RelationsExpression : IExpression, ITypeVisitor
    {
        public RelationsExpression(IExpression leftExpression, IExpression rightExpression)
        {
            LeftExpression = leftExpression;
            RightExpression = rightExpression;
        }

        public IType Type => new BoolType();

        public IExpression LeftExpression { get; }
        public IExpression RightExpression { get; }

        public abstract bool Can(IntType type);

        public abstract bool Can(DoubleType type);

        public abstract bool Can(BoolType type);

        public abstract string GenCode();
    }
    public abstract class AllTypeRelationExpression : RelationsExpression
    {
        public AllTypeRelationExpression(IExpression leftExpression, IExpression rightExpression) : base(leftExpression, rightExpression)
        {
            if (leftExpression.Type.Accept(this) == false || rightExpression.Type.Accept(this) == false)
            {
                ParserCS.ReportError("Wrong type on relation expression");
            }
        }
        bool isNumber = false;
        bool isBool = false;
        public override bool Can(IntType type)
        {
            isNumber = true;
            if (isBool) return false;
            return true;
        }
        public override bool Can(DoubleType type)
        {
            isNumber = true;
            if (isBool) return false;
            return true;
        }
        public override bool Can(BoolType type)
        {
            isBool = true;
            if (isNumber) return false;
            return true;
        }

    }
    public class EqualsExpression : AllTypeRelationExpression
    {
        public EqualsExpression(IExpression leftExpression, IExpression rightExpression) : base(leftExpression, rightExpression) { }

        public override string GenCode()
        {
            var register = ParserCS.GetUniqeRegister();
            var lExpReg = LeftExpression.GenCode();
            var rExpReg = RightExpression.GenCode();
            Console.WriteLine($"{lExpReg} == {rExpReg} to register {register}");
            return register;
        }
    }

    public class NotequalsExpression : AllTypeRelationExpression
    {
        public NotequalsExpression(IExpression leftExpression, IExpression rightExpression) : base(leftExpression, rightExpression) { }

        public override string GenCode()
        {
            var register = ParserCS.GetUniqeRegister();
            var lExpReg = LeftExpression.GenCode();
            var rExpReg = RightExpression.GenCode();
            Console.WriteLine($"{lExpReg} != {rExpReg} to register {register}");
            return register;
        }
    }

    public abstract class NumberRelationExpression : RelationsExpression
    {
        public NumberRelationExpression(IExpression leftExpression, IExpression rightExpression) : base(leftExpression, rightExpression)
        {
            if (leftExpression.Type.Accept(this) == false || rightExpression.Type.Accept(this) == false)
            {
                ParserCS.ReportError("Wrong type on relation expression");
            }
        }
        public override bool Can(IntType type) => true;
        public override bool Can(DoubleType type) => true;
        public override bool Can(BoolType type) => false;

    }

    public class GreaterExpression : NumberRelationExpression
    {
        public GreaterExpression(IExpression leftExpression, IExpression rightExpression) : base(leftExpression, rightExpression) { }

        public override string GenCode()
        {
            var register = ParserCS.GetUniqeRegister();
            var lExpReg = LeftExpression.GenCode();
            var rExpReg = RightExpression.GenCode();
            Console.WriteLine($"{lExpReg} > {rExpReg} to register {register}");
            return register;
        }
    }

    public class GreaterOrEqualExpression : NumberRelationExpression
    {
        public GreaterOrEqualExpression(IExpression leftExpression, IExpression rightExpression) : base(leftExpression, rightExpression) { }

        public override string GenCode()
        {
            var register = ParserCS.GetUniqeRegister();
            var lExpReg = LeftExpression.GenCode();
            var rExpReg = RightExpression.GenCode();
            Console.WriteLine($"{lExpReg} >= {rExpReg} to register {register}");
            return register;
        }
    }
    public class LessExpression : NumberRelationExpression
    {
        public LessExpression(IExpression leftExpression, IExpression rightExpression) : base(leftExpression, rightExpression) { }

        public override string GenCode()
        {
            var register = ParserCS.GetUniqeRegister();
            var lExpReg = LeftExpression.GenCode();
            var rExpReg = RightExpression.GenCode();
            Console.WriteLine($"{lExpReg} < {rExpReg} to register {register}");
            return register;
        }
    }

    public class LessOrEqualExpression : NumberRelationExpression
    {
        public LessOrEqualExpression(IExpression leftExpression, IExpression rightExpression) : base(leftExpression, rightExpression) { }

        public override string GenCode()
        {
            var register = ParserCS.GetUniqeRegister();
            var lExpReg = LeftExpression.GenCode();
            var rExpReg = RightExpression.GenCode();
            Console.WriteLine($"{lExpReg} <= {rExpReg} to register {register}");
            return register;
        }
    }
    #endregion RelationsExpressions

    #region LogicsExpressions
    public abstract class LogicsExpression : IExpression, ITypeVisitor
    {
        public LogicsExpression(IExpression leftExpression, IExpression rightExpression)
        {
            LeftExpression = leftExpression;
            RightExpression = rightExpression;
            if (leftExpression.Type.Accept(this) == false || rightExpression.Type.Accept(this) == false)
            {
                ParserCS.ReportError("Wrong type on logic expression");
            }
        }

        public IType Type => new BoolType();

        public IExpression LeftExpression { get; }
        public IExpression RightExpression { get; }

        public bool Can(IntType type) => false;
        public bool Can(DoubleType type) => false;
        public bool Can(BoolType type) => true;

        public abstract string GenCode();
    }

    public class AndExpression : LogicsExpression
    {
        public AndExpression(IExpression leftExpression, IExpression rightExpression) : base(leftExpression, rightExpression) { }
        public override string GenCode()
        {
            var register = ParserCS.GetUniqeRegister();
            var lExpReg = LeftExpression.GenCode();
            var rExpReg = RightExpression.GenCode();
            Console.WriteLine($"{lExpReg} && {rExpReg} to register {register}");
            return register;
        }
    }

    public class OrExpression : LogicsExpression
    {
        public OrExpression(IExpression leftExpression, IExpression rightExpression) : base(leftExpression, rightExpression) { }
        public override string GenCode()
        {
            var register = ParserCS.GetUniqeRegister();
            var lExpReg = LeftExpression.GenCode();
            var rExpReg = RightExpression.GenCode();
            Console.WriteLine($"{lExpReg} || {rExpReg} to register {register}");
            return register;
        }
    }
    #endregion LogicsExpressions


    public class AssignExpression : IExpression
    {
        public AssignExpression(Identifier identifier, IExpression expression)
        {
            Identifier = identifier;
            Expression = expression;
            if (!Identifier.Type.CanAssign(expression.Type))
            {
                ParserCS.ReportError($"Cannot assign {expression.Type.TypeName} to {identifier.Type.TypeName}");
            }
        }

        public Identifier Identifier { get; }
        public IExpression Expression { get; }

        public IType Type => (Identifier.Type);

        public string GenCode()
        {
            var rightRegister = Expression.GenCode();
            Emiter.EmitCode($"store {Identifier.Type.LLVMName} {rightRegister}, {Identifier.Type.LLVMName}* %{Identifier.Name}");
            var leftRegister = ParserCS.GetUniqeRegister();
            Emiter.EmitCode($"{leftRegister} = load {Identifier.Type.LLVMName}, {Identifier.Type.LLVMName}* %{Identifier.Name}");
            return leftRegister;
        }
    }

    #endregion Expressions


    #region IF

    public class IfInstruction : InstructionNode, ITypeVisitor
    {
        public IfInstruction(IExpression condition, InstructionNode instruction, InstructionNode elseInstruction = null)
        {
            Condition = condition;
            Instruction = instruction;
            ElseInstruction = elseInstruction;
            if (!condition.Type.Accept(this))
            {
                ParserCS.ReportError("If condition is not a bool type");
            }
        }

        public IExpression Condition { get; }
        public InstructionNode Instruction { get; }
        public InstructionNode ElseInstruction { get; }

        public bool Can(IntType type) => false;

        public bool Can(DoubleType type) => false;

        public bool Can(BoolType type) => true;

        public override string GenCode()
        {
            var condRegister = Condition.GenCode();
            if (ElseInstruction == null)
            {
                Console.WriteLine($"If({condRegister}) jak nie to skok na koniec");
            }
            else
            {
                Console.WriteLine($"If({condRegister}) jak nie to skok do elsa");
            }
            Instruction.GenCode();
            if (ElseInstruction == null)
            {
                Console.WriteLine("Etykieta końcowa ");
            }
            else
            {
                Console.WriteLine("Etykieta do elsa ");
                ElseInstruction.GenCode();
            }

            return null;
        }
    }



    #endregion IF


    #region While
    public class WhileInstruction : InstructionNode, ITypeVisitor
    {
        public WhileInstruction(IExpression condition, InstructionNode instruction)
        {
            Condition = condition;
            Instruction = instruction;
            if (!condition.Type.Accept(this))
            {
                ParserCS.ReportError("While condition is not a bool type");
            }
        }

        public IExpression Condition { get; }
        public InstructionNode Instruction { get; }

        public bool Can(IntType type) => false;

        public bool Can(DoubleType type) => false;

        public bool Can(BoolType type) => true;

        public override string GenCode()
        {
            Console.WriteLine("Etykieta start");
            var condRegister = Condition.GenCode();
            Console.WriteLine($"If({condRegister} == flase) skok do etykiety końcowej");
            Instruction.GenCode();
            Console.WriteLine("Skok do etykiety początkowej ");
            Console.WriteLine("Etykieta końcowa ");


            return null;
        }
    }


    #endregion While


    #region Read

    public class ReadInstruction : InstructionNode,ITypeVisitor
    {
        public ReadInstruction(Identifier identifier)
        {
            Identifier = identifier;
            if(!identifier.Type.Accept(this))
            {
                ParserCS.ReportError("Read instruction expects int or bool identifier");
            }
        }

        public Identifier Identifier { get; }

        public bool Can(IntType type) => true;

        public bool Can(DoubleType type) => true;

        public bool Can(BoolType type) => false;

        public override string GenCode()
        {
            Console.WriteLine($"Read to {Identifier.Name}");

            return null;
        }
    }

    public class ReadHexInstruction : InstructionNode,ITypeVisitor
    {
        public ReadHexInstruction(Identifier identifier)
        {
            Identifier = identifier;
            if (!identifier.Type.Accept(this))
            {
                ParserCS.ReportError("Read HEX instruction expects int identifier");
            }
        }

        public Identifier Identifier { get; }
        public bool Can(IntType type) => true;

        public bool Can(DoubleType type) => false;

        public bool Can(BoolType type) => false;
        public override string GenCode()
        {
            Console.WriteLine($"Read HEX to {Identifier.Name}");

            return null;
        }
    }
    #endregion Read

    #region Write

    public class WriteInstruction : InstructionNode, ITypeVisitor
    {
        public WriteInstruction(Identifier identifier)
        {
            Identifier = identifier;
            if (!identifier.Type.Accept(this))
            {
                ParserCS.ReportError("Write instruction expects int, double or bool identifier");
            }
        }

        public Identifier Identifier { get; }

        public bool Can(IntType type) => true;

        public bool Can(DoubleType type) => true;

        public bool Can(BoolType type) => true;

        public override string GenCode()
        {
            Console.WriteLine($"Write {Identifier.Name}");

            return null;
        }
    }

    public class WriteHexInstruction : InstructionNode, ITypeVisitor
    {
        public WriteHexInstruction(Identifier identifier)
        {
            Identifier = identifier;
            if (!identifier.Type.Accept(this))
            {
                ParserCS.ReportError("Write HEX instruction expects int identifier");
            }
        }

        public Identifier Identifier { get; }
        public bool Can(IntType type) => true;

        public bool Can(DoubleType type) => false;

        public bool Can(BoolType type) => false;
        public override string GenCode()
        {
            Console.WriteLine($"Write HEX {Identifier.Name}");

            return null;
        }
    }

    public class WriteStringInstruction : InstructionNode
    {
        public WriteStringInstruction(string s)
        {
            S = s;
        }

        public Identifier Identifier { get; }
        public string S { get; }
      
        public override string GenCode()
        {
            Console.WriteLine($"Write {S}");

            return null;
        }
    }
    #endregion Write

    public static class Emiter
    {
        public static void EmitProlog()
        {
            EmitCode("; prolog");
            EmitCode("define i32 @main()");
            EmitCode("{");
        }
        public static void EmitEpilog()
        {
            EmitCode("ret i32 0"); 
            EmitCode("}");
        }
        public static StreamWriter sw { get; set; }
        public static void EmitCode(string code)
        {
            sw.WriteLine(code);
        }
    }

    #region Return
    public class ReturnInstruction : InstructionNode
    {
        public override string GenCode()
        {
            Emiter.EmitCode("ret i32 0");
            return null;
        }
    }


    #endregion


    #region Main
    public class Program
    {
        public static int Main(string[] args)
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
                return 1;
            }
            else
            {
                try
                {
                    Emiter.sw = new StreamWriter(fileName + ".ll");
                }
                catch (Exception)
                {
                    return 1;
                }
                Emiter.EmitProlog();
                parser.RootNode.GenCode();
                Emiter.EmitEpilog();
                Emiter.sw.Close();
                Console.WriteLine("Compiled.");
                return 0;
            }

        }
    }
    #endregion Main
}
