%namespace Mini_Compiler
%output=Parser.cs

%{
    public int ErrorsCount { get => ParserCS.ErrorsCount; }

    public Node RootNode { get; private set; }

    public Parser(Scanner scanner) : base(scanner) { 
    }
%}

%union
{
    public Node node;
    public string str;
    public int intType;
    public double doubleType;
    public bool boolType;
    public List<Node> nodes;
    public Identifiers identifiers;
    public List<DeclarationNode> declarations;
    public List<InstructionNode> instructions;
    public DeclarationNode declaration;
    public InstructionNode instruction;
    public ExpressionNode expression;
    public IType type;
    public Identifier identifier;
    public StringLex stringLex;
    public List<string> indexes;
    public List<ExpressionNode> expressionsIndexes;
}

%token  Program If Else While String DoubleType IntType Read Write Return BoolType True False Hex 
%token  Assign Or And BitOr BitAnd Equality Unequality Greater GreaterOrEqual Less LessOrEqual
%token  Plus Minus Multiplies Divides Exclamation Tilde 
%token  OpenParenthesis CloseParenthesis OpenBracket CloseBracket Comma Semicolon
%token  DoubleNumber IntNumber HexNumber
%token  Break Continue OpenSquare CloseSquare

%token <str> Identifier IntNumber DoubleNumber HexNumber True False String
%type <declarations> declarations
%type <declaration> declaration 
%type <identifiers> identifiers identifiersComma
%type <type> type 
%type <expression> expression  unaryExpression bitExpression multiplicativeExpression additiveExpression relationalExpression logicExpression assignExpression constantExpression
%type <instructions> instructions
%type <instruction> instruction blockInstruction expressionInstruction ifInstruction whileInstruction readInstruction writeInstruction returnInstruction breakInstruction continueInstruction
%type <identifier> identifier 
%type <stringLex> string 
%type <indexes> indexes indexesComma
%type <expressionsIndexes> expressionsIndexes expressionsIndexesComma 

%nonassoc CloseParenthesis
%nonassoc Else

%%

start               : Program blockInstruction { RootNode  = $2; }
                    ;

blockInstruction    : OpenBracket declarations instructions CloseBracket   { $$  = new BlockInstructionNode($2, $3); }
                    ;

declarations        : declarations declaration { $$ = $1; $$.Add($2); }
                    | { $$ = new List<DeclarationNode>(); }
                    ;

declaration         : type identifiers Semicolon { $$ = new DeclarationNode($1,$2); }
                    ;

identifiers         : identifiersComma Identifier { $$ = $1; $$.SimpleIdentifiers.Add($2); }
                    | identifiersComma Identifier OpenSquare indexes CloseSquare { $$ = $1; $$.ArrayIdentifiers.Add(($2, $4)); }
                    ;

identifiersComma    : identifiersComma Identifier Comma  { $$ = $1; $$.SimpleIdentifiers.Add($2); }
                    | identifiersComma Identifier OpenSquare indexes CloseSquare Comma { $$ = $1; $$.ArrayIdentifiers.Add(($2, $4)); }
                    | { $$ = new Identifiers(); }
                    ;

indexes             : indexesComma IntNumber { $$ = $1; $$.Add($2); }
                    ;

indexesComma        : indexesComma IntNumber Comma  { $$ = $1; $$.Add($2); }
                    | { $$ = new List<string>(); }
                    ;


type                : IntType { $$ = IntType.Get; }
                    | DoubleType { $$ = DoubleType.Get; }
                    | BoolType { $$ = BoolType.Get; }
                    ;

instructions        : instructions instruction { $$ = $1; $$.Add($2); }
                    | { $$ = new List<InstructionNode>(); }
                    ;

instruction         : blockInstruction { $$ = $1; }
                    | expressionInstruction { $$ = $1; }
                    | ifInstruction { $$ = $1; }
                    | whileInstruction { $$ = $1; }
                    | readInstruction { $$ = $1; }
                    | writeInstruction { $$ = $1; }
                    | returnInstruction { $$ = $1; }
                    | breakInstruction { $$ = $1; }
                    | continueInstruction { $$ = $1; }
                    | errorLine  { yyerrok(); }
                    ;

errorLine           : error Semicolon
                    | error CloseSquare 
                    | error CloseParenthesis 
                    | error CloseBracket 
                    ;

expressionInstruction: expression Semicolon { $$ = $1; }
                    ;
                     
expression          : assignExpression { $$ = $1; } 
                    ;
                             
assignExpression    : identifier Assign assignExpression { $$ = new AssignExpression($1,$3); }
                    | logicExpression { $$ = $1; }
                    ;
                    
