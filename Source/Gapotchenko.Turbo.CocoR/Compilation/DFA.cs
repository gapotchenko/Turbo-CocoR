// DFA.cs - Generation of the Scanner Automaton

using System.Text;
using System.Collections;
using Gapotchenko.Turbo.CocoR.Compilation.Grammar;
using Gapotchenko.Turbo.CocoR.Framework.Collections;
using Gapotchenko.Turbo.CocoR.Compilation.CodeGeneration;

namespace Gapotchenko.Turbo.CocoR.Compilation;

//-----------------------------------------------------------------------------
//  State
//-----------------------------------------------------------------------------

/// <summary>
/// The state of a finite automaton.
/// </summary>
class DfaState
{
    public int nr;                      // state number
    public DfaAction firstAction;// to first action of this state
    public Symbol endOf;            // recognized token if state is final
    public bool ctx;                    // true if state is reached via contextTrans
    public DfaState next;

    public void AddAction(DfaAction act)
    {
        DfaAction lasta = null, a = firstAction;
        while (a != null && act.typ >= a.typ)
        {
            lasta = a;
            a = a.next;
        }
        // collecting classes at the beginning gives better performance
        act.next = a;
        if (a == firstAction)
            firstAction = act;
        else
            lasta.next = act;
    }

    public void DetachAction(DfaAction act)
    {
        DfaAction lasta = null, a = firstAction;
        while (a != null && a != act)
        {
            lasta = a;
            a = a.next;
        }
        if (a != null)
            if (a == firstAction)
                firstAction = a.next;
            else
                lasta.next = a.next;
    }

    public void MeltWith(DfaState s)
    {
        // copy actions of s to state
        for (DfaAction action = s.firstAction; action != null; action = action.next)
        {
            var a = new DfaAction(action.typ, action.sym, action.tc);
            a.AddTargets(action);
            AddAction(a);
        }
    }

}

//-----------------------------------------------------------------------------
//  Action
//-----------------------------------------------------------------------------

/// <summary>
/// The action of a finite automaton.
/// </summary>
class DfaAction
{
    public int typ;                 // type of action symbol: clas, chr
    public int sym;                 // action symbol
    public int tc;                  // transition code: normalTrans, contextTrans
    public DfaTarget target;       // states reached from this action
    public DfaAction next;

    public DfaAction(int typ, int sym, int tc)
    {
        this.typ = typ; this.sym = sym; this.tc = tc;
    }

    public void AddTarget(DfaTarget t)
    {
        // add t to the action.targets
        DfaTarget last = null;
        var p = target;
        while (p != null && t.state.nr >= p.state.nr)
        {
            if (t.state == p.state)
                return;
            last = p;
            p = p.next;
        }
        t.next = p;
        if (p == target)
            target = t;
        else
            last.next = t;
    }

    public void AddTargets(DfaAction a)
    {
        // add copy of a.targets to action.targets
        for (var p = a.target; p != null; p = p.next)
        {
            var t = new DfaTarget(p.state);
            AddTarget(t);
        }
        if (a.tc == Node.contextTrans)
            tc = Node.contextTrans;
    }

    public CharSet Symbols(Tab tab)
    {
        CharSet s;
        if (typ == Node.clas)
        {
            s = tab.CharClassSet(sym).Clone();
        }
        else
        {
            s = new CharSet();
            s.Add(sym);
        }
        return s;
    }

    public void ShiftWith(CharSet s, Tab tab)
    {
        if (s.Elements() == 1)
        {
            typ = Node.chr; sym = s.First();
        }
        else
        {
            CharClass c = tab.FindCharClass(s);
            if (c == null) c = tab.NewCharClass("#", s); // class with dummy name
            typ = Node.clas; sym = c.n;
        }
    }

}

//-----------------------------------------------------------------------------
//  Target
//-----------------------------------------------------------------------------

/// <summary>
/// The set of states that are reached by an action.
/// </summary>
class DfaTarget
{
    /// <summary>
    /// The target state.
    /// </summary>
    public DfaState state;

    public DfaTarget next;

    public DfaTarget(DfaState s)
    {
        state = s;
    }
}

//-----------------------------------------------------------------------------
//  Melted
//-----------------------------------------------------------------------------

class DfaMelted
{                   // info about melted states
    public BitArray set;                // set of old states
    public DfaState state;                 // new state
    public DfaMelted next;

    public DfaMelted(BitArray set, DfaState state)
    {
        this.set = set; this.state = state;
    }
}

