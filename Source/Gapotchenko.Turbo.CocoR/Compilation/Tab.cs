// Tab.cs - Symbol Table Management

using System.Text;
using System.Collections;
using Gapotchenko.Turbo.CocoR.Compilation.Grammar;
using Gapotchenko.Turbo.CocoR.Compilation.CodeGeneration;

namespace Gapotchenko.Turbo.CocoR.Compilation;

//=====================================================================
// Symbol
//=====================================================================

class Symbol
{

    // token kinds
    public const int fixedToken = 0; // e.g. 'a' ('b' | 'c') (structure of literals)
    public const int classToken = 1;    // e.g. digit {digit}   (at least one char class)
    public const int litToken = 2; // e.g. "while"
    public const int classLitToken = 3; // e.g. letter {letter} but without literals that have the same structure

    public int n;           // symbol number
    public int typ;         // t, nt, pr, unknown, rslv /* ML 29_11_2002 slv added */ /* AW slv --> rslv */
    public string name;        // symbol name
    public Node graph;       // nt: to first node of syntax graph
    public int tokenKind;   // t:  token kind (fixedToken, classToken, ...)
    public bool deletable;   // nt: true if nonterminal is deletable
    public bool firstReady;  // nt: true if terminal start symbols have already been computed
    public BitArray first;       // nt: terminal start symbols
    public BitArray follow;      // nt: terminal followers
    public BitArray nts;         // nt: nonterminals whose followers have to be added to this sym
    public int line;        // source text line number of item in this node
    public Position attrPos;     // nt: position of attributes in source text (or null)
    public Position semPos;      // pr: pos of semantic action in source text (or null)
                                 // nt: pos of local declarations in source text (or null)

    public Symbol(int typ, string name, int line)
    {
        this.typ = typ; this.name = name; this.line = line;
    }
}


//=====================================================================
// Node
//=====================================================================

class Node
{
    // constants for node kinds
    public const int t = 1;  // terminal symbol
    public const int pr = 2;  // pragma
    public const int nt = 3;  // nonterminal symbol
    public const int clas = 4;  // character class
    public const int chr = 5;  // character
    public const int wt = 6;  // weak terminal symbol
    public const int any = 7;  // 
    public const int eps = 8;  // empty
    public const int sync = 9;  // synchronization symbol
    public const int sem = 10;  // semantic action: (. .)
    public const int alt = 11;  // alternative: |
    public const int iter = 12;  // iteration: { }
    public const int opt = 13;  // option: [ ]
    public const int rslv = 14;  // resolver expr

    public const int normalTrans = 0;       // transition codes
    public const int contextTrans = 1;

    public int n;           // node number
    public int typ;     // t, nt, wt, chr, clas, any, eps, sem, sync, alt, iter, opt, rslv
    public Node next;       // to successor node
    public Node down;       // alt: to next alternative
    public Node sub;        // alt, iter, opt: to first node of substructure
    public bool up;         // true: "next" leads to successor in enclosing structure
    public Symbol sym;      // nt, t, wt: symbol represented by this node
    public int val;     // chr:  ordinal character value
                        // clas: index of character class
    public int code;        // chr, clas: transition code
    public BitArray set;        // any, sync: the set represented by this node
    public Position pos;        // nt, t, wt: pos of actual attributes
                                // sem:       pos of semantic action in source text
                                // rslv:       pos of resolver in source text
    public int line;        // source text line number of item in this node
    public DfaState state; // DFA state corresponding to this node
                           // (only used in DFA.ConvertToStates)

    public Node(int typ, Symbol sym, int line)
    {
        this.typ = typ; this.sym = sym; this.line = line;
    }
}

//=====================================================================
// Graph 
//=====================================================================

class Graph
{
    public Node l;  // left end of graph = head
    public Node r;  // right end of graph = list of nodes to be linked to successor graph

    public Graph()
    {
        l = null; r = null;
    }

    public Graph(Node left, Node right)
    {
        l = left; r = right;
    }

    public Graph(Node p)
    {
        l = p; r = p;
    }
}

//=====================================================================
// Sets 
//=====================================================================

class Sets
{
    public static int GetCountOfSetBits(BitArray s)
    {
        int count = 0;

        int n = s.Count;
        for (int i = 0; i < n; ++i)
        {
            if (s[i])
                ++count;
        }

        return count;
    }

    public static bool SetEquals(BitArray a, BitArray b)
    {
        int n = a.Count;
        if (b.Count != n)
            return false;

        for (int i = 0; i < n; i++)
        {
            if (a[i] != b[i])
                return false;
        }

        return true;
    }

    public static bool Overlaps(BitArray a, BitArray b)
    {
        // a * b != {}
        int max = a.Count;
        for (int i = 0; i < max; i++)
            if (a[i] && b[i]) return true;
        return false;
    }

    public static void Except(BitArray a, BitArray b)
    {
        // a = a - b
        var _b = (BitArray)b.Clone();
        a.And(_b.Not());
    }

}

//=====================================================================
// CharClass
//=====================================================================

class CharClass
{
    public int n;           // class number
    public string name;     // class name
    public CharSet set; // set representing the class

    public CharClass(string name, CharSet s)
    {
        this.name = name; set = s;
    }
}


