// This code was generated by a tool.
// Changes to this file will be lost if the code is regenerated.


using System;

#nullable disable

namespace Calc.Grammar
{


	class Parser
	{
		public const int _EOF = 0;
		public const int _integer = 1;
		public const int maxT = 8;

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
		
		void Calc() {
			Expression(out var value);
			Result = value; 
		}

		void Expression(out int r) {
			SimpleExpression(out r);
		}

		void Number(out int r) {
			Expect(1);
			if (!int.TryParse(t.val, out r))
			SemErr("invalid integer format"); 
		}

		void Literal(out int r) {
			Number(out r);
		}

		void Primary(out int r) {
			r = default; 
			if (la.kind == 1) {
				Literal(out r);
			} else if (la.kind == 2) {
				Get();
				Expression(out r);
				Expect(3);
			} else SynErr(9);
		}

		void Factor(out int r) {
			Primary(out r);
		}

		void Term(out int r) {
			Factor(out r);
			while (la.kind == 4 || la.kind == 5) {
				if (la.kind == 4) {
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
			if (la.kind == 6) {
				Get();
				Term(out var value);
				r = value; 
			} else if (la.kind == 7) {
				Get();
				Term(out var value);
				r = -value; 
			} else if (la.kind == 1 || la.kind == 2) {
				Term(out r);
			} else SynErr(10);
			while (la.kind == 6 || la.kind == 7) {
				if (la.kind == 6) {
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
			Calc();
			Expect(0);

		}
		
		static readonly bool[,] set =
		{
			{_T,_x,_x,_x, _x,_x,_x,_x, _x,_x}

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
			case 1: s = "integer expected"; break;
			case 2: s = "\"(\" expected"; break;
			case 3: s = "\")\" expected"; break;
			case 4: s = "\"*\" expected"; break;
			case 5: s = "\"/\" expected"; break;
			case 6: s = "\"+\" expected"; break;
			case 7: s = "\"-\" expected"; break;
			case 8: s = "??? expected"; break;
			case 9: s = "invalid Primary"; break;
			case 10: s = "invalid SimpleExpression"; break;

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