//-----------------------------------------------------------------------------
//  Comment
//-----------------------------------------------------------------------------

/// <summary>
/// The information about comment syntax.
/// </summary>
sealed class Comment
{
    public Comment(string start, string stop, bool nested)
    {
        Start = start;
        Stop = stop;
        Nested = nested;
    }

    public string Start { get; }
    public string Stop { get; }
    public bool Nested { get; }
    public required Comment Next { get; init; }
}

/// <summary>
/// The set of characters.
/// </summary>
sealed class CharSet
{
    public class Range
    {
        public int from, to;
        public Range next;
        public Range(int from, int to) { this.from = from; this.to = to; }
    }

    public Range head;

    public bool this[int i]
    {
        get
        {
            for (var p = head; p != null; p = p.next)
            {
                if (i < p.from)
                    return false;
                else if (i <= p.to)
                    return true; // p.from <= i <= p.to
            }
            return false;
        }
    }

    public void Add(int i)
    {
        Range? cur = head, prev = null;
        while (cur != null && i >= cur.from - 1)
        {
            if (i <= cur.to + 1)
            {
                // (cur.from-1) <= i <= (cur.to+1)
                if (i == cur.from - 1)
                {
                    cur.from--;
                }
                else if (i == cur.to + 1)
                {
                    cur.to++;
                    var next = cur.next;
                    if (next != null && cur.to == next.from - 1)
                    {
                        cur.to = next.to;
                        cur.next = next.next;
                    };
                }
                return;
            }
            prev = cur;
            cur = cur.next;
        }
        var n = new Range(i, i)
        {
            next = cur
        };
        if (prev == null)
            head = n;
        else
            prev.next = n;
    }

    public CharSet Clone()
    {
        var s = new CharSet();
        Range prev = null;
        for (var cur = head; cur != null; cur = cur.next)
        {
            var r = new Range(cur.from, cur.to);
            if (prev == null)
                s.head = r;
            else
                prev.next = r;
            prev = r;
        }
        return s;
    }

    public bool SetEquals(CharSet s)
    {
        var p = head;
        var q = s.head;
        while (p != null && q != null)
        {
            if (p.from != q.from || p.to != q.to)
                return false;
            p = p.next;
            q = q.next;
        }
        return p == q;
    }

    public int Elements()
    {
        int n = 0;
        for (var p = head; p != null; p = p.next)
            n += p.to - p.from + 1;
        return n;
    }

    public int First()
    {
        if (head != null)
            return head.from;
        return -1;
    }

    public void UnionWith(CharSet s)
    {
        for (var p = s.head; p != null; p = p.next)
            for (int i = p.from; i <= p.to; i++)
                Add(i);
    }

    public void IntersectWith(CharSet s)
    {
        var x = new CharSet();
        for (var p = head; p != null; p = p.next)
            for (int i = p.from; i <= p.to; i++)
                if (s[i])
                    x.Add(i);
        head = x.head;
    }

    public void ExceptWith(CharSet s)
    {
        var x = new CharSet();
        for (var p = head; p != null; p = p.next)
            for (int i = p.from; i <= p.to; i++)
                if (!s[i])
                    x.Add(i);
        head = x.head;
    }

    public bool Includes(CharSet s)
    {
        for (var p = s.head; p != null; p = p.next)
            for (int i = p.from; i <= p.to; i++)
                if (!this[i])
                    return false;
        return true;
    }

    public bool Overlaps(CharSet s)
    {
        for (var p = s.head; p != null; p = p.next)
            for (int i = p.from; i <= p.to; i++)
                if (this[i])
                    return true;
        return false;
    }

    public void Fill()
    {
        head = new Range(char.MinValue, char.MaxValue);
    }
}

//-----------------------------------------------------------------------------
//  DFA
//-----------------------------------------------------------------------------

class DFA
{
    int maxStates;
    int lastStateNr;   // highest state number
    DfaState firstState;
    DfaState lastState;   // last allocated state
    int lastSimState;  // last non melted state
    TextWriter gen;  // generated scanner file
    Symbol curSy;      // current token to be recognized (in FindTrans)
    bool dirtyDFA;     // DFA may become nondeterministic in MatchLiteral

    public bool ignoreCase;   // true if input should be treated case-insensitively
    public bool hasCtxMoves;  // DFA has context transitions

    // other Coco objects
    Parser parser;
    Tab tab;
    Errors errors;
    TextWriter trace;

