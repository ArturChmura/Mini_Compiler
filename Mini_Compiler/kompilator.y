%namespace Mini_Compiler

%output=Parser.cs


%{
    private ParserCS parser;
    public int ErrorsCount { get => ParserCS.ErrorsCount; }

    public Parser(Scanner scanner) : base(scanner) { 
        this.parser = new ParserCS(scanner);
    }

    public INode RootNode { get; private set; }
    
%}

%union
{
    public INode node;
    public string str;
    public int intType;
    public double doubleType;
    public bool boolType;
    public List<INode> nodes;
    public List<string> strings;
    public List<DeclarationNode> declarations;
    public List<InstructionNode> instructions;
    public DeclarationNode declaration;
    public InstructionNode instruction;
    public IExpression expression;
    public IType type;
    public Identifier identifier;
}

%token  Program If Else While String DoubleType IntType Read Write Return BoolType True False Hex 
%token  Assign Or And BitOr BitAnd Equality Unequality Greater GreaterOrEqual Less LessOrEqual
%token  Plus Minus Multiplies Divides Exclamation Tilde 
%token  OpenParenthesis CloseParenthesis OpenBracket CloseBracket Comma Semicolon
%token  Error DoubleNumber IntNumber IntParse DoubleParse

%token <str> Identifier IntNumber DoubleNumber True False

%type <declarations> declarations
%type <declaration> declaration 
%type <strings> identifiers identifiersComma
%type <type> type 
%type <instructions> instructions
%type <instruction> instruction block expressionInstruction
%type <expression> expression constantExpression unaryExpression binaryExpression
%type <identifier> identifier 

%right Assign
%left And Or
%left Equality Unequality Greater GreaterOrEqual Less LessOrEqual
%left Plus Minus
%left Multiplies Divides
%left BitOr BitAnd
%right Tilde Exclamation IntParse DoubleParse Minus

%%

start           : Program block { RootNode  = $2; }
                ;

block           : OpenBracket declarations instructions CloseBracket   { $$  = new BlockInstructionNode($2, $3); }
                ;

declarations    : declarations declaration { $$ = $1; $$.Add($2); }
                | { $$ = new List<DeclarationNode>(); }
                ;

declaration     : type identifiers Semicolon 
                { 
                    $$ = new DeclarationNode($1,$2); 
                    parser.MakeDeclaration($1,$2);
                }
                ;

identifiers     : identifiersComma Identifier { $$ = $1; $$.Add($2); }
                ;

identifiersComma: identifiersComma Identifier Comma  { $$ = $1; $$.Add($2); }
                | { $$ = new List<string>(); }
                ;

type            : IntType { $$ = new IntType(); }
                | DoubleType { $$ = new DoubleType(); }
                | BoolType { $$ = new BoolType(); }
                ;

instructions    : instructions instruction { $$ = $1; $$.Add($2); }
                | { $$ = new List<InstructionNode>(); }
                ;

instruction     : block { $$ = $1; }
                | expressionInstruction { $$ = $1; }
                ;

expressionInstruction      : expression Semicolon { $$ = new ExpressionInstructionNode($1); }
                ;

expression      : unaryExpression { $$ = $1; }
                | binaryExpression { $$ = $1; }
                | identifier { $$ = $1; }
                | constantExpression { $$ = $1; }
                | OpenParenthesis expression CloseParenthesis { $$ = $2; }
                ;

unaryExpression : Minus expression { $$ = new UnaryMinusExpression($2);  }
                | Tilde expression { $$ = new BitNegationExpression($2);  }
                | Exclamation expression { $$ = new LogicNegationExpression($2);  }
                | IntParse expression { $$ = new IntConversionExpression($2);  }
                | DoubleParse expression { $$ = new DoubleConversionExpression($2);  }
                ;

binaryExpression  : expression BitOr expression { $$ = new BitsOrExpression($1,$3);  }
                | expression BitAnd expression { $$ = new BitAndExpression($1,$3);  }

                | expression Multiplies expression { $$ = new Multiplication($1,$3);  }
                | expression Divides expression { $$ = new Division($1,$3);  }

                | expression Plus expression { $$ = new Sum($1,$3);  }
                | expression Minus expression { $$ = new Subtraction($1,$3);  }

                | expression Equality expression { $$ = new EqualsExpression($1,$3);  }
                | expression Unequality expression { $$ = new NotequalsExpression($1,$3);  }
                | expression Greater expression { $$ = new GreaterExpression($1,$3);  }
                | expression GreaterOrEqual expression { $$ = new GreaterOrEqualExpression($1,$3);  }
                | expression Less expression { $$ = new LessExpression($1,$3);  }
                | expression LessOrEqual expression { $$ = new LessOrEqualExpression($1,$3);  }

                | expression And expression { $$ = new AndExpression($1,$3);  }
                | expression Or expression { $$ = new OrExpression($1,$3);  }

                | identifier Assign expression { $$ = new AssignExpression($1,$3); }
                ;

constantExpression  : IntNumber { $$ = new IntConstantExpression($1); }
                | DoubleNumber { $$ = new DoubleConstantExpression($1); }
                | True { $$ = new BoolConstantExpression(true); }
                | False { $$ = new BoolConstantExpression(false); }
                ;

identifier : Identifier { $$ = parser.GetIdentifier($1); }
            ;