//=====================================================================
// Tab
//=====================================================================

class Tab
{
    public Position semDeclPos;       // position of global semantic declarations
    public CharSet ignored;           // characters ignored by the scanner
    public bool[] ddt = new bool[10]; // debug and test switches
    public Symbol gramSy;             // root nonterminal; filled by ATG
    public Symbol eofSy;              // end of file symbol
    public Symbol noSym;              // used in case of an error
    public BitArray allSyncSets;      // union of all synchronisation sets
    public Hashtable literals;        // symbols that are used as literals

    public string srcName;            // name of the atg file (including path)
    public string? nsName;             // namespace for generated files
    public bool checkEOF = true;      // should coco generate a check for EOF at
                                      //   the end of Parser.Parse():
    public bool emitLines;            // emit #line pragmas for semantic actions
                                      //   in the generated parser

    BitArray visited;                 // mark list for graph traversals
    Symbol curSy;                     // current symbol in computation of sets

    Parser parser;                    // other Coco objects
    TextWriter trace;
    Errors errors;

    public required ICodeGenerationService CodeGenerationService { get; init; }

    public Tab(Parser parser)
    {
        this.parser = parser;
        trace = parser.trace;
        errors = parser.errors;
        eofSy = NewSym(Node.t, "EOF", 0);
        dummyNode = NewNode(Node.eps, null, 0);
        literals = new Hashtable();
    }

    //---------------------------------------------------------------------
    //  Symbol list management
    //---------------------------------------------------------------------

    public List<Symbol> terminals = new();
    public List<Symbol> pragmas = new();
    public List<Symbol> nonterminals = new();

    string[] tKind = { "fixedToken", "classToken", "litToken", "classLitToken" };

    public Symbol NewSym(int typ, ReadOnlySpan<char> name, int line)
    {
        if (name.Length == 2 && name[0] == '"')
        {
            parser.SemErr("empty token not allowed"); name = "???";
        }
        var sym = new Symbol(typ, name.ToString(), line);
        switch (typ)
        {
            case Node.t: sym.n = terminals.Count; terminals.Add(sym); break;
            case Node.pr: pragmas.Add(sym); break;
            case Node.nt: sym.n = nonterminals.Count; nonterminals.Add(sym); break;
        }
        return sym;
    }

    public Symbol? FindSym(ReadOnlySpan<char> name)
    {
        foreach (var symbol in terminals)
            if (name.Equals(symbol.name, StringComparison.Ordinal))
                return symbol;

        foreach (var symbol in nonterminals)
            if (name.Equals(symbol.name, StringComparison.Ordinal))
                return symbol;

        return null;
    }

    void PrintSym(Symbol sym)
    {
        trace.Write("{0,3} {1,-14} {2}", sym.n, Name(sym.name), nTyp[sym.typ]);
        if (sym.attrPos == null) trace.Write(" false "); else trace.Write(" true  ");
        if (sym.typ == Node.nt)
        {
            trace.Write("{0,5}", Num(sym.graph));
            if (sym.deletable) trace.Write(" true  "); else trace.Write(" false ");
        }
        else
            trace.Write("            ");
        trace.WriteLine("{0,5} {1}", sym.line, tKind[sym.tokenKind]);

        static int Num(Node p) => p?.n ?? 0;
    }

    public void PrintSymbolTable()
    {
        trace.WriteLine("Symbol Table:");
        trace.WriteLine("------------"); trace.WriteLine();
        trace.WriteLine(" nr name          typ  hasAt graph  del    line tokenKind");
        foreach (Symbol sym in terminals) PrintSym(sym);
        foreach (Symbol sym in pragmas) PrintSym(sym);
        foreach (Symbol sym in nonterminals) PrintSym(sym);
        trace.WriteLine();
        trace.WriteLine("Literal Tokens:");
        trace.WriteLine("--------------");
        foreach (DictionaryEntry e in literals)
        {
            trace.WriteLine("_" + ((Symbol)e.Value).name + " = " + e.Key + ".");
        }
        trace.WriteLine();
    }

    public void PrintSet(BitArray s, int indent)
    {
        int col, len;
        col = indent;
        foreach (Symbol sym in terminals)
        {
            if (s[sym.n])
            {
                len = sym.name.Length;
                if (col + len >= 80)
                {
                    trace.WriteLine();
                    for (col = 1; col < indent; col++) trace.Write(" ");
                }
                trace.Write("{0} ", sym.name);
                col += len + 1;
            }
        }
        if (col == indent) trace.Write("-- empty set --");
        trace.WriteLine();
    }

    //---------------------------------------------------------------------
    //  Syntax graph management
    //---------------------------------------------------------------------

    public ArrayList nodes = new ArrayList();
    public string[] nTyp =
        {"    ", "t   ", "pr  ", "nt  ", "clas", "chr ", "wt  ", "any ", "eps ",
     "sync", "sem ", "alt ", "iter", "opt ", "rslv"};
    Node dummyNode;

    public Node NewNode(int typ, Symbol sym, int line)
    {
        Node node = new Node(typ, sym, line);
        node.n = nodes.Count;
        nodes.Add(node);
        return node;
    }