    //---------- Output primitives
    string Ch(int ch)
    {
        if (ch < ' ' || ch >= 127 || ch == '\'' || ch == '\\') return Convert.ToString(ch);
        else return string.Format("'{0}'", (char)ch);
    }

    string ChCond(char ch)
    {
        return string.Format("ch == {0}", Ch(ch));
    }

    void PutRange(CharSet s)
    {
        for (CharSet.Range r = s.head; r != null; r = r.next)
        {
            if (r.from == r.to) { gen.Write("ch == " + Ch(r.from)); }
            else if (r.from == 0) { gen.Write("ch <= " + Ch(r.to)); }
            else { gen.Write("ch >= " + Ch(r.from) + " && ch <= " + Ch(r.to)); }
            if (r.next != null) gen.Write(" || ");
        }
    }

    //---------- State handling

    DfaState NewState()
    {
        DfaState s = new DfaState(); s.nr = ++lastStateNr;
        if (firstState == null) firstState = s; else lastState.next = s;
        lastState = s;
        return s;
    }

    void NewTransition(DfaState from, DfaState to, int typ, int sym, int tc)
    {
        DfaTarget t = new DfaTarget(to);
        DfaAction a = new DfaAction(typ, sym, tc); a.target = t;
        from.AddAction(a);
        if (typ == Node.clas) curSy.tokenKind = Symbol.classToken;
    }

    void CombineShifts()
    {
        DfaState state;
        DfaAction a, b, c;
        CharSet seta, setb;
        for (state = firstState; state != null; state = state.next)
        {
            for (a = state.firstAction; a != null; a = a.next)
            {
                b = a.next;
                while (b != null)
                    if (a.target.state == b.target.state && a.tc == b.tc)
                    {
                        seta = a.Symbols(tab); setb = b.Symbols(tab);
                        seta.UnionWith(setb);
                        a.ShiftWith(seta, tab);
                        c = b; b = b.next; state.DetachAction(c);
                    }
                    else b = b.next;
            }
        }
    }

    void FindUsedStates(DfaState state, BitArray used)
    {
        if (used[state.nr]) return;
        used[state.nr] = true;
        for (DfaAction a = state.firstAction; a != null; a = a.next)
            FindUsedStates(a.target.state, used);
    }

    void DeleteRedundantStates()
    {
        DfaState[] newState = new DfaState[lastStateNr + 1];
        BitArray used = new BitArray(lastStateNr + 1);
        FindUsedStates(firstState, used);
        // combine equal final states
        for (DfaState s1 = firstState.next; s1 != null; s1 = s1.next) // firstState cannot be final
            if (used[s1.nr] && s1.endOf != null && s1.firstAction == null && !s1.ctx)
                for (DfaState s2 = s1.next; s2 != null; s2 = s2.next)
                    if (used[s2.nr] && s1.endOf == s2.endOf && s2.firstAction == null & !s2.ctx)
                    {
                        used[s2.nr] = false; newState[s2.nr] = s1;
                    }
        for (DfaState state = firstState; state != null; state = state.next)
            if (used[state.nr])
                for (DfaAction a = state.firstAction; a != null; a = a.next)
                    if (!used[a.target.state.nr])
                        a.target.state = newState[a.target.state.nr];
        // delete unused states
        lastState = firstState; lastStateNr = 0; // firstState has number 0
        for (DfaState state = firstState.next; state != null; state = state.next)
            if (used[state.nr]) { state.nr = ++lastStateNr; lastState = state; }
            else lastState.next = state.next;
    }

    DfaState TheState(Node p)
    {
        DfaState state;
        if (p == null) { state = NewState(); state.endOf = curSy; return state; }
        else return p.state;
    }

    void Step(DfaState from, Node p, BitArray stepped)
    {
        if (p == null) return;
        stepped[p.n] = true;
        switch (p.typ)
        {
            case Node.clas:
            case Node.chr:
                {
                    NewTransition(from, TheState(p.next), p.typ, p.val, p.code);
                    break;
                }
            case Node.alt:
                {
                    Step(from, p.sub, stepped); Step(from, p.down, stepped);
                    break;
                }
            case Node.iter:
                {
                    if (Tab.DelSubGraph(p.sub))
                    {
                        parser.SemErr("contents of {...} must not be deletable");
                        return;
                    }
                    if (p.next != null && !stepped[p.next.n]) Step(from, p.next, stepped);
                    Step(from, p.sub, stepped);
                    if (p.state != from)
                    {
                        Step(p.state, p, new BitArray(tab.nodes.Count));
                    }
                    break;
                }
            case Node.opt:
                {
                    if (p.next != null && !stepped[p.next.n]) Step(from, p.next, stepped);
                    Step(from, p.sub, stepped);
                    break;
                }
        }
    }

