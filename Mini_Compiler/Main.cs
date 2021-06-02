using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Mini_Compiler
{
    public class ParserCS
    {
        public static void Reset()
        {
            registerNumber = 0;
            labelNumber = 0;
            variableNumber = 0;
            ErrorsCount = 0;
            strings = new List<StringLex>();
        }
        public static int ErrorsCount { get; private set; }
        public static void ReportError(string message, int lineNumber)
        {
            ErrorsCount++;
            Console.WriteLine($"Error on line {lineNumber}: {message}");
        }

        static int registerNumber = 0;
        public static string GetUniqeRegister()
        {
            return "%t" + (++registerNumber).ToString();
        }

        static int variableNumber = 0;
        public static string GetUniqeVariable()
        {
            return "%v" + (++variableNumber).ToString();
        }

        static int labelNumber = 0;
        public static string GetUniqeLabel()
        {
            return "label" + (++labelNumber).ToString();
        }

        public static List<StringLex> strings = new List<StringLex>();
    }


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

    public interface INode
    {
        BlockInstructionNode Parent { get; set; }
        string GenCode();
        void SecondRun();
    }

    #region Instructions
    public abstract class InstructionNode : INode
    {
        public int LineNumber = Scanner.LineNumber;
        public BlockInstructionNode Parent { get; set; }
        public WhileInstruction ParentLoop { get; set; }

        public abstract void SecondRun();
        public abstract string GenCode();
    }
    public class BlockInstructionNode : InstructionNode
    {
        public List<DeclarationNode> Declarations { get; }
        public List<InstructionNode> Instructions { get; }
        public IEnumerable<Identifier> Identifiers
        {
            get
            {
                IEnumerable<Identifier> ret = SimpleIdentifiers;
                return ret.Concat(ArrayIdentifiers);
            }
        }

        public List<SimpleIdentifier> SimpleIdentifiers { get; set; } = new List<SimpleIdentifier>();
        public List<ArrayIdentifier> ArrayIdentifiers { get; set; } = new List<ArrayIdentifier>();

        public BlockInstructionNode(List<DeclarationNode> declarations, List<InstructionNode> instructions)
        {
            Declarations = declarations;
            Instructions = instructions;
        }

        public SimpleIdentifier GetSimpleIdentifier(string originalName)
        {
            BlockInstructionNode p = this;
            while (p != null)
            {
                var ident = p.SimpleIdentifiers.Find(i => i.OriginalName == originalName);
                if (ident != null)
                {
                    return ident;
                }
                p = p.Parent;
            }

            ParserCS.ReportError($"undeclared variable \"{originalName}\"", LineNumber);
            return null;
        }

        public ArrayIdentifier GetArrayIdentifier(string originalName)
        {
            BlockInstructionNode p = this;
            while (p != null)
            {
                var ident = p.ArrayIdentifiers.Find(i => i.OriginalName == originalName);
                if (ident != null)
                {
                    return ident;
                }
                p = p.Parent;
            }

            ParserCS.ReportError($"undeclared array variable \"{originalName}\"", LineNumber);
            return null;
        }

        public override void SecondRun()
        {
            foreach (var dec in Declarations)
            {
                dec.Parent = this;
            }
            foreach (var ins in Instructions)
            {
                ins.Parent = this;
            }

            foreach (var dec in Declarations)
            {
                dec.SecondRun();
            }
            foreach (var ins in Instructions)
            {
                ins.SecondRun();
            }
        }

        public override string GenCode()
        {
            foreach (var ident in ArrayIdentifiers)
            {
                Emiter.EmitCode($"{ident.Name} = alloca {ident.Type.LLVMName}, i32 {ident.GetFillSize}");
            }
            foreach (var ident in SimpleIdentifiers)
            {
                Emiter.EmitCode($"{ident.Name} = alloca {ident.Type.LLVMName}");
            }
            foreach (var instruction in Instructions)
            {
                instruction.GenCode();
            }
            return null;
        }
    }


    #endregion Instructions


    #region Declarations
    public class DeclarationNode : INode
    {
        public int LineNumber = Scanner.LineNumber;
        public IType Type { get; }
        public Identifiers Identifiers { get; }
        public BlockInstructionNode Parent { get; set; }

        public DeclarationNode(IType type, Identifiers identifiers)
        {
            Type = type;
            Identifiers = identifiers;
        }

        public void SecondRun()
        {
            foreach (var ident in Identifiers.ArrayIdentifiers)
            {
                if (Parent.Identifiers.Any(id => id.OriginalName == ident.name))
                {
                    ParserCS.ReportError("Variable \"{identifierName}\" already declared", LineNumber);
                }
                string name = ParserCS.GetUniqeVariable() + ident.name;
                var identifier = new ArrayIdentifier(ident.name, name, Type, ident.size.Select(s => int.Parse(s)).ToList());
                Parent.ArrayIdentifiers.Add(identifier);
            }
            foreach (var ident in Identifiers.SimpleIdentifiers)
            {
                if (Parent.Identifiers.Any(id => id.OriginalName == ident))
                {
                    ParserCS.ReportError("Variable \"{identifierName}\" already declared", LineNumber);
                }
                string name = ParserCS.GetUniqeVariable() + ident;
                var identifier = new SimpleIdentifier(ident, name, Type);

                Parent.SimpleIdentifiers.Add(identifier);
            }

        }

        public string GenCode()
        {
            return null;
        }
    }

    public class Identifiers
    {
        public List<(string name, List<string> size)> ArrayIdentifiers { get; set; } = new List<(string name, List<string> size)>();
        public List<string> SimpleIdentifiers { get; set; } = new List<string>();
    }


    public abstract class ExpressionNode : InstructionNode
    {
        public IType Type { get; protected set; }
    }


    #endregion Declarations

    #region Identifiers
    public abstract class Identifier : ExpressionNode
    {
        public string Name { get; set; }
        public string OriginalName { get; }
        public abstract T Accept<T>(IIdentifierVisitor<T> visitor);
        public Identifier(string originalName)
        {
            OriginalName = originalName;
        }
    }

    public class SimpleIdentifier : Identifier
    {
        public SimpleIdentifier(string originalName) : base(originalName) { }
        public SimpleIdentifier(string originalName, string name, IType type) : base(originalName)
        {
            Name = name;
            Type = type;
        }

        public override T Accept<T>(IIdentifierVisitor<T> visitor) => visitor.Visit(this);

        public override void SecondRun()
        {
            var ident = Parent.GetSimpleIdentifier(OriginalName); if (ident == null)
            {
                Name = "";
                Type = IntType.Get;
            }
            else
            {
                Name = ident.Name;
                Type = ident.Type;
            }

        }

        public override string GenCode()
        {
            var register = ParserCS.GetUniqeRegister();
            Emiter.EmitCode($"{register} = load {Type.LLVMName}, {Type.LLVMName}* {Name}");
            return register;
        }
    }

    public class ArrayIdentifier : Identifier, ITypeVisitor<bool>
    {
        public List<int> Sizes { get; set; }
        public int GetFillSize => Sizes.Aggregate(1, (int a, int b) => a * b);
        public List<ExpressionNode> Expressions { get; }

        public ArrayIdentifier(string originalName, string name, IType type, List<int> sizes) : base(originalName)
        {
            Name = name;
            Type = type;
            Sizes = sizes;
            foreach (var size in sizes)
            {
                if (size < 0)
                {
                    ParserCS.ReportError("Array size must be positive", LineNumber);
                }
            }
        }
        public override T Accept<T>(IIdentifierVisitor<T> visitor) => visitor.Visit(this);

        public ArrayIdentifier(string originalName, List<ExpressionNode> expression) : base(originalName)
        {
            Expressions = expression;
        }
        private int GetSizeFrom(int i)
        {
            int res = 1;
            while (i < Sizes.Count)
            {
                res *= Sizes[i];
                i++;
            }
            return res;
        }
        public bool Visit(IntType type) => true;
        public bool Visit(DoubleType type) => false;
        public bool Visit(BoolType type) => false;

        public override void SecondRun()
        {
            var ident = Parent.GetArrayIdentifier(OriginalName);
            if (ident == null)
            {
                Name = "";
                Type = IntType.Get;
                Sizes = new List<int> { 1 };
            }
            else
            {
                Name = ident.Name;
                Type = ident.Type;
                Sizes = ident.Sizes;
            }
            if (Sizes.Count != Expressions.Count)
            {
                ParserCS.ReportError($"Expected {Sizes.Count} indexes but got {Expressions.Count}", LineNumber);
            }
            foreach (var exp in Expressions)
            {
                exp.Parent = this.Parent;
                exp.SecondRun();
                if (exp.Type.Accept(this) == false)
                {
                    ParserCS.ReportError("Array index must be int type", LineNumber);
                }
            }
        }

        public string GenPointerCode()
        {
            var registerPointer = ParserCS.GetUniqeRegister();
            string sumReg = "0";
            for (int i = 0; i < Expressions.Count; i++)
            {
                var exp = Expressions[i];
                var expReg = exp.GenCode();
                string multReg = ParserCS.GetUniqeRegister();
                Emiter.EmitCode($"{multReg} = mul i32 {expReg}, {GetSizeFrom(i + 1)}");

                string newSumReg = ParserCS.GetUniqeRegister();
                Emiter.EmitCode($"{newSumReg} = add i32 {sumReg}, {multReg}");
                sumReg = newSumReg;

            }
            Emiter.EmitCode($"{registerPointer} = getelementptr inbounds {Type.LLVMName}, {Type.LLVMName}* {Name}, i32 {sumReg}");
            return registerPointer;
        }

        public override string GenCode()
        {
            var registerPointer = GenPointerCode();
            var register = ParserCS.GetUniqeRegister();
            Emiter.EmitCode($"{register} = load {Type.LLVMName}, {Type.LLVMName}* {registerPointer}");

            return register;
        }
    }


    #endregion


    #region Expressions

    #region ConstantExpressions
    public abstract class ConstantExpression : ExpressionNode
    {
        public override void SecondRun()
        {
            return;
        }
    }
    public class IntConstantExpression : ConstantExpression
    {
        public int Value { get; }

        public IntConstantExpression(string value, bool hex = false)
        {
            if (hex)
            {
                Value = Convert.ToInt32(value, 16);
            }
            else
            {
                Value = int.Parse(value, CultureInfo.CreateSpecificCulture("en-US"));
            }

            Type = IntType.Get;
        }

        public override string GenCode()
        {
            return Value.ToString();
        }
    }

    public class DoubleConstantExpression : ConstantExpression
    {
        public double Value { get; }

        public DoubleConstantExpression(string value)
        {
            Value = double.Parse(value, CultureInfo.CreateSpecificCulture("en-US"));
            Type = DoubleType.Get;
        }

        public override string GenCode()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:0.0###############}", Value);
        }
    }

    public class BoolConstantExpression : ConstantExpression
    {
        public bool Value { get; }

        public BoolConstantExpression(bool value)
        {
            Value = value;
            Type = BoolType.Get;
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

    public abstract class UnaryExpression : ExpressionNode, ITypeVisitor<bool>
    {
        public ExpressionNode Expression { get; }
        public UnaryExpression(ExpressionNode expression)
        {
            Expression = expression;
        }

        public abstract bool Visit(IntType type);
        public abstract bool Visit(DoubleType type);
        public abstract bool Visit(BoolType type);

        public override void SecondRun()
        {
            Expression.Parent = this.Parent;
            Expression.SecondRun();
            if (Expression.Type.Accept(this) == false)
            {
                ParserCS.ReportError("Wrong type on unary expression", LineNumber);
            }
        }
    }

    public class UnaryMinusExpression : UnaryExpression, ITypeVisitor<bool>, ITypeVisitor<string>
    {
        public UnaryMinusExpression(ExpressionNode expression) : base(expression) { }

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

        public override void SecondRun()
        {
            base.SecondRun();
            Type = Expression.Type;
        }

        public override string GenCode()
        {
            return Expression.Type.Accept<string>(this);
        }
    }

    public class BitNegationExpression : UnaryExpression, ITypeVisitor<bool>
    {
        public BitNegationExpression(ExpressionNode expression) : base(expression) { }
        public override bool Visit(IntType type) => true;
        public override bool Visit(DoubleType type) => false;
        public override bool Visit(BoolType type) => false;

        public override void SecondRun()
        {
            base.SecondRun();
            Type = Expression.Type;
        }
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
        public LogicNegationExpression(ExpressionNode expression) : base(expression) { }
        public override bool Visit(IntType type) => false;
        public override bool Visit(DoubleType type) => false;
        public override bool Visit(BoolType type) => true;

        public override void SecondRun()
        {
            base.SecondRun();
            Type = Expression.Type;
        }

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
        public IntConversionExpression(ExpressionNode expression) : base(expression) { Type = IntType.Get; }

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
        public DoubleConversionExpression(ExpressionNode expression) : base(expression) { Type = DoubleType.Get; }

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
    public abstract class BitsExpression : ExpressionNode, ITypeVisitor<bool>
    {
        public ExpressionNode LeftExpression { get; }
        public ExpressionNode RightExpression { get; }
        public BitsExpression(ExpressionNode leftExpression, ExpressionNode rightExpression)
        {
            LeftExpression = leftExpression;
            RightExpression = rightExpression;
            Type = IntType.Get;
        }

        public bool Visit(IntType type) => true;
        public bool Visit(DoubleType type) => false;
        public bool Visit(BoolType type) => false;

        public override void SecondRun()
        {
            LeftExpression.Parent = this.Parent;
            RightExpression.Parent = this.Parent;
            LeftExpression.SecondRun();
            RightExpression.SecondRun();
            if (LeftExpression.Type.Accept(this) == false || RightExpression.Type.Accept(this) == false)
            {
                ParserCS.ReportError("Wrong type on bits expression", LineNumber);
            }
        }
    }

    public class BitsOrExpression : BitsExpression
    {
        public BitsOrExpression(ExpressionNode leftExpression, ExpressionNode rightExpression) : base(leftExpression, rightExpression) { }

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
        public BitAndExpression(ExpressionNode leftExpression, ExpressionNode rightExpression) : base(leftExpression, rightExpression) { }

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
    public abstract class AdditivesMultiplicativeExpressions : ExpressionNode, ITypeVisitor<bool>
    {
        public ExpressionNode LeftExpression { get; }
        public ExpressionNode RightExpression { get; }
        public AdditivesMultiplicativeExpressions(ExpressionNode leftExpression, ExpressionNode rightExpression)
        {
            LeftExpression = leftExpression;
            RightExpression = rightExpression;

        }
        protected abstract string GetOperator();

        protected bool isDouble = false;

        public bool Visit(IntType type) => true;
        public bool Visit(DoubleType type)
        {
            isDouble = true;
            return true;
        }
        public bool Visit(BoolType type) => false;

        public override void SecondRun()
        {
            LeftExpression.Parent = this.Parent;
            RightExpression.Parent = this.Parent;
            LeftExpression.SecondRun();
            RightExpression.SecondRun();
            if (LeftExpression.Type.Accept(this) == false || RightExpression.Type.Accept(this) == false)
            {
                ParserCS.ReportError("Wrong type on bits expression", LineNumber);
            }
            if (isDouble) Type = DoubleType.Get;
            else Type = IntType.Get;
        }


        public override string GenCode()
        {
            var lExpReg = LeftExpression.GenCode();
            var rExpReg = RightExpression.GenCode();

            var lTyped = new Converter().Convert(LeftExpression.Type, this.Type, lExpReg);
            var rTyped = new Converter().Convert(RightExpression.Type, this.Type, rExpReg);

            var register = ParserCS.GetUniqeRegister();

            Emiter.EmitCode($"{register} = {GetOperator()} {Type.LLVMName} {lTyped}, {rTyped}");

            return register;
        }
    }

    public class Sum : AdditivesMultiplicativeExpressions
    {
        public Sum(ExpressionNode leftExpression, ExpressionNode rightExpression) : base(leftExpression, rightExpression) { }

        protected override string GetOperator()
        {
            if (isDouble) return "fadd";
            return "add";
        }
    }
    public class Subtraction : AdditivesMultiplicativeExpressions
    {
        public Subtraction(ExpressionNode leftExpression, ExpressionNode rightExpression) : base(leftExpression, rightExpression) { }

        protected override string GetOperator()
        {
            if (isDouble) return "fsub";
            return "sub";
        }
    }

    public class Multiplication : AdditivesMultiplicativeExpressions
    {
        public Multiplication(ExpressionNode leftExpression, ExpressionNode rightExpression) : base(leftExpression, rightExpression) { }

        protected override string GetOperator()
        {
            if (isDouble) return "fmul";
            return "mul";
        }
    }

    public class Division : AdditivesMultiplicativeExpressions
    {
        public Division(ExpressionNode leftExpression, ExpressionNode rightExpression) : base(leftExpression, rightExpression) { }

        protected override string GetOperator()
        {
            if (isDouble) return "fdiv";
            return "sdiv";
        }
    }
    #endregion AdditivesMultiplicativeExpressions

    #region RelationsExpressions
    public abstract class RelationsExpression : ExpressionNode, ITypeVisitor<bool>
    {
        public ExpressionNode LeftExpression { get; }
        public ExpressionNode RightExpression { get; }

        public RelationsExpression(ExpressionNode leftExpression, ExpressionNode rightExpression)
        {
            LeftExpression = leftExpression;
            RightExpression = rightExpression;
            Type = BoolType.Get;
        }
        protected abstract string GetOperator();

        protected bool isDouble = false;
        protected bool isNumber = false;
        protected bool isBool = false;

        public abstract bool Visit(IntType type);
        public abstract bool Visit(DoubleType type);
        public abstract bool Visit(BoolType type);

        public override void SecondRun()
        {
            LeftExpression.Parent = this.Parent;
            RightExpression.Parent = this.Parent;
            LeftExpression.SecondRun();
            RightExpression.SecondRun();
            if (LeftExpression.Type.Accept(this) == false || RightExpression.Type.Accept(this) == false)
            {
                ParserCS.ReportError("Wrong type on relation expression", LineNumber);
            }
        }

        public override string GenCode()
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
        public AllTypeRelationExpression(ExpressionNode leftExpression, ExpressionNode rightExpression) : base(leftExpression, rightExpression)
        {

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
        public EqualsExpression(ExpressionNode leftExpression, ExpressionNode rightExpression) : base(leftExpression, rightExpression) { }

        protected override string GetOperator()
        {
            if (isDouble) return "oeq";
            return "eq";
        }
    }

    public class NotequalsExpression : AllTypeRelationExpression
    {
        public NotequalsExpression(ExpressionNode leftExpression, ExpressionNode rightExpression) : base(leftExpression, rightExpression) { }

        protected override string GetOperator()
        {
            if (isDouble) return "une";
            return "ne";
        }
    }

    public abstract class NumberRelationExpression : RelationsExpression
    {
        public NumberRelationExpression(ExpressionNode leftExpression, ExpressionNode rightExpression) : base(leftExpression, rightExpression)
        {

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
        public GreaterExpression(ExpressionNode leftExpression, ExpressionNode rightExpression) : base(leftExpression, rightExpression) { }

        protected override string GetOperator()
        {
            if (isDouble) return "ogt";
            return "sgt";
        }
    }

    public class GreaterOrEqualExpression : NumberRelationExpression
    {
        public GreaterOrEqualExpression(ExpressionNode leftExpression, ExpressionNode rightExpression) : base(leftExpression, rightExpression) { }

        protected override string GetOperator()
        {
            if (isDouble) return "oge";
            return "sge";
        }
    }
    public class LessExpression : NumberRelationExpression
    {
        public LessExpression(ExpressionNode leftExpression, ExpressionNode rightExpression) : base(leftExpression, rightExpression) { }

        protected override string GetOperator()
        {
            if (isDouble) return "olt";
            return "slt";
        }
    }

    public class LessOrEqualExpression : NumberRelationExpression
    {
        public LessOrEqualExpression(ExpressionNode leftExpression, ExpressionNode rightExpression) : base(leftExpression, rightExpression) { }

        protected override string GetOperator()
        {
            if (isDouble) return "ole";
            return "sle";
        }
    }
    #endregion RelationsExpressions

    #region LogicsExpressions
    public abstract class LogicsExpression : ExpressionNode, ITypeVisitor<bool>
    {
        public ExpressionNode LeftExpression { get; }
        public ExpressionNode RightExpression { get; }
        public LogicsExpression(ExpressionNode leftExpression, ExpressionNode rightExpression)
        {
            LeftExpression = leftExpression;
            RightExpression = rightExpression;
            Type = BoolType.Get;
        }

        public bool Visit(IntType type) => false;
        public bool Visit(DoubleType type) => false;
        public bool Visit(BoolType type) => true;

        public override void SecondRun()
        {
            LeftExpression.Parent = this.Parent;
            RightExpression.Parent = this.Parent;
            LeftExpression.SecondRun();
            RightExpression.SecondRun();
            if (LeftExpression.Type.Accept(this) == false || RightExpression.Type.Accept(this) == false)
            {
                ParserCS.ReportError("Wrong type on logic expression", LineNumber);
            }
        }
    }

    public class AndExpression : LogicsExpression
    {
        public AndExpression(ExpressionNode leftExpression, ExpressionNode rightExpression) : base(leftExpression, rightExpression) { }
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
        public OrExpression(ExpressionNode leftExpression, ExpressionNode rightExpression) : base(leftExpression, rightExpression) { }
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

    public interface IIdentifierVisitor<T>
    {
        T Visit(SimpleIdentifier identifier);
        T Visit(ArrayIdentifier identifier);
    }

    public class AssignExpression : ExpressionNode, IIdentifierVisitor<string>
    {
        public Identifier Identifier { get; }
        public ExpressionNode Expression { get; }

        public AssignExpression(Identifier identifier, ExpressionNode expression)
        {
            Identifier = identifier;
            Expression = expression;
        }


        public override void SecondRun()
        {
            Identifier.Parent = this.Parent;
            Expression.Parent = this.Parent;
            Identifier.SecondRun();
            Expression.SecondRun();
            Type = Identifier.Type;
            if (!new ImplicitConverter().CanConvertImplicitly(Expression.Type, Identifier.Type))
            {
                ParserCS.ReportError($"Cannot assign {Expression.Type.TypeName} to {Identifier.Type.TypeName}", LineNumber);
            }
        }

        public string Visit(SimpleIdentifier identifier)
        {
            var rightRegister = Expression.GenCode();
            var rightRegisterTyped = new Converter().Convert(Expression.Type, identifier.Type, rightRegister);
            Emiter.EmitCode($"store {identifier.Type.LLVMName} {rightRegisterTyped}, {identifier.Type.LLVMName}* {identifier.Name}");
            var leftRegister = ParserCS.GetUniqeRegister();
            Emiter.EmitCode($"{leftRegister} = load {identifier.Type.LLVMName}, {identifier.Type.LLVMName}* {identifier.Name}");
            return leftRegister;
        }

        public string Visit(ArrayIdentifier identifier)
        {
            var rightRegister = Expression.GenCode();
            var rightRegisterTyped = new Converter().Convert(Expression.Type, identifier.Type, rightRegister);
            var identReg = identifier.GenPointerCode();
            Emiter.EmitCode($"store {identifier.Type.LLVMName} {rightRegisterTyped}, {identifier.Type.LLVMName}* {identReg}");
            var leftRegister = ParserCS.GetUniqeRegister();
            Emiter.EmitCode($"{leftRegister} = load {identifier.Type.LLVMName}, {identifier.Type.LLVMName}* {identifier.Name}");
            return leftRegister;
        }
        public override string GenCode()
        {
            return Identifier.Accept(this);
        }
    }

    #endregion Expressions


    #region IF

    public class IfInstruction : InstructionNode, ITypeVisitor<bool>
    {
        public ExpressionNode Condition { get; }
        public InstructionNode Instruction { get; }
        public InstructionNode ElseInstruction { get; }

        public IfInstruction(ExpressionNode condition, InstructionNode instruction, InstructionNode elseInstruction = null)
        {
            Condition = condition;
            Instruction = instruction;
            ElseInstruction = elseInstruction;

        }

        public bool Visit(IntType type) => false;
        public bool Visit(DoubleType type) => false;
        public bool Visit(BoolType type) => true;

        public override void SecondRun()
        {
            Condition.Parent = this.Parent;
            Instruction.Parent = this.Parent;
            Condition.SecondRun();
            Instruction.SecondRun();
            if (ElseInstruction != null)
            {
                ElseInstruction.Parent = this.Parent;
                ElseInstruction.SecondRun();
            }
            if (!Condition.Type.Accept(this))
            {
                ParserCS.ReportError("If condition is not a bool type", LineNumber);
            }
        }

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
        public ExpressionNode Condition { get; }
        public InstructionNode Instruction { get; }

        public WhileInstruction(ExpressionNode condition, InstructionNode instruction)
        {
            Condition = condition;
            Instruction = instruction;
            Instruction.ParentLoop = this;
        }

        public bool Visit(IntType type) => false;
        public bool Visit(DoubleType type) => false;
        public bool Visit(BoolType type) => true;


        public string LabelStart;
        public string LabelThen;
        public string LabelEnd;

        public override void SecondRun()
        {
            LabelStart = ParserCS.GetUniqeLabel();
            LabelThen = ParserCS.GetUniqeLabel();
            LabelEnd = ParserCS.GetUniqeLabel();
            Condition.Parent = this.Parent;
            Instruction.Parent = this.Parent;
            Condition.SecondRun();
            Instruction.SecondRun();
            if (!Condition.Type.Accept(this))
            {
                ParserCS.ReportError("While condition is not a bool type", LineNumber);
            }
        }
        public override string GenCode()
        {

            Emiter.EmitCode($"br label %{LabelStart}");
            Emiter.EmitCode(LabelStart + ":");
            var lExpReg = Condition.GenCode();
            Emiter.EmitCode($"br i1 {lExpReg}, label %{LabelThen}, label %{LabelEnd}");
            Emiter.EmitCode(LabelThen + ":");
            Instruction.GenCode();
            Emiter.EmitCode($"br label %{LabelStart}");
            Emiter.EmitCode(LabelEnd + ":");

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

        }

        public bool Visit(IntType type) => true;
        public bool Visit(DoubleType type) => true;
        public bool Visit(BoolType type) => false;

        string ITypeVisitor<string>.Visit(IntType type)
        {
            Emiter.EmitCode($"call i32 (i8*, ...) @scanf(i8* bitcast ([3 x i8]* @int_res to i8*), i32* {Identifier.Name})");
            return null;
        }

        string ITypeVisitor<string>.Visit(DoubleType type)
        {
            Emiter.EmitCode($"call i32 (i8*, ...) @scanf(i8* bitcast ([4 x i8]* @double_res to i8*), double* {Identifier.Name})");
            return null;
        }

        string ITypeVisitor<string>.Visit(BoolType type)
        {
            throw new NotImplementedException();
        }

        public override void SecondRun()
        {
            Identifier.Parent = this.Parent;
            Identifier.SecondRun();
            if (!Identifier.Type.Accept<bool>(this))
            {
                ParserCS.ReportError("Read instruction expects int or bool identifier", LineNumber);
            }
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
        }

        public bool Visit(IntType type) => true;
        public bool Visit(DoubleType type) => false;
        public bool Visit(BoolType type) => false;

        public override void SecondRun()
        {
            Identifier.Parent = this.Parent;
            Identifier.SecondRun();
            if (!Identifier.Type.Accept(this))
            {
                ParserCS.ReportError("Read HEX instruction expects int identifier", LineNumber);
            }
        }

        public override string GenCode()
        {
            Emiter.EmitCode($"call i32 (i8*, ...) @scanf(i8* bitcast ([5 x i8]* @hex_res to i8*), i32* {Identifier.Name})");
            return null;
        }
    }
    #endregion Read

    #region Write

    public class WriteInstruction : InstructionNode, ITypeVisitor<bool>, ITypeVisitor<string>
    {
        public ExpressionNode Expression { get; }

        public WriteInstruction(ExpressionNode expression)
        {
            Expression = expression;
        }

        public bool Visit(IntType type) => true;
        public bool Visit(DoubleType type) => true;
        public bool Visit(BoolType type) => true;


        public override void SecondRun()
        {
            Expression.Parent = this.Parent;
            Expression.SecondRun();
            if (!Expression.Type.Accept<bool>(this))
            {
                ParserCS.ReportError("Write instruction expects int, double or bool identifier", LineNumber);
            }
        }

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
        public ExpressionNode Expression { get; }

        public WriteHexInstruction(ExpressionNode expression)
        {
            Expression = expression;

        }

        public bool Visit(IntType type) => true;
        public bool Visit(DoubleType type) => false;
        public bool Visit(BoolType type) => false;

        public override void SecondRun()
        {
            Expression.Parent = this.Parent;
            Expression.SecondRun();
            if (!Expression.Type.Accept(this))
            {
                ParserCS.ReportError("Write HEX instruction expects int identifier", LineNumber);
            }
        }

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
        public override void SecondRun()
        {
            return;
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
        public override void SecondRun()
        {
            return;
        }

        public override string GenCode()
        {
            Emiter.EmitCode("ret i32 0");
            return null;
        }
    }

    #endregion Return

    #region Break
    public class BreakInstruction : InstructionNode
    {
        public int Depth { get; }

        public BreakInstruction(string depth = "1")
        {

            Depth = int.Parse(depth);
            if (Depth <= 0)
            {
                ParserCS.ReportError($"Break number must be positive", LineNumber);
            }
        }

        public string Label;

        public override void SecondRun()
        {
            WhileInstruction p = this.ParentLoop == null ? this.Parent.ParentLoop : this.ParentLoop;
            if (p == null)
            {
                ParserCS.ReportError($"Break in not inside {Depth} loops", LineNumber);
                return;
            }
            for (int i = 0; i < Depth - 1; i++)
            {
                p = p.ParentLoop == null ? p.Parent.ParentLoop : p.ParentLoop;
                if (p == null)
                {
                    ParserCS.ReportError($"Break in not inside {Depth} loops", LineNumber);
                    return;
                }
            }
            Label = p.LabelEnd;

        }

        public override string GenCode()
        {
            Emiter.EmitCode($"br label %{Label}");
            return null;
        }
    }

    public class ContinueInstruction : InstructionNode
    {
        public int Deep { get; }

        public ContinueInstruction() { }

        public string Label;

        public override void SecondRun()
        {
            WhileInstruction p = this.ParentLoop == null ? this.Parent.ParentLoop : this.ParentLoop;
            if (p == null)
            {
                ParserCS.ReportError($"Continue is not inside loop", LineNumber);
                return;
            }
            Label = p.LabelStart;

        }

        public override string GenCode()
        {
            Emiter.EmitCode($"br label %{Label}");
            return null;
        }
    }

    #endregion Break

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

            if (args.Length == 0)
            {
                Console.WriteLine("Usage: 1 argument - path to source file");
                return 1;
            }

            string fileName = args[0];

            StreamReader streamReader;
            try
            {
                streamReader = new StreamReader(fileName);
            }
            catch (Exception)
            {
                Console.WriteLine("Wrong file path");
                return 1;
            }

            var scanner = new Scanner(streamReader.BaseStream);

            scanner.Reset();
            ParserCS.Reset();

            var parser = new Parser(scanner);
            parser.Parse();

            if (parser.RootNode != null)
            {
                parser.RootNode.SecondRun();
            }

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
                    Console.WriteLine("Could not create output file");
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
