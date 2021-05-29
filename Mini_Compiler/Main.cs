using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

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
            registerNumber = 0;
            labelNumber = 0;
            ErrorsCount = 0;
            strings = new List<StringLex>();
        }
        public static int ErrorsCount { get; private set; }
        public static void ReportError(string message)
        {
            ErrorsCount++;
            Console.WriteLine($"Error on line {scanner.LineNumber}: {message}");
        }


        public DeclarationNode MakeDeclaration(IType type, List<string> identifiersNames)
        {
            List<string> names = new List<string>();
            foreach (var identifierName in identifiersNames)
            {
                if (declaredIdentifiers.Any(di => di.OriginalName == identifierName))
                {
                    ReportError($"Variable \"{identifierName}\" already declared");
                }
                else
                {
                    var iden = new Identifier(type, identifierName);
                    declaredIdentifiers.Add(iden); ;
                    names.Add(iden.Name);
                }
            }
            DeclarationNode declarationNode = new DeclarationNode(type, names);
            return declarationNode;
        }

        public Identifier GetIdentifier(string name)
        {
            var ident = declaredIdentifiers.FirstOrDefault(di => di.OriginalName == name);
            if (ident == null)
            {
                ReportError($"Undeclared variable \"{name}\"");
                return new Identifier(IntType.Get, "");
            }
            else
            {
                return ident;
            }
        }

        private readonly HashSet<Identifier> declaredIdentifiers = new HashSet<Identifier>();

        static int registerNumber = 0;
        public static string GetUniqeRegister()
        {
            return "%t" + (++registerNumber).ToString();
        }

        static int labelNumber = 0;
        public static string GetUniqeLabel()
        {
            return "label" + (++labelNumber).ToString();
        }

        public static List<StringLex> strings;
    }
    #endregion


    #region Type

    public interface IType
    {
        string TypeName { get; }
        string LLVMName { get; }
        T Accept<T>(ITypeVisitor<T> visitor);
    }

    public class IntType : IType
    {
        static public IntType Get { get; } = new IntType();
        private IntType() { }
        public string TypeName => "int";
        public string LLVMName => "i32";
        public T Accept<T>(ITypeVisitor<T> visitor) => visitor.Visit(this);
    }

    public class DoubleType : IType
    {
        static public DoubleType Get { get; } = new DoubleType();
        private DoubleType() { }
        public string TypeName => "double";
        public string LLVMName => "double";
        public T Accept<T>(ITypeVisitor<T> visitor) => visitor.Visit(this);
    }

    public class BoolType : IType
    {
        static public BoolType Get { get; } = new BoolType();
        private BoolType() { }
        public string TypeName => "bool";
        public string LLVMName => "i1";
        public T Accept<T>(ITypeVisitor<T> visitor) => visitor.Visit(this);
    }

    #endregion Type

    #region Converters

    public abstract class AConverter
    {
        protected IType _to;
        protected string _fromRegister;
    }
    public class Converter : AConverter, ITypeVisitor<string>
    {
        public string Convert(IType from, IType to, string fromRegister)
        {
            _to = to;
            _fromRegister = fromRegister;
            return from.Accept(this);
        }

        public string Visit(IntType type) => FromIntConverter.Get.Convert(_to, _fromRegister);
        public string Visit(DoubleType type) => FromDoubleConverter.Get.Convert(_to, _fromRegister);
        public string Visit(BoolType type) => FromBoolConverter.Get.Convert(_to, _fromRegister);
    }
    public class FromIntConverter : AConverter, ITypeVisitor<string>
    {
        public static FromIntConverter Get { get; } = new FromIntConverter();
        private FromIntConverter() { }
        public string Convert(IType to, string fromRegister)
        {
            _fromRegister = fromRegister;
            return to.Accept(this);
        }
        public string Visit(IntType type)
        {
            return _fromRegister;
        }

        public string Visit(DoubleType type)
        {
            string register = ParserCS.GetUniqeRegister();
            Emiter.EmitCode($"{register} = sitofp i32 {_fromRegister} to double ");
            return register;
        }

        public string Visit(BoolType type)
        {
            throw new ApplicationException("There is no such conversion");
        }
    }

    public class FromDoubleConverter : AConverter, ITypeVisitor<string>
    {
        public static FromDoubleConverter Get { get; } = new FromDoubleConverter();
        private FromDoubleConverter() { }
        public string Convert(IType to, string fromRegister)
        {
            _fromRegister = fromRegister;
            return to.Accept(this);
        }
        public string Visit(IntType type)
        {
            string register = ParserCS.GetUniqeRegister();
            Emiter.EmitCode($"{register} = fptosi double {_fromRegister} to i32");
            return register;
        }

        public string Visit(DoubleType type)
        {
            return _fromRegister;
        }

        public string Visit(BoolType type)
        {
            throw new ApplicationException("There is no such conversion");
        }
    }

    public class FromBoolConverter : AConverter, ITypeVisitor<string>
    {
        public static FromBoolConverter Get { get; } = new FromBoolConverter();
        private FromBoolConverter() { }
        public string Convert(IType to, string fromRegister)
        {
            _fromRegister = fromRegister;
            return to.Accept(this);
        }
        public string Visit(IntType type)
        {
            string register = ParserCS.GetUniqeRegister();
            Emiter.EmitCode($"{register} = zext i1 {_fromRegister} to i32");
            return register;
        }

        public string Visit(DoubleType type)
        {
            throw new ApplicationException("There is no such conversion");
        }

        public string Visit(BoolType type)
        {
            return _fromRegister;
        }
    }

    public abstract class AImplicit
    {
        protected IType _to;
    }
    public class ImplicitConverter : AImplicit, ITypeVisitor<bool>
    {
        public bool CanConvertImplicitly(IType from, IType to)
        {
            _to = to;
            return from.Accept(this);
        }

        public bool Visit(IntType type) => IntImplicitConverter.Get.CanConvertImplicitly(_to);
        public bool Visit(DoubleType type) => DoubleImplicitConverter.Get.CanConvertImplicitly(_to);
        public bool Visit(BoolType type) => BoolImplicitConverter.Get.CanConvertImplicitly(_to);
    }
    public class IntImplicitConverter : AImplicit, ITypeVisitor<bool>
    {
        public static IntImplicitConverter Get { get; } = new IntImplicitConverter();
        private IntImplicitConverter() { }
        public bool CanConvertImplicitly(IType to)
        {
            return to.Accept(this);
        }

        public bool Visit(IntType type) => true;
        public bool Visit(DoubleType type) => true;
        public bool Visit(BoolType type) => false;
    }
    public class DoubleImplicitConverter : AImplicit, ITypeVisitor<bool>
    {
        public static DoubleImplicitConverter Get { get; } = new DoubleImplicitConverter();
        private DoubleImplicitConverter() { }
        public bool CanConvertImplicitly(IType to)
        {
            return to.Accept(this);
        }

        public bool Visit(IntType type) => false;
        public bool Visit(DoubleType type) => true;
        public bool Visit(BoolType type) => false;
    }
    public class BoolImplicitConverter : AImplicit, ITypeVisitor<bool>
    {
        public static BoolImplicitConverter Get { get; } = new BoolImplicitConverter();
        private BoolImplicitConverter() { }
        public bool CanConvertImplicitly(IType to)
        {
            return to.Accept(this);
        }

        public bool Visit(IntType type) => false;
        public bool Visit(DoubleType type) => false;
        public bool Visit(BoolType type) => true;
    }


    #endregion Converters

    #region Nodes
    public interface INode
    {
        string GenCode();
    }

    public class DeclarationNode : INode
    {
        public IType Type { get; }
        public List<string> Identifiers { get; }

        public DeclarationNode(IType type, List<string> identifiers)
        {
            Type = type;
            Identifiers = identifiers;
        }

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
        public IType Type { get; }
        public string Name { get; }
        public string OriginalName { get; }

        public Identifier(IType type, string name)
        {
            Type = type;
            OriginalName = name;
            Name = "v" + name;
        }

        public string GenCode()
        {
            var register = ParserCS.GetUniqeRegister();
            Emiter.EmitCode($"{register} = load {Type.LLVMName}, {Type.LLVMName}* %{Name}");
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
        public int Value { get; }
        public override IType Type { get => IntType.Get; }

        public IntConstantExpression(string value)
        {
            Value = int.Parse(value, CultureInfo.CreateSpecificCulture("en-US"));
        }

        public override string GenCode()
        {
            return Value.ToString();
        }
    }

    public class DoubleConstantExpression : ConstantExpression
    {
        public double Value { get; }
        public override IType Type { get => DoubleType.Get; }

        public DoubleConstantExpression(string value)
        {
            Value = double.Parse(value, CultureInfo.CreateSpecificCulture("en-US"));
        }

        public override string GenCode()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:0.0###############}", Value);
        }
    }

    public class BoolConstantExpression : ConstantExpression
    {
        public bool Value { get; }
        public override IType Type { get => BoolType.Get; }

        public BoolConstantExpression(bool value)
        {
            Value = value;
        }

        public override string GenCode()
        {
            if (Value)
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


    public interface ITypeVisitor<T>
    {
        T Visit(IntType type);
        T Visit(DoubleType type);
        T Visit(BoolType type);
    }

    #region UnaryExpressions

    public abstract class UnaryExpression : IExpression, ITypeVisitor<bool>
    {
        public UnaryExpression(IExpression expression)
        {
            Expression = expression;
            if (expression.Type.Accept(this) == false)
            {
                ParserCS.ReportError("Wrong type on unary expression");
            }
        }
        public IExpression Expression { get; }

        public abstract IType Type { get; }
        public abstract bool Visit(IntType type);
        public abstract bool Visit(DoubleType type);
        public abstract bool Visit(BoolType type);
        public abstract string GenCode();
    }

    public class UnaryMinusExpression : UnaryExpression, ITypeVisitor<bool>, ITypeVisitor<string>
    {
        public override IType Type => Expression.Type;

        public UnaryMinusExpression(IExpression expression) : base(expression) { }

        public override string GenCode()
        {
            return Expression.Type.Accept<string>(this);
        }
        public override bool Visit(IntType type) => true;
        public override bool Visit(DoubleType type) => true;
        public override bool Visit(BoolType type) => false;

        string ITypeVisitor<string>.Visit(IntType type)
        {
            var exRegister = Expression.GenCode();
            var register = ParserCS.GetUniqeRegister();
            Emiter.EmitCode($"{register} = mul {Type.LLVMName} -1, {exRegister}");
            return register;
        }

        string ITypeVisitor<string>.Visit(DoubleType type)
        {
            var exRegister = Expression.GenCode();
            var register = ParserCS.GetUniqeRegister();
            Emiter.EmitCode($"{register} = fneg {type.LLVMName} {exRegister} ");
            return register;
        }

        string ITypeVisitor<string>.Visit(BoolType type)
        {
            throw new NotImplementedException();
        }
    }

    public class BitNegationExpression : UnaryExpression, ITypeVisitor<bool>
    {
        public override IType Type => Expression.Type;
        public BitNegationExpression(IExpression expression) : base(expression) { }
        public override bool Visit(IntType type) => true;
        public override bool Visit(DoubleType type) => false;
        public override bool Visit(BoolType type) => false;

        public override string GenCode()
        {
            var exRegisterNumber = Expression.GenCode();
            var registerNumber = ParserCS.GetUniqeRegister();
            Emiter.EmitCode($"{registerNumber} = xor {Type.LLVMName} {exRegisterNumber}, -1 ");
            return registerNumber;
        }
    }

    public class LogicNegationExpression : UnaryExpression, ITypeVisitor<bool>
    {
        public override IType Type => Expression.Type;
        public LogicNegationExpression(IExpression expression) : base(expression) { }
        public override bool Visit(IntType type) => false;
        public override bool Visit(DoubleType type) => false;
        public override bool Visit(BoolType type) => true;

        public override string GenCode()
        {
            var exRegister = Expression.GenCode();
            var register = ParserCS.GetUniqeRegister();
            Emiter.EmitCode($"{register} = mul {Type.LLVMName} -1, {exRegister}");
            return register;
        }
    }

    public class IntConversionExpression : UnaryExpression, ITypeVisitor<bool>
    {
        public override IType Type => IntType.Get;
        public IntConversionExpression(IExpression expression) : base(expression) { }

        public override bool Visit(IntType type) => true;
        public override bool Visit(DoubleType type) => true;
        public override bool Visit(BoolType type) => true;

        public override string GenCode()
        {
            var expReg = Expression.GenCode();
            return new Converter().Convert(Expression.Type, this.Type, expReg);
        }
    }

    public class DoubleConversionExpression : UnaryExpression, ITypeVisitor<bool>
    {
        public override IType Type => DoubleType.Get;
        public DoubleConversionExpression(IExpression expression) : base(expression) { }

        public override bool Visit(IntType type) => true;
        public override bool Visit(DoubleType type) => true;
        public override bool Visit(BoolType type) => false;

        public override string GenCode()
        {
            var expReg = Expression.GenCode();
            return new Converter().Convert(Expression.Type, this.Type, expReg);
        }
    }
    #endregion UnaryExpressions

    #region BitsExpressions
    public abstract class BitsExpression : IExpression, ITypeVisitor<bool>
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

        public bool Visit(IntType type) => true;
        public bool Visit(DoubleType type) => false;
        public bool Visit(BoolType type) => false;

        public abstract string GenCode();
    }

    public class BitsOrExpression : BitsExpression
    {
        public BitsOrExpression(IExpression leftExpression, IExpression rightExpression) : base(leftExpression, rightExpression) { }

        public override string GenCode()
        {
            var lExpReg = LeftExpression.GenCode();
            var rExpReg = RightExpression.GenCode();
            var register = ParserCS.GetUniqeRegister();
            Emiter.EmitCode($"{register} = or {Type.LLVMName} {lExpReg}, {rExpReg}");
            return register;
        }
    }
    public class BitAndExpression : BitsExpression
    {
        public BitAndExpression(IExpression leftExpression, IExpression rightExpression) : base(leftExpression, rightExpression) { }

        public override string GenCode()
        {
            var lExpReg = LeftExpression.GenCode();
            var rExpReg = RightExpression.GenCode();
            var register = ParserCS.GetUniqeRegister();
            Emiter.EmitCode($"{register} = and {Type.LLVMName} {lExpReg}, {rExpReg}");
            return register;
        }
    }
    #endregion BitsExpressions

    #region AdditivesMultiplicativeExpressions
    public abstract class AdditivesMultiplicativeExpressions : IExpression, ITypeVisitor<bool>
    {
        public IType Type { get { if (isDouble) return DoubleType.Get; return IntType.Get; } }
        public IExpression LeftExpression { get; }
        public IExpression RightExpression { get; }
        public AdditivesMultiplicativeExpressions(IExpression leftExpression, IExpression rightExpression)
        {
            LeftExpression = leftExpression;
            RightExpression = rightExpression;
            if (leftExpression.Type.Accept(this) == false || rightExpression.Type.Accept(this) == false)
            {
                ParserCS.ReportError("Wrong type on bits expression");
            }
        }

        protected bool isDouble = false;

        public bool Visit(IntType type) => true;
        public bool Visit(DoubleType type)
        {
            isDouble = true;
            return true;
        }
        public bool Visit(BoolType type) => false;

        public string GenCode()
        {
            var lExpReg = LeftExpression.GenCode();
            var rExpReg = RightExpression.GenCode();

            var lTyped = new Converter().Convert(LeftExpression.Type, this.Type, lExpReg);
            var rTyped = new Converter().Convert(RightExpression.Type, this.Type, rExpReg);

            var register = ParserCS.GetUniqeRegister();

            Emiter.EmitCode($"{register} = {GetOperator()} {Type.LLVMName} {lTyped}, {rTyped}");

            return register;
        }

        protected abstract string GetOperator();
    }

    public class Sum : AdditivesMultiplicativeExpressions
    {
        public Sum(IExpression leftExpression, IExpression rightExpression) : base(leftExpression, rightExpression) { }

        protected override string GetOperator()
        {
            if (isDouble) return "fadd";
            return "add";
        }
    }
    public class Subtraction : AdditivesMultiplicativeExpressions
    {
        public Subtraction(IExpression leftExpression, IExpression rightExpression) : base(leftExpression, rightExpression) { }

        protected override string GetOperator()
        {
            if (isDouble) return "fsub";
            return "sub";
        }
    }

    public class Multiplication : AdditivesMultiplicativeExpressions
    {
        public Multiplication(IExpression leftExpression, IExpression rightExpression) : base(leftExpression, rightExpression) { }

        protected override string GetOperator()
        {
            if (isDouble) return "fmul";
            return "mul";
        }
    }

    public class Division : AdditivesMultiplicativeExpressions
    {
        public Division(IExpression leftExpression, IExpression rightExpression) : base(leftExpression, rightExpression) { }

        protected override string GetOperator()
        {
            if (isDouble) return "fdiv";
            return "sdiv";
        }
    }
    #endregion AdditivesMultiplicativeExpressions

    #region RelationsExpressions
    public abstract class RelationsExpression : IExpression, ITypeVisitor<bool>
    {
        public IType Type => BoolType.Get;
        public IExpression LeftExpression { get; }
        public IExpression RightExpression { get; }

        public RelationsExpression(IExpression leftExpression, IExpression rightExpression)
        {
            LeftExpression = leftExpression;
            RightExpression = rightExpression;
        }

        protected bool isDouble = false;
        protected bool isNumber = false;
        protected bool isBool = false;

        public abstract bool Visit(IntType type);
        public abstract bool Visit(DoubleType type);
        public abstract bool Visit(BoolType type);
        protected abstract string GetOperator();

        public string GenCode()
        {
            var lExpReg = LeftExpression.GenCode();
            var rExpReg = RightExpression.GenCode();

            string lTyped = lExpReg;
            string rTyped = rExpReg;

            string type;
            if (isDouble)
            {
                type = DoubleType.Get.LLVMName;
                lTyped = new Converter().Convert(LeftExpression.Type, DoubleType.Get, lExpReg);
                rTyped = new Converter().Convert(RightExpression.Type, DoubleType.Get, rExpReg);
            }
            else if (isNumber)
            {
                type = IntType.Get.LLVMName;
            }
            else
            {
                type = BoolType.Get.LLVMName;
            }
            string cmp = isDouble ? "fcmp" : "icmp";
            var register = ParserCS.GetUniqeRegister();
            Emiter.EmitCode($"{register} = {cmp} {GetOperator()} {type} {lTyped}, {rTyped}");

            return register;
        }
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
        public override bool Visit(IntType type)
        {
            isNumber = true;
            if (isBool) return false;
            return true;
        }
        public override bool Visit(DoubleType type)
        {
            isDouble = true;
            isNumber = true;
            if (isBool) return false;
            return true;
        }
        public override bool Visit(BoolType type)
        {
            isBool = true;
            if (isNumber) return false;
            return true;
        }

    }
    public class EqualsExpression : AllTypeRelationExpression
    {
        public EqualsExpression(IExpression leftExpression, IExpression rightExpression) : base(leftExpression, rightExpression) { }

        protected override string GetOperator()
        {
            if (isDouble) return "oeq";
            return "eq";
        }
    }

    public class NotequalsExpression : AllTypeRelationExpression
    {
        public NotequalsExpression(IExpression leftExpression, IExpression rightExpression) : base(leftExpression, rightExpression) { }

        protected override string GetOperator()
        {
            if (isDouble) return "une";
            return "ne";
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
        public override bool Visit(IntType type)
        {
            isNumber = true;
            return true;
        }
        public override bool Visit(DoubleType type)
        {
            isNumber = true;
            isDouble = true;
            return true;
        }
        public override bool Visit(BoolType type) => false;

    }

    public class GreaterExpression : NumberRelationExpression
    {
        public GreaterExpression(IExpression leftExpression, IExpression rightExpression) : base(leftExpression, rightExpression) { }

        protected override string GetOperator()
        {
            if (isDouble) return "ogt";
            return "sgt";
        }
    }

    public class GreaterOrEqualExpression : NumberRelationExpression
    {
        public GreaterOrEqualExpression(IExpression leftExpression, IExpression rightExpression) : base(leftExpression, rightExpression) { }

        protected override string GetOperator()
        {
            if (isDouble) return "oge";
            return "sge";
        }
    }
    public class LessExpression : NumberRelationExpression
    {
        public LessExpression(IExpression leftExpression, IExpression rightExpression) : base(leftExpression, rightExpression) { }

        protected override string GetOperator()
        {
            if (isDouble) return "olt";
            return "slt";
        }
    }

    public class LessOrEqualExpression : NumberRelationExpression
    {
        public LessOrEqualExpression(IExpression leftExpression, IExpression rightExpression) : base(leftExpression, rightExpression) { }

        protected override string GetOperator()
        {
            if (isDouble) return "ole";
            return "sle";
        }
    }
    #endregion RelationsExpressions

    #region LogicsExpressions
    public abstract class LogicsExpression : IExpression, ITypeVisitor<bool>
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

        public IType Type => BoolType.Get;

        public IExpression LeftExpression { get; }
        public IExpression RightExpression { get; }

        public bool Visit(IntType type) => false;
        public bool Visit(DoubleType type) => false;
        public bool Visit(BoolType type) => true;

        public abstract string GenCode();
    }

    public class AndExpression : LogicsExpression
    {
        public AndExpression(IExpression leftExpression, IExpression rightExpression) : base(leftExpression, rightExpression) { }
        public override string GenCode()
        {
            var labelStart = ParserCS.GetUniqeLabel();
            var labelCondRight = ParserCS.GetUniqeLabel();
            var labelCondRightEnd = ParserCS.GetUniqeLabel();
            var labelAssign = ParserCS.GetUniqeLabel();

            var lExpReg = LeftExpression.GenCode();
            Emiter.EmitCode($"br label %{labelStart}");
            Emiter.EmitCode(labelStart + ":");
            Emiter.EmitCode($"br i1 {lExpReg}, label %{labelCondRight}, label %{labelAssign}");

            Emiter.EmitCode(labelCondRight + ":");
            var rExpReg = RightExpression.GenCode();
            Emiter.EmitCode($"br label %{labelCondRightEnd}");
            Emiter.EmitCode(labelCondRightEnd + ":"); // ponieważ w RightExpression też mogą być etykiety, to dodajemy na koniec ektykietę aby phi zadziałało
            Emiter.EmitCode($"br label %{labelAssign}");


            Emiter.EmitCode(labelAssign + ":");
            var register = ParserCS.GetUniqeRegister();
            Emiter.EmitCode($"{register} = phi {Type.LLVMName} [{lExpReg},%{labelStart}],[{rExpReg},%{labelCondRightEnd}]");
            return register;
        }
    }

    public class OrExpression : LogicsExpression
    {
        public OrExpression(IExpression leftExpression, IExpression rightExpression) : base(leftExpression, rightExpression) { }
        public override string GenCode()
        {

            var labelStart = ParserCS.GetUniqeLabel();
            var labelCondRight = ParserCS.GetUniqeLabel();
            var labelCondRightEnd = ParserCS.GetUniqeLabel();
            var labelAssign = ParserCS.GetUniqeLabel();

            var lExpReg = LeftExpression.GenCode();
            Emiter.EmitCode($"br label %{labelStart}");
            Emiter.EmitCode(labelStart + ":");
            Emiter.EmitCode($"br i1 {lExpReg}, label %{labelAssign}, label %{labelCondRight}");

            Emiter.EmitCode(labelCondRight + ":");
            var rExpReg = RightExpression.GenCode();
            Emiter.EmitCode($"br label %{labelCondRightEnd}");
            Emiter.EmitCode(labelCondRightEnd + ":"); // ponieważ w RightExpression też mogą być etykiety, to dodajemy na koniec ektykietę aby phi zadziałało
            Emiter.EmitCode($"br label %{labelAssign}");


            Emiter.EmitCode(labelAssign + ":");
            var register = ParserCS.GetUniqeRegister();
            Emiter.EmitCode($"{register} = phi {Type.LLVMName} [{lExpReg},%{labelStart}],[{rExpReg},%{labelCondRightEnd}]");
            return register;
        }
    }
    #endregion LogicsExpressions


    public class AssignExpression : IExpression
    {
        public IType Type => (Identifier.Type);
        public Identifier Identifier { get; }
        public IExpression Expression { get; }

        public AssignExpression(Identifier identifier, IExpression expression)
        {
            Identifier = identifier;
            Expression = expression;
            if (!new ImplicitConverter().CanConvertImplicitly(expression.Type, Identifier.Type))
            {
                ParserCS.ReportError($"Cannot assign {expression.Type.TypeName} to {identifier.Type.TypeName}");
            }
        }

        public string GenCode()
        {
            var rightRegister = Expression.GenCode();
            var rightRegisterTyped = new Converter().Convert(Expression.Type, Identifier.Type, rightRegister);
            Emiter.EmitCode($"store {Identifier.Type.LLVMName} {rightRegisterTyped}, {Identifier.Type.LLVMName}* %{Identifier.Name}");
            var leftRegister = ParserCS.GetUniqeRegister();
            Emiter.EmitCode($"{leftRegister} = load {Identifier.Type.LLVMName}, {Identifier.Type.LLVMName}* %{Identifier.Name}");
            return leftRegister;
        }
    }

    #endregion Expressions


    #region IF

    public class IfInstruction : InstructionNode, ITypeVisitor<bool>
    {
        public IExpression Condition { get; }
        public InstructionNode Instruction { get; }
        public InstructionNode ElseInstruction { get; }

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

        public bool Visit(IntType type) => false;
        public bool Visit(DoubleType type) => false;
        public bool Visit(BoolType type) => true;

        public override string GenCode()
        {
            var labelThen = ParserCS.GetUniqeLabel();
            var labelEnd = ParserCS.GetUniqeLabel();
            var lExpReg = Condition.GenCode();
            if (ElseInstruction == null)
            {
                Emiter.EmitCode($"br i1 {lExpReg}, label %{labelThen}, label %{labelEnd}");
                Emiter.EmitCode(labelThen + ":");
                Instruction.GenCode();
                Emiter.EmitCode($"br label %{labelEnd}");
                Emiter.EmitCode(labelEnd + ":");
            }
            else
            {
                var labelElse = ParserCS.GetUniqeLabel();
                Emiter.EmitCode($"br i1 {lExpReg}, label %{labelThen}, label %{labelElse}");
                Emiter.EmitCode(labelThen + ":");
                Instruction.GenCode();
                Emiter.EmitCode($"br label %{labelEnd}");
                Emiter.EmitCode(labelElse + ":");
                ElseInstruction.GenCode();
                Emiter.EmitCode($"br label %{labelEnd}");
                Emiter.EmitCode(labelEnd + ":");
            }

            return null;
        }
    }


    #endregion IF


    #region While
    public class WhileInstruction : InstructionNode, ITypeVisitor<bool>
    {
        public IExpression Condition { get; }
        public InstructionNode Instruction { get; }

        public WhileInstruction(IExpression condition, InstructionNode instruction)
        {
            Condition = condition;
            Instruction = instruction;
            if (!condition.Type.Accept(this))
            {
                ParserCS.ReportError("While condition is not a bool type");
            }
        }

        public bool Visit(IntType type) => false;
        public bool Visit(DoubleType type) => false;
        public bool Visit(BoolType type) => true;

        public override string GenCode()
        {
            var labelStart = ParserCS.GetUniqeLabel();
            var labelThen = ParserCS.GetUniqeLabel();
            var labelEnd = ParserCS.GetUniqeLabel();

            Emiter.EmitCode($"br label %{labelStart}");
            Emiter.EmitCode(labelStart + ":");
            var lExpReg = Condition.GenCode();
            Emiter.EmitCode($"br i1 {lExpReg}, label %{labelThen}, label %{labelEnd}");
            Emiter.EmitCode(labelThen + ":");
            Instruction.GenCode();
            Emiter.EmitCode($"br label %{labelStart}");
            Emiter.EmitCode(labelEnd + ":");

            return null;
        }
    }


    #endregion While


    #region Read

    public class ReadInstruction : InstructionNode, ITypeVisitor<bool>, ITypeVisitor<string>
    {
        public Identifier Identifier { get; }

        public ReadInstruction(Identifier identifier)
        {
            Identifier = identifier;
            if (!identifier.Type.Accept<bool>(this))
            {
                ParserCS.ReportError("Read instruction expects int or bool identifier");
            }
        }

        public bool Visit(IntType type) => true;
        public bool Visit(DoubleType type) => true;
        public bool Visit(BoolType type) => false;

        string ITypeVisitor<string>.Visit(IntType type)
        {
            Emiter.EmitCode($"call i32 (i8*, ...) @scanf(i8* bitcast ([3 x i8]* @int_res to i8*), i32* %{Identifier.Name})");
            return null;
        }

        string ITypeVisitor<string>.Visit(DoubleType type)
        {
            Emiter.EmitCode($"call i32 (i8*, ...) @scanf(i8* bitcast ([4 x i8]* @double_res to i8*), double* %{Identifier.Name})");
            return null;
        }

        string ITypeVisitor<string>.Visit(BoolType type)
        {
            throw new NotImplementedException();
        }
        public override string GenCode()
        {
            Identifier.Type.Accept<string>(this);
            return null;
        }
    }

    public class ReadHexInstruction : InstructionNode, ITypeVisitor<bool>
    {
        public Identifier Identifier { get; }

        public ReadHexInstruction(Identifier identifier)
        {
            Identifier = identifier;
            if (!identifier.Type.Accept(this))
            {
                ParserCS.ReportError("Read HEX instruction expects int identifier");
            }
        }

        public bool Visit(IntType type) => true;
        public bool Visit(DoubleType type) => false;
        public bool Visit(BoolType type) => false;

        public override string GenCode()
        {
            Emiter.EmitCode($"call i32 (i8*, ...) @scanf(i8* bitcast ([5 x i8]* @hex_res to i8*), i32* %{Identifier.Name})");
            return null;
        }
    }
    #endregion Read

    #region Write

    public class WriteInstruction : InstructionNode, ITypeVisitor<bool>, ITypeVisitor<string>
    {
        public IExpression Expression { get; }

        public WriteInstruction(IExpression expression)
        {
            if (!expression.Type.Accept<bool>(this))
            {
                ParserCS.ReportError("Write instruction expects int, double or bool identifier");
            }
            Expression = expression;
        }

        public bool Visit(IntType type) => true;
        public bool Visit(DoubleType type) => true;
        public bool Visit(BoolType type) => true;

        string ITypeVisitor<string>.Visit(IntType type)
        {
            var reg = Expression.GenCode();
            Emiter.EmitCode($"call i32 (i8*, ...) @printf(i8* bitcast ([3 x i8]* @int_res to i8*), i32 {reg})");
            return null;
        }

        string ITypeVisitor<string>.Visit(DoubleType type)
        {
            var reg = Expression.GenCode();
            Emiter.EmitCode($"call i32 (i8*, ...) @printf(i8* bitcast ([4 x i8]* @double_res to i8*), double {reg})");
            return null;
        }

        string ITypeVisitor<string>.Visit(BoolType type)
        {
            var labelTrue = ParserCS.GetUniqeLabel();
            var labelFalse = ParserCS.GetUniqeLabel();
            var labelEnd = ParserCS.GetUniqeLabel();
            var reg = Expression.GenCode();
            Emiter.EmitCode($"br i1 {reg}, label %{labelTrue}, label %{labelFalse}");

            Emiter.EmitCode($"{labelTrue}:");
            Emiter.EmitCode($"call i32 (i8*, ...) @printf(i8* bitcast ([5 x i8]* @true_res to i8*))");
            Emiter.EmitCode($"br label %{labelEnd}");

            Emiter.EmitCode($"{labelFalse}:");
            Emiter.EmitCode($"call i32 (i8*, ...) @printf(i8* bitcast ([6 x i8]* @false_res to i8*))");
            Emiter.EmitCode($"br label %{labelEnd}");

            Emiter.EmitCode($"{labelEnd}:");
            return null;
        }

        public override string GenCode()
        {
            Expression.Type.Accept<string>(this);
            return null;
        }
    }

    public class WriteHexInstruction : InstructionNode, ITypeVisitor<bool>
    {
        public IExpression Expression { get; }

        public WriteHexInstruction(IExpression expression)
        {
            Expression = expression;
            if (!expression.Type.Accept(this))
            {
                ParserCS.ReportError("Write HEX instruction expects int identifier");
            }
        }

        public bool Visit(IntType type) => true;
        public bool Visit(DoubleType type) => false;
        public bool Visit(BoolType type) => false;

        public override string GenCode()
        {
            var reg = Expression.GenCode();
            Emiter.EmitCode($"call i32 (i8*, ...) @printf(i8* bitcast ([5 x i8]* @hex_res to i8*), i32 {reg})");

            return null;
        }
    }

    public class StringLex
    {
        public string InnerString { get; }
        public string ConstName { get; }
        public int LexLength
        {
            get
            {
                int slashesCount = InnerString.Count(c => c == '\\');
                return InnerString.Length + 1 - slashesCount * 2;
            }
        }

        public StringLex(string s)
        {
            s = s.Substring(1, s.Length - 2);
            s = s.Replace("\\", "\\\\");
            s = s.Replace("\\\\\\\\", "\\5C");
            s = s.Replace("\\\\n", "\n");
            s = s.Replace("\\\\\"", "\\22");
            s = s.Replace("\\\\", "");
            InnerString = s;
            ConstName = "str" + (++count).ToString();
            ParserCS.strings.Add(this);
        }

        private static int count = 0;
    }

    public class WriteStringInstruction : InstructionNode
    {
        public StringLex String { get; }

        public WriteStringInstruction(StringLex s)
        {
            String = s;
        }

        public override string GenCode()
        {
            Emiter.EmitCode($"call i32 (i8*, ...) @printf(i8* bitcast ([{String.LexLength} x i8]* @{String.ConstName} to i8*))");

            return null;
        }
    }
    #endregion Write

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

    public static class Emiter
    {
        public static StreamWriter SW { get; set; }

        public static void EmitProlog()
        {
            EmitCode("; prolog");
            EmitCode("@int_res = constant [3 x i8] c\"%d\\00\"");
            EmitCode("@double_res = constant [4 x i8] c\"%lf\\00\"");
            EmitCode("@hex_res = constant [5 x i8] c\"0X%X\\00\"");
            EmitCode("@true_res = constant [5 x i8] c\"True\\00\"");
            EmitCode("@false_res = constant [6 x i8] c\"False\\00\"");
            foreach (var s in ParserCS.strings)
            {
                EmitCode($"@{s.ConstName} = constant[{s.LexLength} x i8] c\"{s.InnerString}\\00\"");
            }
            EmitCode("declare i32 @printf(i8* noalias nocapture, ...)");
            EmitCode("declare i32 @scanf(i8* noalias nocapture, ...)");
            EmitCode("define i32 @main()");
            EmitCode("{");
        }
        public static void EmitEpilog()
        {
            EmitCode("ret i32 0");
            EmitCode("}");
        }
        public static void EmitCode(string code)
        {
            SW.WriteLine(code);
        }
    }


    #region Main
    public class Program
    {
        public static int Main(string[] args)
        {
            string fileName;
            if (args.Length == 0)
            {
                fileName = Console.ReadLine();
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
                    Emiter.SW = new StreamWriter(fileName + ".ll");
                }
                catch (Exception)
                {
                    return 1;
                }
                Emiter.EmitProlog();
                parser.RootNode.GenCode();
                Emiter.EmitEpilog();
                Emiter.SW.Close();
                Console.WriteLine("Compiled.");
                return 0;
            }

        }
    }
    #endregion Main
}