    // Assigns a state n.state to every node n. There will be a transition from
    // n.state to n.next.state triggered by n.val. All nodes in an alternative
    // chain are represented by the same state.
    // Numbering scheme:
    //  - any node after a chr, clas, opt, or alt, must get a new number
    //  - if a nested structure starts with an iteration the iter node must get a new number
    //  - if an iteration follows an iteration, it must get a new number
    void NumberNodes(Node p, DfaState state, bool renumIter)
    {
        if (p == null) return;
        if (p.state != null) return; // already visited;
        if (state == null || p.typ == Node.iter && renumIter) state = NewState();
        p.state = state;
        if (Tab.DelGraph(p)) state.endOf = curSy;
        switch (p.typ)
        {
            case Node.clas:
            case Node.chr:
                {
                    NumberNodes(p.next, null, false);
                    break;
                }
            case Node.opt:
                {
                    NumberNodes(p.next, null, false);
                    NumberNodes(p.sub, state, true);
                    break;
                }
            case Node.iter:
                {
                    NumberNodes(p.next, state, true);
                    NumberNodes(p.sub, state, true);
                    break;
                }
            case Node.alt:
                {
                    NumberNodes(p.next, null, false);
                    NumberNodes(p.sub, state, true);
                    NumberNodes(p.down, state, renumIter);
                    break;
                }
        }
    }

    void FindTrans(Node p, bool start, BitArray marked)
    {
        if (p == null || marked[p.n]) return;
        marked[p.n] = true;
        if (start) Step(p.state, p, new BitArray(tab.nodes.Count)); // start of group of equally numbered nodes
        switch (p.typ)
        {
            case Node.clas:
            case Node.chr:
                {
                    FindTrans(p.next, true, marked);
                    break;
                }
            case Node.opt:
                {
                    FindTrans(p.next, true, marked); FindTrans(p.sub, false, marked);
                    break;
                }
            case Node.iter:
                {
                    FindTrans(p.next, false, marked); FindTrans(p.sub, false, marked);
                    break;
                }
            case Node.alt:
                {
                    FindTrans(p.sub, false, marked); FindTrans(p.down, false, marked);
                    break;
                }
        }
    }

    public void ConvertToStates(Node p, Symbol sym)
    {
        curSy = sym;
        if (Tab.DelGraph(p))
        {
            parser.SemErr("token might be empty");
            return;
        }
        NumberNodes(p, firstState, true);
        FindTrans(p, true, new BitArray(tab.nodes.Count));
        if (p.typ == Node.iter)
        {
            Step(firstState, p, new BitArray(tab.nodes.Count));
        }
    }

    // match string against current automaton; store it either as a fixedToken or as a litToken
    public void MatchLiteral(string s, Symbol sym)
    {
        s = tab.Unescape(s.Substring(1, s.Length - 2));
        int i, len = s.Length;
        DfaState state = firstState;
        DfaAction a = null;
        for (i = 0; i < len; i++)
        { // try to match s against existing DFA
            a = FindAction(state, s[i]);
            if (a == null) break;
            state = a.target.state;
        }
        // if s was not totally consumed or leads to a non-final state => make new DFA from it
        if (i != len || state.endOf == null)
        {
            state = firstState; i = 0; a = null;
            dirtyDFA = true;
        }
        for (; i < len; i++)
        { // make new DFA for s[i..len-1], ML: i is either 0 or len
            DfaState to = NewState();
            NewTransition(state, to, Node.chr, s[i], Node.normalTrans);
            state = to;
        }
        Symbol matchedSym = state.endOf;
        if (state.endOf == null)
        {
            state.endOf = sym;
        }
        else if (matchedSym.tokenKind == Symbol.fixedToken || a != null && a.tc == Node.contextTrans)
        {
            // s matched a token with a fixed definition or a token with an appendix that will be cut off
            parser.SemErr("tokens " + sym.name + " and " + matchedSym.name + " cannot be distinguished");
        }
        else
        {
            // matchedSym == classToken || classLitToken
            matchedSym.tokenKind = Symbol.classLitToken;
            sym.tokenKind = Symbol.litToken;
        }
    }

