// ---------------------------------------------------------------------------
// MACHINE-GENERATED FILE
//
// This file was generated by Turbo Coco/R tool. Changes to this file will be
// lost if the file is regenerated.
// ---------------------------------------------------------------------------

using System;

#nullable disable

namespace RadCalc.Grammar
{


	class Parser
	{
		public const int _EOF = 0;
		public const int _identifier = 1;
		public const int _integer = 2;
		public const int maxT = 10;

		const bool _T = true;
		const bool _x = false;
		const int minErrDist = 2;
		
		public Scanner scanner;
		public Errors  errors;

		public Token t;    // last recognized token
		public Token la;   // lookahead token
		int errDist = minErrDist;

	public int Result { get; private set; }



		public Parser(Scanner scanner) {
			this.scanner = scanner;
			errors = new Errors();
		}

		void SynErr (int n) {
			if (errDist >= minErrDist) errors.SynErr(la.line, la.col, n);
			errDist = 0;
		}

		public void SemErr (string msg) {
			if (errDist >= minErrDist) errors.SemErr(t.line, t.col, msg);
			errDist = 0;
		}
		
		void Get () {
			for (;;) {
				t = la;
				la = scanner.Scan();
				if (la.kind <= maxT) { ++errDist; break; }

				la = t;
			}
		}
		
		void Expect (int n) {
			if (la.kind==n) Get(); else { SynErr(n); }
		}
		
		bool StartOf (int s) {
			return set[s, la.kind];
		}
		
		void ExpectWeak (int n, int follow) {
			if (la.kind == n) Get();
			else {
				SynErr(n);
				while (!StartOf(follow)) Get();
			}
		}

		bool WeakSeparator(int n, int syFol, int repFol) {
			int kind = la.kind;
			if (kind == n) {Get(); return true;}
			else if (StartOf(repFol)) {return false;}
			else {
				SynErr(n);
				while (!(set[syFol, kind] || set[repFol, kind] || set[0, kind])) {
					Get();
					kind = la.kind;
				}
				return StartOf(syFol);
			}
		}
		
		void RadCalc() {
			Expression(out var value);
			Result = value; 
		}

		void Expression(out int r) {
			SimpleExpression(out r);
		}

		void Number(out int r) {
			Expect(2);
			if (!int.TryParse(t.val, out r))
			   SemErr("invalid integer format"); 
		}

		void Literal(out int r) {
			Number(out r);
		}

		void Primary(out int r) {
			r = default; 
			if (la.kind == 2) {
				Literal(out r);
			} else if (la.kind == 1) {
				FunctionCall(out r);
			} else if (la.kind == 3) {
				Get();
				Expression(out r);
				Expect(4);
			} else SynErr(11);
		}

		void FunctionCall(out int r) {
			string name;
			int[] args = Array.Empty<int>();
			r = default; 
			Designator(out name);
			ActualParameters(out args);
			switch (name)
			{
			   case "abs":
			       if (args.Length != 1)
			       {
			           SemErr("invalid number of arguments");
			           break;
			       }
			       r = Math.Abs(args[0]);
			       break;
			
			   default:
			       SemErr("unknown function");
			       break;
			} 
		}

		void Designator(out string s) {
			Expect(1);
			s = t.val; 
		}

		void ActualParameters(out int[] r) {
			r = Array.Empty<int>(); 
			Expect(3);
			if (StartOf(1)) {
				ExpList(out r);
			}
			Expect(4);
		}

		void ExpList(out int[] r) {
			var list = new List<int>(); 
			Expression(out var e);
			list.Add(e); 
			while (la.kind == 5) {
				Get();
				Expression(out e);
				list.Add(e); 
			}
			r = list.ToArray(); 
		}

		void Factor(out int r) {
			Primary(out r);
		}

		void Term(out int r) {
			Factor(out r);
			while (la.kind == 6 || la.kind == 7) {
				if (la.kind == 6) {
					Get();
					Factor(out var factor);
					r = r * factor; 
				} else {
					Get();
					Factor(out var factor);
					r = r / factor; 
				}
			}
		}

		void SimpleExpression(out int r) {
			r = default; 
			if (la.kind == 8) {
				Get();
				Term(out var value);
				r = value; 
			} else if (la.kind == 9) {
				Get();
				Term(out var value);
				r = -value; 
			} else if (la.kind == 1 || la.kind == 2 || la.kind == 3) {
				Term(out r);
			} else SynErr(12);
			while (la.kind == 8 || la.kind == 9) {
				if (la.kind == 8) {
					Get();
					Term(out var term);
					r += term; 
				} else {
					Get();
					Term(out var term);
					r -= term; 
				}
			}
		}



		public void Parse() {
			la = new Token();
			la.val = "";		
			Get();
			RadCalc();
			Expect(0);

		}
		
		static readonly bool[,] set =
		{
			{_T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
			{_x,_T,_T,_T, _x,_x,_x,_x, _T,_T,_x,_x}

		};
	} // end Parser


	class Errors
	{
		public int count = 0;                                    // number of errors detected
		public System.IO.TextWriter errorStream = Console.Out;   // error messages go to this stream
		public string errMsgFormat = "-- line {0} col {1}: {2}"; // 0=line, 1=column, 2=text

		public virtual void SynErr (int line, int col, int n) {
			string s;
			switch (n) {
				case 0: s = "EOF expected"; break;
			case 1: s = "identifier expected"; break;
			case 2: s = "integer expected"; break;
			case 3: s = "\"(\" expected"; break;
			case 4: s = "\")\" expected"; break;
			case 5: s = "\",\" expected"; break;
			case 6: s = "\"*\" expected"; break;
			case 7: s = "\"/\" expected"; break;
			case 8: s = "\"+\" expected"; break;
			case 9: s = "\"-\" expected"; break;
			case 10: s = "??? expected"; break;
			case 11: s = "invalid Primary"; break;
			case 12: s = "invalid SimpleExpression"; break;

				default: s = "error " + n; break;
			}
			errorStream.WriteLine(errMsgFormat, line, col, s);
			count++;
		}

		public virtual void SemErr (int line, int col, string s) {
			errorStream.WriteLine(errMsgFormat, line, col, s);
			count++;
		}
		
		public virtual void SemErr (string s) {
			errorStream.WriteLine(s);
			count++;
		}
		
		public virtual void Warning (int line, int col, string s) {
			errorStream.WriteLine(errMsgFormat, line, col, s);
		}
		
		public virtual void Warning(string s) {
			errorStream.WriteLine(s);
		}
	} // Errors
}