    public Node NewNode(int typ, Node sub)
    {
        Node node = NewNode(typ, null, 0);
        node.sub = sub;
        return node;
    }

    public Node NewNode(int typ, int val, int line)
    {
        Node node = NewNode(typ, null, line);
        node.val = val;
        return node;
    }

    public void MakeFirstAlt(Graph g)
    {
        g.l = NewNode(Node.alt, g.l); g.l.line = g.l.sub.line;
        g.r.up = true;
        g.l.next = g.r;
        g.r = g.l;
    }

    // The result will be in g1
    public void MakeAlternative(Graph g1, Graph g2)
    {
        g2.l = NewNode(Node.alt, g2.l); g2.l.line = g2.l.sub.line;
        g2.l.up = true;
        g2.r.up = true;
        Node p = g1.l; while (p.down != null) p = p.down;
        p.down = g2.l;
        p = g1.r; while (p.next != null) p = p.next;
        // append alternative to g1 end list
        p.next = g2.l;
        // append g2 end list to g1 end list
        g2.l.next = g2.r;
    }

    // The result will be in g1
    public void MakeSequence(Graph g1, Graph g2)
    {
        Node p = g1.r.next; g1.r.next = g2.l; // link head node
        while (p != null)
        {  // link substructure
            Node q = p.next; p.next = g2.l;
            p = q;
        }
        g1.r = g2.r;
    }

    public void MakeIteration(Graph g)
    {
        g.l = NewNode(Node.iter, g.l);
        g.r.up = true;
        Node p = g.r;
        g.r = g.l;
        while (p != null)
        {
            Node q = p.next; p.next = g.l;
            p = q;
        }
    }

    public void MakeOption(Graph g)
    {
        g.l = NewNode(Node.opt, g.l);
        g.r.up = true;
        g.l.next = g.r;
        g.r = g.l;
    }

    public void Finish(Graph g)
    {
        Node p = g.r;
        while (p != null)
        {
            Node q = p.next; p.next = null;
            p = q;
        }
    }

    public void DeleteNodes()
    {
        nodes = new ArrayList();
        dummyNode = NewNode(Node.eps, null, 0);
    }

    public Graph StrToGraph(string str)
    {
        string s = Unescape(str.Substring(1, str.Length - 2));
        if (s.Length == 0) parser.SemErr("empty token not allowed");
        Graph g = new Graph();
        g.r = dummyNode;
        for (int i = 0; i < s.Length; i++)
        {
            Node p = NewNode(Node.chr, s[i], 0);
            g.r.next = p; g.r = p;
        }
        g.l = dummyNode.next; dummyNode.next = null;
        return g;
    }

    public void SetContextTrans(Node p)
    { // set transition code in the graph rooted at p
        while (p != null)
        {
            if (p.typ == Node.chr || p.typ == Node.clas)
            {
                p.code = Node.contextTrans;
            }
            else if (p.typ == Node.opt || p.typ == Node.iter)
            {
                SetContextTrans(p.sub);
            }
            else if (p.typ == Node.alt)
            {
                SetContextTrans(p.sub); SetContextTrans(p.down);
            }
            if (p.up) break;
            p = p.next;
        }
    }

    //------------ graph deletability check -----------------

    public static bool DelGraph(Node p)
    {
        return p == null || DelNode(p) && DelGraph(p.next);
    }

    public static bool DelSubGraph(Node p)
    {
        return p == null || DelNode(p) && (p.up || DelSubGraph(p.next));
    }

    public static bool DelNode(Node p)
    {
        if (p.typ == Node.nt) return p.sym.deletable;
        else if (p.typ == Node.alt) return DelSubGraph(p.sub) || p.down != null && DelSubGraph(p.down);
        else return p.typ == Node.iter || p.typ == Node.opt || p.typ == Node.sem
            || p.typ == Node.eps || p.typ == Node.rslv || p.typ == Node.sync;
    }

    //----------------- graph printing ----------------------

    string Ptr(Node p, bool up)
    {
        string ptr = p == null ? "0" : p.n.ToString();
        return up ? "-" + ptr : ptr;
    }

    string Pos(Position pos)
    {
        if (pos == null) return "     "; else return string.Format("{0,5}", pos.Begin);
    }

    public string Name(string name)
    {
        return (name + "           ").Substring(0, 12);
        // found no simpler way to get the first 12 characters of the name
        // padded with blanks on the right
    }

    public void PrintNodes()
    {
        trace.WriteLine("Graph nodes:");
        trace.WriteLine("----------------------------------------------------");
        trace.WriteLine("   n type name          next  down   sub   pos  line");
        trace.WriteLine("                               val  code");
        trace.WriteLine("----------------------------------------------------");
        foreach (Node p in nodes)
        {
            trace.Write("{0,4} {1} ", p.n, nTyp[p.typ]);
            if (p.sym != null)
                trace.Write("{0,12} ", Name(p.sym.name));
            else if (p.typ == Node.clas)
            {
                CharClass c = classes[p.val];
                trace.Write("{0,12} ", Name(c.name));
            }
            else trace.Write("             ");
            trace.Write("{0,5} ", Ptr(p.next, p.up));
            switch (p.typ)
            {
                case Node.t:
                case Node.nt:
                case Node.wt:
                    trace.Write("             {0,5}", Pos(p.pos)); break;
                case Node.chr:
                    trace.Write("{0,5} {1,5}       ", p.val, p.code); break;
                case Node.clas:
                    trace.Write("      {0,5}       ", p.code); break;
                case Node.alt:
                case Node.iter:
                case Node.opt:
                    trace.Write("{0,5} {1,5}       ", Ptr(p.down, false), Ptr(p.sub, false)); break;
                case Node.sem:
                    trace.Write("             {0,5}", Pos(p.pos)); break;
                case Node.eps:
                case Node.any:
                case Node.sync:
                    trace.Write("                  "); break;
            }
            trace.WriteLine("{0,5}", p.line);
        }
        trace.WriteLine();
    }