    void SplitActions(DfaState state, DfaAction a, DfaAction b)
    {
        DfaAction c; CharSet seta, setb, setc;
        seta = a.Symbols(tab); setb = b.Symbols(tab);
        if (seta.SetEquals(setb))
        {
            a.AddTargets(b);
            state.DetachAction(b);
        }
        else if (seta.Includes(setb))
        {
            setc = seta.Clone(); setc.ExceptWith(setb);
            b.AddTargets(a);
            a.ShiftWith(setc, tab);
        }
        else if (setb.Includes(seta))
        {
            setc = setb.Clone(); setc.ExceptWith(seta);
            a.AddTargets(b);
            b.ShiftWith(setc, tab);
        }
        else
        {
            setc = seta.Clone(); setc.IntersectWith(setb);
            seta.ExceptWith(setc);
            setb.ExceptWith(setc);
            a.ShiftWith(seta, tab);
            b.ShiftWith(setb, tab);
            c = new DfaAction(0, 0, Node.normalTrans);  // typ and sym are set in ShiftWith
            c.AddTargets(a);
            c.AddTargets(b);
            c.ShiftWith(setc, tab);
            state.AddAction(c);
        }
    }

    bool Overlap(DfaAction a, DfaAction b)
    {
        CharSet seta, setb;
        if (a.typ == Node.chr)
            if (b.typ == Node.chr) return a.sym == b.sym;
            else { setb = tab.CharClassSet(b.sym); return setb[a.sym]; }
        else
        {
            seta = tab.CharClassSet(a.sym);
            if (b.typ == Node.chr) return seta[b.sym];
            else { setb = tab.CharClassSet(b.sym); return seta.Overlaps(setb); }
        }
    }

    void MakeUnique(DfaState state)
    {
        bool changed;
        do
        {
            changed = false;
            for (var a = state.firstAction; a != null; a = a.next)
            {
                for (var b = a.next; b != null; b = b.next)
                {
                    if (Overlap(a, b))
                    {
                        SplitActions(state, a, b);
                        changed = true;
                    }
                }
            }
        } while (changed);
    }

    void MeltStates(DfaState state)
    {
        for (var action = state.firstAction; action != null; action = action.next)
        {
            if (action.target.next != null)
            {
                GetTargetStates(action, out var targets, out var endOf, out var ctx);
                var melt = StateWithSet(targets);
                if (melt == null)
                {
                    var s = NewState();
                    s.endOf = endOf;
                    s.ctx = ctx;
                    for (var target = action.target; target != null; target = target.next)
                        s.MeltWith(target.state);
                    MakeUnique(s);
                    melt = NewMelted(targets, s);
                }
                action.target.next = null;
                action.target.state = melt.state;
            }
        }
    }

    void FindCtxStates()
    {
        for (var state = firstState; state != null; state = state.next)
            for (var a = state.firstAction; a != null; a = a.next)
                if (a.tc == Node.contextTrans)
                    a.target.state.ctx = true;
    }

    public void MakeDeterministic()
    {
        lastSimState = lastState.nr;
        maxStates = 2 * lastSimState; // heuristic for set size in Melted.set
        FindCtxStates();
        for (var state = firstState; state != null; state = state.next)
            MakeUnique(state);
        for (var state = firstState; state != null; state = state.next)
            MeltStates(state);
        DeleteRedundantStates();
        CombineShifts();
    }

    public void PrintStates()
    {
        trace.WriteLine();
        trace.WriteLine("---------- states ----------");
        for (DfaState state = firstState; state != null; state = state.next)
        {
            if (state.endOf == null)
                trace.Write("               ");
            else
                trace.Write("E({0,12})", tab.Name(state.endOf.name));
            trace.Write("{0,3}:", state.nr);
            if (state.firstAction == null)
                trace.WriteLine();

            bool first = true;
            for (var action = state.firstAction; action != null; action = action.next)
            {
                if (first)
                {
                    trace.Write(" ");
                    first = false;
                }
                else
                {
                    trace.Write("                    ");
                }

                if (action.typ == Node.clas)
                    trace.Write(tab.classes[action.sym].name);
                else
                    trace.Write("{0, 3}", Ch(action.sym));

                for (var target = action.target; target != null; target = target.next)
                    trace.Write(" {0, 3}", target.state.nr);

                if (action.tc == Node.contextTrans)
                    trace.WriteLine(" context");
                else
                    trace.WriteLine();
            }
        }
        trace.WriteLine();
        trace.WriteLine("---------- character classes ----------");
        tab.WriteCharClasses();
    }