logicExpression     : logicExpression And relationalExpression { $$ = new AndExpression($1,$3);  }
                    | logicExpression Or relationalExpression { $$ = new OrExpression($1,$3);  }
                    | relationalExpression  { $$ = $1; }
                    ;

relationalExpression  : relationalExpression Equality additiveExpression { $$ = new EqualsExpression($1,$3);  }
                    | relationalExpression Unequality additiveExpression { $$ = new NotequalsExpression($1,$3);  }
                    | relationalExpression Greater additiveExpression { $$ = new GreaterExpression($1,$3);  }
                    | relationalExpression GreaterOrEqual additiveExpression { $$ = new GreaterOrEqualExpression($1,$3);  }
                    | relationalExpression Less additiveExpression { $$ = new LessExpression($1,$3);  }
                    | relationalExpression LessOrEqual additiveExpression { $$ = new LessOrEqualExpression($1,$3);  }
                    | additiveExpression  { $$ = $1; }
                    ;
                                        
additiveExpression  : additiveExpression Plus multiplicativeExpression { $$ = new Sum($1,$3);  }
                    | additiveExpression Minus multiplicativeExpression { $$ = new Subtraction($1,$3);  }
                    | multiplicativeExpression  { $$ = $1; }
                    ;  
                    
multiplicativeExpression     : multiplicativeExpression Multiplies bitExpression { $$ = new Multiplication($1,$3);  }
                    | multiplicativeExpression Divides bitExpression { $$ = new Division($1,$3);  }
                    | bitExpression  { $$ = $1; }
                    ;
                    
bitExpression       : bitExpression BitOr unaryExpression { $$ = new BitsOrExpression($1,$3);  }
                    | bitExpression BitAnd unaryExpression { $$ = new BitAndExpression($1,$3);  }
                    | unaryExpression { $$ = $1; }
                    ;

unaryExpression     : Minus unaryExpression { $$ = new UnaryMinusExpression($2);  }
                    | Tilde unaryExpression { $$ = new BitNegationExpression($2);  }
                    | Exclamation unaryExpression { $$ = new LogicNegationExpression($2);  }
                    | OpenParenthesis IntType CloseParenthesis unaryExpression { $$ = new IntConversionExpression($4);  }
                    | OpenParenthesis DoubleType CloseParenthesis unaryExpression { $$ = new DoubleConversionExpression($4);  }
                    | constantExpression
                    ;

constantExpression  : IntNumber { $$ = new IntConstantExpression($1); }
                    | HexNumber { $$ = new IntConstantExpression($1, true); }
                    | DoubleNumber { $$ = new DoubleConstantExpression($1); }
                    | True { $$ = new BoolConstantExpression(true); }
                    | False { $$ = new BoolConstantExpression(false); }
                    | identifier { $$ = $1; }
                    | OpenParenthesis expression CloseParenthesis { $$ = $2; }
                    ;

ifInstruction       : If OpenParenthesis expression CloseParenthesis instruction { $$ = new IfInstruction($3,$5); }
                    | If OpenParenthesis expression CloseParenthesis instruction Else instruction { $$ = new IfInstruction($3,$5,$7); }
                    ;

whileInstruction    : While OpenParenthesis expression CloseParenthesis instruction { $$ = new WhileInstruction($3,$5); }
                    ;

readInstruction     : Read identifier Semicolon { $$ = new ReadInstruction($2); }
                    | Read identifier Comma Hex Semicolon { $$ = new ReadHexInstruction($2); }
                    ;
                
writeInstruction    : Write expression Semicolon { $$ = new WriteInstruction($2); }
                    | Write expression Comma Hex Semicolon { $$ = new WriteHexInstruction($2); }
                    | Write string Semicolon { $$ = new WriteStringInstruction($2); }
                    ;
string              : String {$$ = new StringLex($1);}
                    ;

returnInstruction   : Return Semicolon { $$ = new ReturnInstruction(); }
                    ;

breakInstruction    : Break IntNumber Semicolon { $$ = new BreakInstruction($2); }    
                    | Break Semicolon { $$ = new BreakInstruction(); }  
                    ;

continueInstruction : Continue Semicolon { $$ = new ContinueInstruction(); }    
                    ;

                                    
identifier          : Identifier { $$ = new SimpleIdentifier($1);  }
                    | Identifier OpenSquare expressionsIndexes CloseSquare  { $$ = new ArrayIdentifier($1,$3);  }
                    ;

expressionsIndexes  : expressionsIndexesComma expression { $$ = $1; $$.Add($2); }
                    ;

expressionsIndexesComma : expressionsIndexesComma expression Comma  { $$ = $1; $$.Add($2); }
                        | { $$ = new List<ExpressionNode>(); }
                        ;