    //---------------------------------------------------------------------
    //  Character class management
    //---------------------------------------------------------------------

    public List<CharClass> classes = new();
    public int dummyName = 'A';

    public CharClass NewCharClass(ReadOnlySpan<char> name, CharSet s)
    {
        if (name == "#")
            name = "#" + (char)dummyName++;
        var c = new CharClass(name.ToString(), s)
        {
            n = classes.Count
        };
        classes.Add(c);
        return c;
    }

    public CharClass FindCharClass(ReadOnlySpan<char> name)
    {
        foreach (var c in classes)
            if (name.Equals(c.name, StringComparison.Ordinal))
                return c;

        return null;
    }

    public CharClass FindCharClass(CharSet s)
    {
        foreach (var c in classes)
            if (s.Equals(c.set))
                return c;

        return null;
    }

    public CharSet CharClassSet(int i) => classes[i].set;

    //----------- character class printing

    string Ch(int ch)
    {
        if (ch < ' ' || ch >= 127 || ch == '\'' || ch == '\\') return ch.ToString();
        else return string.Format("'{0}'", (char)ch);
    }

    void WriteCharSet(CharSet s)
    {
        for (CharSet.Range r = s.head; r != null; r = r.next)
            if (r.from < r.to) { trace.Write(Ch(r.from) + ".." + Ch(r.to) + " "); }
            else { trace.Write(Ch(r.from) + " "); }
    }

    public void WriteCharClasses()
    {
        foreach (CharClass c in classes)
        {
            trace.Write("{0,-10}: ", c.name);
            WriteCharSet(c.set);
            trace.WriteLine();
        }
        trace.WriteLine();
    }


    //---------------------------------------------------------------------
    //  Symbol set computations
    //---------------------------------------------------------------------

    /* Computes the first set for the graph rooted at p */
    BitArray First0(Node p, BitArray mark)
    {
        BitArray fs = new BitArray(terminals.Count);
        while (p != null && !mark[p.n])
        {
            mark[p.n] = true;
            switch (p.typ)
            {
                case Node.nt:
                    {
                        if (p.sym.firstReady) fs.Or(p.sym.first);
                        else fs.Or(First0(p.sym.graph, mark));
                        break;
                    }
                case Node.t:
                case Node.wt:
                    {
                        fs[p.sym.n] = true; break;
                    }
                case Node.any:
                    {
                        fs.Or(p.set); break;
                    }
                case Node.alt:
                    {
                        fs.Or(First0(p.sub, mark));
                        fs.Or(First0(p.down, mark));
                        break;
                    }
                case Node.iter:
                case Node.opt:
                    {
                        fs.Or(First0(p.sub, mark));
                        break;
                    }
            }
            if (!DelNode(p)) break;
            p = p.next;
        }
        return fs;
    }

    public BitArray First(Node p)
    {
        BitArray fs = First0(p, new BitArray(nodes.Count));
        if (ddt[3])
        {
            trace.WriteLine();
            if (p != null) trace.WriteLine("First: node = {0}", p.n);
            else trace.WriteLine("First: node = null");
            PrintSet(fs, 0);
        }
        return fs;
    }

    void CompFirstSets()
    {
        foreach (Symbol sym in nonterminals)
        {
            sym.first = new BitArray(terminals.Count);
            sym.firstReady = false;
        }
        foreach (Symbol sym in nonterminals)
        {
            sym.first = First(sym.graph);
            sym.firstReady = true;
        }
    }

    void CompFollow(Node p)
    {
        while (p != null && !visited[p.n])
        {
            visited[p.n] = true;
            if (p.typ == Node.nt)
            {
                BitArray s = First(p.next);
                p.sym.follow.Or(s);
                if (DelGraph(p.next))
                    p.sym.nts[curSy.n] = true;
            }
            else if (p.typ == Node.opt || p.typ == Node.iter)
            {
                CompFollow(p.sub);
            }
            else if (p.typ == Node.alt)
            {
                CompFollow(p.sub); CompFollow(p.down);
            }
            p = p.next;
        }
    }

    void Complete(Symbol sym)
    {
        if (!visited[sym.n])
        {
            visited[sym.n] = true;
            foreach (Symbol s in nonterminals)
            {
                if (sym.nts[s.n])
                {
                    Complete(s);
                    sym.follow.Or(s.follow);
                    if (sym == curSy) sym.nts[s.n] = false;
                }
            }
        }
    }