    //---------------------------- actions --------------------------------

    public DfaAction FindAction(DfaState state, char ch)
    {
        for (DfaAction a = state.firstAction; a != null; a = a.next)
            if (a.typ == Node.chr && ch == a.sym) return a;
            else if (a.typ == Node.clas)
            {
                CharSet s = tab.CharClassSet(a.sym);
                if (s[ch]) return a;
            }
        return null;
    }

    public void GetTargetStates(DfaAction a, out BitArray targets, out Symbol endOf, out bool ctx)
    {
        // compute the set of target states
        targets = new BitArray(maxStates); endOf = null;
        ctx = false;
        for (DfaTarget t = a.target; t != null; t = t.next)
        {
            int stateNr = t.state.nr;
            if (stateNr <= lastSimState) targets[stateNr] = true;
            else targets.Or(MeltedSet(stateNr));
            if (t.state.endOf != null)
                if (endOf == null || endOf == t.state.endOf)
                    endOf = t.state.endOf;
                else
                    errors.SemErr("Tokens " + endOf.name + " and " + t.state.endOf.name + " cannot be distinguished");
            if (t.state.ctx)
            {
                ctx = true;
                // The following check seems to be unnecessary. It reported an error
                // if a symbol + context was the prefix of another symbol, e.g.
                //   s1 = "a" "b" "c".
                //   s2 = "a" CONTEXT("b").
                // But this is ok.
                // if (t.state.endOf != null) {
                //   Console.WriteLine("Ambiguous context clause");
                //	 errors.count++;
                // }
            }
        }
    }

    //------------------------- melted states ------------------------------

    DfaMelted firstMelted; // head of melted state list

    DfaMelted NewMelted(BitArray set, DfaState state)
    {
        DfaMelted m = new DfaMelted(set, state);
        m.next = firstMelted; firstMelted = m;
        return m;
    }

    BitArray MeltedSet(int nr)
    {
        DfaMelted m = firstMelted;
        while (m != null)
        {
            if (m.state.nr == nr) return m.set; else m = m.next;
        }
        throw new Exception("compiler error in Melted.Set");
    }

    DfaMelted StateWithSet(BitArray s)
    {
        for (DfaMelted m = firstMelted; m != null; m = m.next)
            if (s.SetEquals(m.set))
                return m;
        return null;
    }

    //------------------------ comments --------------------------------

    public Comment firstComment;    // list of comments

    string CommentStr(Node p)
    {
        StringBuilder s = new StringBuilder();
        while (p != null)
        {
            if (p.typ == Node.chr)
            {
                s.Append((char)p.val);
            }
            else if (p.typ == Node.clas)
            {
                CharSet set = tab.CharClassSet(p.val);
                if (set.Elements() != 1) parser.SemErr("character set contains more than 1 character");
                s.Append((char)set.First());
            }
            else parser.SemErr("comment delimiters may not be structured");
            p = p.next;
        }
        if (s.Length == 0 || s.Length > 2)
        {
            parser.SemErr("comment delimiters must be 1 or 2 characters long");
            s = new StringBuilder("?");
        }
        return s.ToString();
    }

    public void NewComment(Node from, Node to, bool nested)
    {
        var c = new Comment(CommentStr(from), CommentStr(to), nested)
        {
            Next = firstComment
        };
        firstComment = c;
    }


    //------------------------ scanner generation ----------------------

    void GenComBody(Comment com)
    {
        gen.WriteLine("\t\t\tfor(;;) {");
        gen.Write("\t\t\t\tif ({0}) ", ChCond(com.Stop[0])); gen.WriteLine("{");
        if (com.Stop.Length == 1)
        {
            gen.WriteLine("\t\t\t\t\tlevel--;");
            gen.WriteLine("\t\t\t\t\tif (level == 0) { oldEols = line - line0; NextCh(); return true; }");
            gen.WriteLine("\t\t\t\t\tNextCh();");
        }
        else
        {
            gen.WriteLine("\t\t\t\t\tNextCh();");
            gen.WriteLine("\t\t\t\t\tif ({0}) {{", ChCond(com.Stop[1]));
            gen.WriteLine("\t\t\t\t\t\tlevel--;");
            gen.WriteLine("\t\t\t\t\t\tif (level == 0) { oldEols = line - line0; NextCh(); return true; }");
            gen.WriteLine("\t\t\t\t\t\tNextCh();");
            gen.WriteLine("\t\t\t\t\t}");
        }
        if (com.Nested)
        {
            gen.Write("\t\t\t\t}"); gen.Write(" else if ({0}) ", ChCond(com.Start[0])); gen.WriteLine("{");
            if (com.Start.Length == 1)
                gen.WriteLine("\t\t\t\t\tlevel++; NextCh();");
            else
            {
                gen.WriteLine("\t\t\t\t\tNextCh();");
                gen.Write("\t\t\t\t\tif ({0}) ", ChCond(com.Start[1])); gen.WriteLine("{");
                gen.WriteLine("\t\t\t\t\t\tlevel++; NextCh();");
                gen.WriteLine("\t\t\t\t\t}");
            }
        }
        gen.WriteLine("\t\t\t\t} else if (ch == Buffer.EOF) return false;");
        gen.WriteLine("\t\t\t\telse NextCh();");
        gen.WriteLine("\t\t\t}");
    }

