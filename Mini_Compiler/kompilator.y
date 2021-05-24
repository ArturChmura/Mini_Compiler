%namespace Mini_Compiler

%output=Parser.cs


%{
    private Corrector corrector;
    public int ErrorsCount { get => corrector.ErrorsCount; }

    public Parser(Scanner scanner) : base(scanner) { 
        this.corrector = new Corrector(scanner);
    }

    public INode RootNode { get; private set; }
    
%}

%union
{
    public string str;
    public INode node;
    public List<INode> nodes;
    public List<string> strings;
    public List<DeclarationNode> declarations;
    public DeclarationNode declaration;
    public IType type;
}

%token  Program If Else While String RealType IntType Read Write Return BoolType True False Hex 
%token  Assign Or And BitOR BitAnd Equality Unequality Greater GreaterOrEqual Less LessOrEqual
%token  Plus Minus Multiplies Divides Exclamation Tilde OpenParenthesis CloseParenthesis OpenBracket CloseBracket Comma Semicolon
%token   Error RealNumber IntNumber 

%token <str> Identifier 
%type <node> block 
%type <declarations> declarations
%type <declaration> declaration 
%type <strings> identifiers identifiersComma
%type <type> type 

%%

start           : Program block { RootNode  = $2; }
                ;
block           : OpenBracket declarations CloseBracket   { $$  = new RootNode($2); }
                ;
declarations    : declarations declaration { $$ = $1; $$.Add($2); }
                | { $$ = new List<DeclarationNode>(); }
                ;
declaration     : type identifiers Semicolon 
                { 
                    $$ = new DeclarationNode($1,$2); 
                    corrector.MakeDeclaration($2);
                }
                ;
identifiers     : identifiersComma Identifier { $$ = $1; $$.Add($2); }
                ;
identifiersComma: identifiersComma Identifier Comma  { $$ = $1; $$.Add($2); }     
                | { $$ = new List<string>(); }
                ;
type            : IntType { $$ = new IntType(); }
                | RealType { $$ = new RealType(); }
                | BoolType { $$ = new BoolType(); }
                ;