    void CompFollowSets()
    {
        foreach (Symbol sym in nonterminals)
        {
            sym.follow = new BitArray(terminals.Count);
            sym.nts = new BitArray(nonterminals.Count);
        }
        gramSy.follow[eofSy.n] = true;
        visited = new BitArray(nodes.Count);
        foreach (Symbol sym in nonterminals)
        { // get direct successors of nonterminals
            curSy = sym;
            CompFollow(sym.graph);
        }
        foreach (Symbol sym in nonterminals)
        { // add indirect successors to followers
            visited = new BitArray(nonterminals.Count);
            curSy = sym;
            Complete(sym);
        }
    }

    Node LeadingAny(Node p)
    {
        if (p == null) return null;
        Node a = null;
        if (p.typ == Node.any) a = p;
        else if (p.typ == Node.alt)
        {
            a = LeadingAny(p.sub);
            if (a == null) a = LeadingAny(p.down);
        }
        else if (p.typ == Node.opt || p.typ == Node.iter) a = LeadingAny(p.sub);
        if (a == null && DelNode(p) && !p.up) a = LeadingAny(p.next);
        return a;
    }

    void FindAS(Node p)
    { // find ANY sets
        Node a;
        while (p != null)
        {
            if (p.typ == Node.opt || p.typ == Node.iter)
            {
                FindAS(p.sub);
                a = LeadingAny(p.sub);
                if (a != null) Sets.Except(a.set, First(p.next));
            }
            else if (p.typ == Node.alt)
            {
                BitArray s1 = new BitArray(terminals.Count);
                Node q = p;
                while (q != null)
                {
                    FindAS(q.sub);
                    a = LeadingAny(q.sub);
                    if (a != null)
                        Sets.Except(a.set, First(q.down).Or(s1));
                    else
                        s1.Or(First(q.sub));
                    q = q.down;
                }
            }

            // Remove alternative terminals before ANY, in the following
            // examples a and b must be removed from the ANY set:
            // [a] ANY, or {a|b} ANY, or [a][b] ANY, or (a|) ANY, or
            // A = [a]. A ANY
            if (DelNode(p))
            {
                a = LeadingAny(p.next);
                if (a != null)
                {
                    Node q = p.typ == Node.nt ? p.sym.graph : p.sub;
                    Sets.Except(a.set, First(q));
                }
            }

            if (p.up) break;
            p = p.next;
        }
    }

    void CompAnySets()
    {
        foreach (Symbol sym in nonterminals) FindAS(sym.graph);
    }

    public BitArray Expected(Node p, Symbol curSy)
    {
        BitArray s = First(p);
        if (DelGraph(p)) s.Or(curSy.follow);
        return s;
    }

    // does not look behind resolvers; only called during LL(1) test and in CheckRes
    public BitArray Expected0(Node p, Symbol curSy)
    {
        if (p.typ == Node.rslv) return new BitArray(terminals.Count);
        else return Expected(p, curSy);
    }

    void CompSync(Node p)
    {
        while (p != null && !visited[p.n])
        {
            visited[p.n] = true;
            if (p.typ == Node.sync)
            {
                BitArray s = Expected(p.next, curSy);
                s[eofSy.n] = true;
                allSyncSets.Or(s);
                p.set = s;
            }
            else if (p.typ == Node.alt)
            {
                CompSync(p.sub); CompSync(p.down);
            }
            else if (p.typ == Node.opt || p.typ == Node.iter)
                CompSync(p.sub);
            p = p.next;
        }
    }

    void CompSyncSets()
    {
        allSyncSets = new BitArray(terminals.Count);
        allSyncSets[eofSy.n] = true;
        visited = new BitArray(nodes.Count);
        foreach (Symbol sym in nonterminals)
        {
            curSy = sym;
            CompSync(curSy.graph);
        }
    }

    public void SetupAnys()
    {
        foreach (Node p in nodes)
            if (p.typ == Node.any)
            {
                p.set = new BitArray(terminals.Count, true);
                p.set[eofSy.n] = false;
            }
    }

    public void CompDeletableSymbols()
    {
        bool changed;
        do
        {
            changed = false;
            foreach (Symbol sym in nonterminals)
                if (!sym.deletable && sym.graph != null && DelGraph(sym.graph))
                {
                    sym.deletable = true; changed = true;
                }
        } while (changed);
        foreach (Symbol sym in nonterminals)
            if (sym.deletable) errors.Warning("  " + sym.name + " deletable");
    }

    public void RenumberPragmas()
    {
        int n = terminals.Count;
        foreach (Symbol sym in pragmas) sym.n = n++;
    }

    public void CompSymbolSets()
    {
        CompDeletableSymbols();
        CompFirstSets();
        CompAnySets();
        CompFollowSets();
        CompSyncSets();
        if (ddt[1])
        {
            trace.WriteLine();
            trace.WriteLine("First & follow symbols:");
            trace.WriteLine("----------------------"); trace.WriteLine();
            foreach (Symbol sym in nonterminals)
            {
                trace.WriteLine(sym.name);
                trace.Write("first:   "); PrintSet(sym.first, 10);
                trace.Write("follow:  "); PrintSet(sym.follow, 10);
                trace.WriteLine();
            }
        }
        if (ddt[4])
        {
            trace.WriteLine();
            trace.WriteLine("ANY and SYNC sets:");
            trace.WriteLine("-----------------");
            foreach (Node p in nodes)
                if (p.typ == Node.any || p.typ == Node.sync)
                {
                    trace.Write("{0,4} {1,4}: ", p.n, nTyp[p.typ]);
                    PrintSet(p.set, 11);
                }
        }
    }