    void GenComment(Comment com, int i)
    {
        gen.WriteLine();
        gen.Write("\tbool Comment{0}() ", i); gen.WriteLine("{");
        gen.WriteLine("\t\tint level = 1, pos0 = pos, line0 = line, col0 = col, charPos0 = charPos;");
        if (com.Start.Length == 1)
        {
            gen.WriteLine("\t\tNextCh();");
            GenComBody(com);
        }
        else
        {
            gen.WriteLine("\t\tNextCh();");
            gen.Write("\t\tif ({0}) ", ChCond(com.Start[1])); gen.WriteLine("{");
            gen.WriteLine("\t\t\tNextCh();");
            GenComBody(com);
            gen.WriteLine("\t\t} else {");
            gen.WriteLine("\t\t\tbuffer.Pos = pos0; NextCh(); line = line0; col = col0; charPos = charPos0;");
            gen.WriteLine("\t\t}");
            gen.WriteLine("\t\treturn false;");
        }
        gen.WriteLine("\t}");
    }

    string SymName(Symbol sym)
    {
        if (char.IsLetter(sym.name[0]))
        { // real name value is stored in Tab.literals
            foreach (DictionaryEntry e in tab.literals)
                if ((Symbol)e.Value == sym) return (string)e.Key;
        }
        return sym.name;
    }

    void GenLiterals()
    {
        if (ignoreCase)
        {
            gen.WriteLine("\t\tswitch (t.val.ToLower()) {");
        }
        else
        {
            gen.WriteLine("\t\tswitch (t.val) {");
        }
        foreach (IList ts in new IList[] { tab.terminals, tab.pragmas })
        {
            foreach (Symbol sym in ts)
            {
                if (sym.tokenKind == Symbol.litToken)
                {
                    string name = SymName(sym);
                    if (ignoreCase) name = name.ToLower();
                    // sym.name stores literals with quotes, e.g. "\"Literal\""
                    gen.WriteLine("\t\t\tcase {0}: t.kind = {1}; break;", name, sym.n);
                }
            }
        }
        gen.WriteLine("\t\t\tdefault: break;");
        gen.Write("\t\t}");
    }

    void WriteState(DfaState state)
    {
        Symbol endOf = state.endOf;
        gen.WriteLine("\t\t\tcase {0}:", state.nr);
        if (endOf != null && state.firstAction != null)
        {
            gen.WriteLine("\t\t\t\trecEnd = pos; recKind = {0};", endOf.n);
        }
        bool ctxEnd = state.ctx;
        for (DfaAction action = state.firstAction; action != null; action = action.next)
        {
            if (action == state.firstAction) gen.Write("\t\t\t\tif (");
            else gen.Write("\t\t\t\telse if (");
            if (action.typ == Node.chr) gen.Write(ChCond((char)action.sym));
            else PutRange(tab.CharClassSet(action.sym));
            gen.Write(") {");
            if (action.tc == Node.contextTrans)
            {
                gen.Write("apx++; "); ctxEnd = false;
            }
            else if (state.ctx)
                gen.Write("apx = 0; ");
            gen.Write("AddCh(); goto case {0};", action.target.state.nr);
            gen.WriteLine("}");
        }
        if (state.firstAction == null)
            gen.Write("\t\t\t\t{");
        else
            gen.Write("\t\t\t\telse {");
        if (ctxEnd)
        { // final context state: cut appendix
            gen.WriteLine();
            gen.WriteLine("\t\t\t\t\ttlen -= apx;");
            gen.WriteLine("\t\t\t\t\tSetScannerBehindT();");
            gen.Write("\t\t\t\t\t");
        }
        if (endOf == null)
        {
            gen.WriteLine("goto case 0;}");
        }
        else
        {
            gen.Write("t.kind = {0}; ", endOf.n);
            if (endOf.tokenKind == Symbol.classLitToken)
            {
                gen.WriteLine("t.val = new String(tval, 0, tlen); CheckLiteral(); return t;}");
            }
            else
            {
                gen.WriteLine("break;}");
            }
        }
    }

