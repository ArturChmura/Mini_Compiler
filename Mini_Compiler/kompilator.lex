%using QUT.Gppg;
%namespace Mini_Compiler

IntNum			([1-9][0-9]*|0)
Identifier      ([a-zA-Z][a-zA-Z0-9]*)
IntNumber       (0|[1-9][0-9]*)
DoubleNumber    (0|[1-9][0-9]*)\.[0-9]+
HexNumber		0[x|X]([0-9|a-f|A-F])*
Comment			(\/\/.*)
String			\"(\\.|[^"\n\\])*\"

%{
	public static int LineNumber { get; private set; } = 1;
 
	public void Reset()
	{
		ErrorsCount = 0;
		LineNumber = 1;
	}

	public int ErrorsCount { get; private set; } = 0;

	public override void yyerror(string msg, params object[] args)
	{
		ErrorsCount++;
		Console.WriteLine("Scanner error on line " + LineNumber.ToString() + ": " + msg);
	}
%}

%%

"program"		{ return (int)Tokens.Program; }
"if" 			{ return (int)Tokens.If; }
"else" 			{ return (int)Tokens.Else; }
"while"			{ return (int)Tokens.While; }
"read" 			{ return (int)Tokens.Read; }
"write"			{ return (int)Tokens.Write; }
"return"		{ return (int)Tokens.Return; }
"int" 			{ return (int)Tokens.IntType; }
"double"		{ return (int)Tokens.DoubleType; }
"bool" 			{ return (int)Tokens.BoolType; }
"true" 			{ return (int)Tokens.True; }
"false"			{ return (int)Tokens.False; }
"hex" 			{ return (int)Tokens.Hex; }
"break" 		{ return (int)Tokens.Break; }
"continue" 		{ return (int)Tokens.Continue; }

"="				{ return (int)Tokens.Assign; }
"||"			{ return (int)Tokens.Or; }
"&&"			{ return (int)Tokens.And; }
"|"				{ return (int)Tokens.BitOr; }
"&"				{ return (int)Tokens.BitAnd; }
"=="			{ return (int)Tokens.Equality; }
"!="			{ return (int)Tokens.Unequality; }
">"				{ return (int)Tokens.Greater; }
">="			{ return (int)Tokens.GreaterOrEqual; }
"<"				{ return (int)Tokens.Less; }
"<="			{ return (int)Tokens.LessOrEqual; }
"+"				{ return (int)Tokens.Plus; }
"-"				{ return (int)Tokens.Minus; }
"*"				{ return (int)Tokens.Multiplies; }
"/"				{ return (int)Tokens.Divides; }
"!"				{ return (int)Tokens.Exclamation; }
"~"				{ return (int)Tokens.Tilde; }
"("				{ return (int)Tokens.OpenParenthesis; }
")"				{ return (int)Tokens.CloseParenthesis; }
"{"				{ return (int)Tokens.OpenBracket; }
"}"				{ return (int)Tokens.CloseBracket; }
"["				{ return (int)Tokens.OpenSquare; }
"]"				{ return (int)Tokens.CloseSquare; }
","				{ return (int)Tokens.Comma; }
";"				{ return (int)Tokens.Semicolon; }

"\r"			{ }
" "				{ }
"\t"			{ }
"\n"			{ LineNumber++; }

{Identifier}	{ yylval.str=yytext; return (int)Tokens.Identifier; }
{IntNumber}		{ yylval.str=yytext; return (int)Tokens.IntNumber; }     
{DoubleNumber}	{ yylval.str=yytext; return (int)Tokens.DoubleNumber; }    
{HexNumber}		{ yylval.str=yytext; return (int)Tokens.HexNumber; }   
{String}		{ yylval.str=yytext; return (int)Tokens.String; }         
{Comment}		{ }    
<<EOF>>			{ return (int)Tokens.EOF; } 
.				{ yyerror("Unexpected token: " + yytext); return (int)Tokens.error; }