    //---------------------------------------------------------------------
    //  String handling
    //---------------------------------------------------------------------

    char Hex2Char(ReadOnlySpan<char> s)
    {
        int val = 0;
        for (int i = 0; i < s.Length; i++)
        {
            char ch = s[i];
            if ('0' <= ch && ch <= '9') val = 16 * val + (ch - '0');
            else if ('a' <= ch && ch <= 'f') val = 16 * val + (10 + ch - 'a');
            else if ('A' <= ch && ch <= 'F') val = 16 * val + (10 + ch - 'A');
            else parser.SemErr("bad escape sequence in string or character");
        }
        if (val > char.MaxValue) /* pdt */
            parser.SemErr("bad escape sequence in string or character");
        return (char)val;
    }

    string Char2Hex(char ch) => Invariant($"\\u{(int)ch:x4}");

    public string Unescape(ReadOnlySpan<char> s)
    {
        /* replaces escape sequences in s by their Unicode values. */
        var buf = new StringBuilder(s.Length);
        int i = 0;
        while (i < s.Length)
        {
            if (s[i] == '\\')
            {
                switch (s[i + 1])
                {
                    case '\\': buf.Append('\\'); i += 2; break;
                    case '\'': buf.Append('\''); i += 2; break;
                    case '\"': buf.Append('\"'); i += 2; break;
                    case 'r': buf.Append('\r'); i += 2; break;
                    case 'n': buf.Append('\n'); i += 2; break;
                    case 't': buf.Append('\t'); i += 2; break;
                    case '0': buf.Append('\0'); i += 2; break;
                    case 'a': buf.Append('\a'); i += 2; break;
                    case 'b': buf.Append('\b'); i += 2; break;
                    case 'f': buf.Append('\f'); i += 2; break;
                    case 'v': buf.Append('\v'); i += 2; break;
                    case 'u':
                    case 'x':
                        if (i + 6 <= s.Length)
                        {
                            buf.Append(Hex2Char(s.Slice(i + 2, 4))); i += 6; break;
                        }
                        else
                        {
                            parser.SemErr("bad escape sequence in string or character"); i = s.Length; break;
                        }
                    default: parser.SemErr("bad escape sequence in string or character"); i += 2; break;
                }
            }
            else
            {
                buf.Append(s[i]);
                i++;
            }
        }
        return buf.ToString();
    }

    public string Escape(ReadOnlySpan<char> s)
    {
        var buf = new StringBuilder(s.Length);
        foreach (char ch in s)
        {
            switch (ch)
            {
                case '\\': buf.Append("\\\\"); break;
                case '\'': buf.Append("\\'"); break;
                case '\"': buf.Append("\\\""); break;
                case '\t': buf.Append("\\t"); break;
                case '\r': buf.Append("\\r"); break;
                case '\n': buf.Append("\\n"); break;
                default:
                    if (ch < ' ' || ch > '\u007f')
                        buf.Append(Char2Hex(ch));
                    else
                        buf.Append(ch);
                    break;
            }
        }
        return buf.ToString();
    }

    //---------------------------------------------------------------------
    //  Grammar checks
    //---------------------------------------------------------------------

    public bool GrammarOk()
    {
        bool ok = NtsComplete()
            && AllNtReached()
            && NoCircularProductions()
            && AllNtToTerm();
        if (ok) { CheckResolvers(); CheckLL1(); }
        return ok;
    }

    //--------------- check for circular productions ----------------------

    class CNode
    {   // node of list for finding circular productions
        public Symbol left, right;

        public CNode(Symbol l, Symbol r)
        {
            left = l; right = r;
        }
    }

    void GetSingles(Node p, ArrayList singles)
    {
        if (p == null) return;  // end of graph
        if (p.typ == Node.nt)
        {
            if (p.up || DelGraph(p.next)) singles.Add(p.sym);
        }
        else if (p.typ == Node.alt || p.typ == Node.iter || p.typ == Node.opt)
        {
            if (p.up || DelGraph(p.next))
            {
                GetSingles(p.sub, singles);
                if (p.typ == Node.alt) GetSingles(p.down, singles);
            }
        }
        if (!p.up && DelNode(p)) GetSingles(p.next, singles);
    }

    public bool NoCircularProductions()
    {
        bool ok, changed, onLeftSide, onRightSide;
        ArrayList list = new ArrayList();
        foreach (Symbol sym in nonterminals)
        {
            ArrayList singles = new ArrayList();
            GetSingles(sym.graph, singles); // get nonterminals s such that sym-->s
            foreach (Symbol s in singles) list.Add(new CNode(sym, s));
        }
        do
        {
            changed = false;
            for (int i = 0; i < list.Count; i++)
            {
                CNode n = (CNode)list[i];
                onLeftSide = false; onRightSide = false;
                foreach (CNode m in list)
                {
                    if (n.left == m.right) onRightSide = true;
                    if (n.right == m.left) onLeftSide = true;
                }
                if (!onLeftSide || !onRightSide)
                {
                    list.Remove(n); i--; changed = true;
                }
            }
        } while (changed);
        ok = true;
        foreach (CNode n in list)
        {
            ok = false;
            errors.SemErr("  " + n.left.name + " --> " + n.right.name);
        }
        return ok;
    }