    void WriteStartTab()
    {
        for (DfaAction action = firstState.firstAction; action != null; action = action.next)
        {
            int targetState = action.target.state.nr;
            if (action.typ == Node.chr)
            {
                gen.WriteLine("\t\tstart[" + action.sym + "] = " + targetState + "; ");
            }
            else
            {
                CharSet s = tab.CharClassSet(action.sym);
                for (CharSet.Range r = s.head; r != null; r = r.next)
                {
                    gen.WriteLine("\t\tfor (int i = " + r.from + "; i <= " + r.to + "; ++i) start[i] = " + targetState + ";");
                }
            }
        }
        gen.WriteLine("\t\tstart[Buffer.EOF] = -1;");
    }

    public void WriteScanner()
    {
        var cgs = tab.CodeGenerationService;

        using var frame = cgs.OpenFrame(FrameFileNames.Scanner);
        using var codeWriter = cgs.CreateWriter("Scanner.cs");
        gen = codeWriter.Output;
        if (dirtyDFA)
            MakeDeterministic();

        cgs.GenerateEpilogue(codeWriter);
        frame.SkipPart("-->begin");

        frame.CopyPart("-->namespace", gen);
        if (!string.IsNullOrEmpty(tab.nsName))
            codeWriter.BeginNamespace(tab.nsName);
        frame.CopyPart("-->declarations", gen);
        gen.WriteLine("\tconst int maxT = {0};", tab.terminals.Count - 1);
        gen.WriteLine("\tconst int noSym = {0};", tab.noSym.n);
        if (ignoreCase)
            gen.Write("\tchar valCh;       // current input character (for token.val)");
        frame.CopyPart("-->initialization", gen);
        WriteStartTab();
        frame.CopyPart("-->casing1", gen);
        if (ignoreCase)
        {
            gen.WriteLine("\t\tif (ch != Buffer.EOF) {");
            gen.WriteLine("\t\t\tvalCh = (char) ch;");
            gen.WriteLine("\t\t\tch = char.ToLower((char) ch);");
            gen.WriteLine("\t\t}");
        }
        frame.CopyPart("-->casing2", gen);
        gen.Write("\t\t\ttval[tlen++] = ");
        if (ignoreCase) gen.Write("valCh;"); else gen.Write("(char) ch;");
        frame.CopyPart("-->comments", gen);
        Comment com = firstComment;
        int comIdx = 0;
        while (com != null)
        {
            GenComment(com, comIdx);
            com = com.Next; comIdx++;
        }
        frame.CopyPart("-->literals", gen); GenLiterals();
        frame.CopyPart("-->scan1", gen);
        gen.Write("\t\t\t");
        if (tab.ignored.Elements() > 0) { PutRange(tab.ignored); } else { gen.Write("false"); }
        frame.CopyPart("-->scan2", gen);
        if (firstComment != null)
        {
            gen.Write("\t\tif (");
            com = firstComment; comIdx = 0;
            while (com != null)
            {
                gen.Write(ChCond(com.Start[0]));
                gen.Write(" && Comment{0}()", comIdx);
                if (com.Next != null) gen.Write(" ||");
                com = com.Next; comIdx++;
            }
            gen.Write(") return NextToken();");
        }
        if (hasCtxMoves) { gen.WriteLine(); gen.Write("\t\tint apx = 0;"); } /* pdt */
        frame.CopyPart("-->scan3", gen);
        for (DfaState state = firstState.next; state != null; state = state.next)
            WriteState(state);
        frame.CopyRest(gen);
        if (!string.IsNullOrEmpty(tab.nsName))
            codeWriter.EndNamespace();
    }

    public DFA(Parser parser)
    {
        this.parser = parser;
        tab = parser.tab;
        errors = parser.errors;
        trace = parser.trace;
        firstState = null; lastState = null; lastStateNr = -1;
        firstState = NewState();
        firstMelted = null; firstComment = null;
        ignoreCase = false;
        dirtyDFA = false;
        hasCtxMoves = false;
    }

}
