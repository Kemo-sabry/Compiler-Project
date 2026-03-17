using System.Collections.Generic;

namespace TINY_Compiler
{
	public static class Errors
	{
		public static List<string> Error_List = new List<string>();
	}

	public static class TINY_Compiler
	{
		public static Scanner tiny_Scanner = new Scanner();
		public static List<Token> TokenStream => CompilerState.TokenStream;

		public static void Start_Compiling(string code)
		{
			// Clear previous results
			Errors.Error_List.Clear();
			tiny_Scanner.Tokens.Clear();
			CompilerState.TokenStream.Clear();

			tiny_Scanner.StartScanning(code);
		}
	}
}