    //--------------- check for LL(1) errors ----------------------

    void LL1Error(int cond, Symbol sym)
    {
        string s = "  LL1 warning in " + curSy.name + ": ";
        if (sym != null) s += sym.name + " is ";
        switch (cond)
        {
            case 1: s += "start of several alternatives"; break;
            case 2: s += "start & successor of deletable structure"; break;
            case 3: s += "an ANY node that matches no symbol"; break;
            case 4: s += "contents of [...] or {...} must not be deletable"; break;
        }
        errors.Warning(s);
    }

    void CheckOverlap(BitArray s1, BitArray s2, int cond)
    {
        foreach (Symbol sym in terminals)
        {
            if (s1[sym.n] && s2[sym.n]) LL1Error(cond, sym);
        }
    }

    void CheckAlts(Node p)
    {
        BitArray s1, s2;
        while (p != null)
        {
            if (p.typ == Node.alt)
            {
                Node q = p;
                s1 = new BitArray(terminals.Count);
                while (q != null)
                { // for all alternatives
                    s2 = Expected0(q.sub, curSy);
                    CheckOverlap(s1, s2, 1);
                    s1.Or(s2);
                    CheckAlts(q.sub);
                    q = q.down;
                }
            }
            else if (p.typ == Node.opt || p.typ == Node.iter)
            {
                if (DelSubGraph(p.sub)) LL1Error(4, null); // e.g. [[...]]
                else
                {
                    s1 = Expected0(p.sub, curSy);
                    s2 = Expected(p.next, curSy);
                    CheckOverlap(s1, s2, 2);
                }
                CheckAlts(p.sub);
            }
            else if (p.typ == Node.any)
            {
                if (Sets.GetCountOfSetBits(p.set) == 0) LL1Error(3, null);
                // e.g. {ANY} ANY or [ANY] ANY or ( ANY | ANY )
            }
            if (p.up) break;
            p = p.next;
        }
    }

    public void CheckLL1()
    {
        foreach (Symbol sym in nonterminals)
        {
            curSy = sym;
            CheckAlts(curSy.graph);
        }
    }

    //------------- check if resolvers are legal  --------------------

    void ResErr(Node p, string msg)
    {
        errors.Warning(p.line, p.pos.Column, msg);
    }

    void CheckRes(Node p, bool rslvAllowed)
    {
        while (p != null)
        {
            switch (p.typ)
            {
                case Node.alt:
                    BitArray expected = new BitArray(terminals.Count);
                    for (Node q = p; q != null; q = q.down)
                        expected.Or(Expected0(q.sub, curSy));
                    BitArray soFar = new BitArray(terminals.Count);
                    for (Node q = p; q != null; q = q.down)
                    {
                        if (q.sub.typ == Node.rslv)
                        {
                            BitArray fs = Expected(q.sub.next, curSy);
                            if (Sets.Overlaps(fs, soFar))
                                ResErr(q.sub, "Warning: Resolver will never be evaluated. " +
                                "Place it at previous conflicting alternative.");
                            if (!Sets.Overlaps(fs, expected))
                                ResErr(q.sub, "Warning: Misplaced resolver: no LL(1) conflict.");
                        }
                        else soFar.Or(Expected(q.sub, curSy));
                        CheckRes(q.sub, true);
                    }
                    break;
                case Node.iter:
                case Node.opt:
                    if (p.sub.typ == Node.rslv)
                    {
                        BitArray fs = First(p.sub.next);
                        BitArray fsNext = Expected(p.next, curSy);
                        if (!Sets.Overlaps(fs, fsNext))
                            ResErr(p.sub, "Warning: Misplaced resolver: no LL(1) conflict.");
                    }
                    CheckRes(p.sub, true);
                    break;
                case Node.rslv:
                    if (!rslvAllowed)
                        ResErr(p, "Warning: Misplaced resolver: no alternative.");
                    break;
            }
            if (p.up) break;
            p = p.next;
            rslvAllowed = false;
        }
    }

    public void CheckResolvers()
    {
        foreach (Symbol sym in nonterminals)
        {
            curSy = sym;
            CheckRes(curSy.graph, false);
        }
    }

    //------------- check if every nts has a production --------------------

    public bool NtsComplete()
    {
        bool complete = true;
        foreach (Symbol sym in nonterminals)
        {
            if (sym.graph == null)
            {
                complete = false;
                errors.SemErr("  No production for " + sym.name);
            }
        }
        return complete;
    }

    //-------------- check if every nts can be reached  -----------------

    void MarkReachedNts(Node p)
    {
        while (p != null)
        {
            if (p.typ == Node.nt && !visited[p.sym.n])
            { // new nt reached
                visited[p.sym.n] = true;
                MarkReachedNts(p.sym.graph);
            }
            else if (p.typ == Node.alt || p.typ == Node.iter || p.typ == Node.opt)
            {
                MarkReachedNts(p.sub);
                if (p.typ == Node.alt) MarkReachedNts(p.down);
            }
            if (p.up) break;
            p = p.next;
        }
    }

