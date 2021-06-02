# About Mini_Compiler
Is a compiler for made-up language inspired by C. Programs are compiled to LLVM code that can be run by LLI.
# Mini Language details
* Available types are: int, double, bool as well as array type of all 3
* Keywords: program if else while read write return int double bool true false hex
* Operators: = || && | & == != > >= < <= + - * / ! ~ ( ) { } , ;
* program must begin with keyword "program" followed by { }
* example program:
```
program
{
   int a, b, t[5,7], i , j;
   read a;
   read b;

   i = 0;
   while(i < 5)
   {
		j = 0;
		while(j < 7)
		{
			t[i,j] = a*i - b|j;
			j = j+1;
			if(t[3,3] < 0)
				break 2;
		}
		i = i+1;
   }

   if(t[3,3] > 0)
   {
	write "positive \n";
   }
   else
   {
	write "not positive \n";
   }

}
```