    public bool AllNtReached()
    {
        bool ok = true;
        visited = new BitArray(nonterminals.Count);
        visited[gramSy.n] = true;
        MarkReachedNts(gramSy.graph);
        foreach (Symbol sym in nonterminals)
        {
            if (!visited[sym.n])
            {
                ok = false;
                errors.Warning("  " + sym.name + " cannot be reached");
            }
        }
        return ok;
    }

    //--------- check if every nts can be derived to terminals  ------------

    bool IsTerm(Node p, BitArray mark)
    { // true if graph can be derived to terminals
        while (p != null)
        {
            if (p.typ == Node.nt && !mark[p.sym.n]) return false;
            if (p.typ == Node.alt && !IsTerm(p.sub, mark)
            && (p.down == null || !IsTerm(p.down, mark))) return false;
            if (p.up) break;
            p = p.next;
        }
        return true;
    }

    public bool AllNtToTerm()
    {
        bool changed, ok = true;
        BitArray mark = new BitArray(nonterminals.Count);
        // a nonterminal is marked if it can be derived to terminal symbols
        do
        {
            changed = false;
            foreach (Symbol sym in nonterminals)
                if (!mark[sym.n] && IsTerm(sym.graph, mark))
                {
                    mark[sym.n] = true; changed = true;
                }
        } while (changed);
        foreach (Symbol sym in nonterminals)
            if (!mark[sym.n])
            {
                ok = false;
                errors.SemErr("  " + sym.name + " cannot be derived to terminals");
            }
        return ok;
    }

    //---------------------------------------------------------------------
    //  Cross reference list
    //---------------------------------------------------------------------

    public void XRef()
    {
        SortedList xref = new SortedList(new SymbolComp());
        // collect lines where symbols have been defined
        foreach (Symbol sym in nonterminals)
        {
            ArrayList list = (ArrayList)xref[sym];
            if (list == null) { list = new ArrayList(); xref[sym] = list; }
            list.Add(-sym.line);
        }
        // collect lines where symbols have been referenced
        foreach (Node n in nodes)
        {
            if (n.typ == Node.t || n.typ == Node.wt || n.typ == Node.nt)
            {
                ArrayList list = (ArrayList)xref[n.sym];
                if (list == null) { list = new ArrayList(); xref[n.sym] = list; }
                list.Add(n.line);
            }
        }
        // print cross reference list
        trace.WriteLine();
        trace.WriteLine("Cross reference list:");
        trace.WriteLine("--------------------"); trace.WriteLine();
        foreach (Symbol sym in xref.Keys)
        {
            trace.Write("  {0,-12}", Name(sym.name));
            ArrayList list = (ArrayList)xref[sym];
            int col = 14;
            foreach (int line in list)
            {
                if (col + 5 > 80)
                {
                    trace.WriteLine();
                    for (col = 1; col <= 14; col++) trace.Write(" ");
                }
                trace.Write("{0,5}", line); col += 5;
            }
            trace.WriteLine();
        }
        trace.WriteLine(); trace.WriteLine();
    }

    public void SetTrace(ReadOnlySpan<char> s)
    {
        foreach (var ch in s)
        {
            var i = char.ToUpperInvariant(ch) switch
            {
                'A' or '0' => 0, // trace automaton
                'F' or '1' => 1, // list first/follow sets
                'G' or '2' => 2, // print syntax graph
                'I' or '3' => 3, // trace computation of first sets
                'J' or '4' => 4, // print ANY and SYNC sets
                'P' or '8' => 8, // print statistics
                'S' or '6' => 6, // list symbol table
                'X' or '7' => 7, // list cross reference table
                _ => throw new Exception($"Unknown trace option '{ch}'."),
            };

            ddt[i] = true;
        }
    }

    public void SetOption(ReadOnlySpan<char> s)
    {
        string[] option = s.ToString().Split(new char[] { '=' }, 2);
        string name = option[0];
        string value = option[1].Trim();

        if ("$namespace".Equals(name, StringComparison.Ordinal))
        {
            if (nsName == null)
                nsName = value;
        }
        else if ("$checkEOF".Equals(name, StringComparison.Ordinal))
        {
            checkEOF = "true".Equals(value, StringComparison.Ordinal);
        }
        else if ("$compatibility".Equals(name, StringComparison.Ordinal))
        {
            // TODO

            // This is just a future implementation stub.
            if (!value.Equals("Turbo Coco/R 2022.1", StringComparison.OrdinalIgnoreCase))
                parser.SynErr("unsupported compatibility version");
        }
        else if ("$lang".Equals(name, StringComparison.Ordinal))
        {
            // TODO

            // This is just a future implementation stub.
            if (!value.Equals("C#", StringComparison.OrdinalIgnoreCase))
                parser.SynErr("unsupported language");
        }
        else
        {
            parser.SynErr("unrecognized pragma option");
        }
    }

    class SymbolComp : IComparer
    {
        public int Compare(object x, object y)
        {
            return ((Symbol)x).name.CompareTo(((Symbol)y).name);
        }
    }

}
