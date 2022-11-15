
#nullable enable

#pragma warning disable IDE0161 // Convert to file-scoped namespace

namespace Gapotchenko.Turbo.CocoR.NET.Grammar {

sealed class Token
{
	public int kind;    // token kind
	public int pos;     // token position in bytes in the source text (starting at 0)
	public int charPos;  // token position in characters in the source text (starting at 0)
	public int col;     // token column (starting at 1)
	public int line;    // token line (starting at 1)
	public string val = string.Empty;  // token value
	public Token? next;  // ML 2005-03-11 Tokens are kept in linked list
}

//-----------------------------------------------------------------------------------
// Buffer
//-----------------------------------------------------------------------------------
class Buffer
{
	// This Buffer supports the following cases:
	// 1) seekable stream (file)
	//    a) whole stream in buffer
	//    b) part of stream in buffer
	// 2) non seekable stream (network, console)

	public const int EOF = char.MaxValue + 1;
	const int MIN_BUFFER_LENGTH = 1024; // 1KB
	const int MAX_BUFFER_LENGTH = MIN_BUFFER_LENGTH * 64; // 64KB
	byte[] buf;         // input buffer
	int bufStart;       // position of first byte in buffer relative to input stream
	int bufLen;         // length of buffer
	int fileLen;        // length of input stream (may change if the stream is no file)
	int bufPos;         // current position in buffer
	Stream? stream;      // input stream (seekable)
	bool isUserStream;  // was the stream opened by the user?
	
	public Buffer (Stream s, bool isUserStream)
	{
		stream = s; this.isUserStream = isUserStream;

		bool canSeek = stream.CanSeek;
		if (canSeek)
		{
			fileLen = (int)stream.Length;
			bufLen = Math.Min(fileLen, MAX_BUFFER_LENGTH);
			bufStart = Int32.MaxValue; // nothing in the buffer so far
		}
		else
		{
			fileLen = bufLen = bufStart = 0;
		}

		buf = new byte[bufLen > 0 ? bufLen : MIN_BUFFER_LENGTH];
		if (fileLen > 0)
			Pos = 0; // setup buffer to position 0 (start)
		else
			bufPos = 0; // index 0 is already after the file, thus Pos = 0 is invalid

		if (bufLen == fileLen && canSeek)
			Close();
	}
	
	protected Buffer(Buffer b)
	{
		// called in UTF8Buffer constructor
		buf = b.buf;
		bufStart = b.bufStart;
		bufLen = b.bufLen;
		fileLen = b.fileLen;
		bufPos = b.bufPos;
		stream = b.stream;
		// keep destructor from closing the stream
		b.stream = null;
		isUserStream = b.isUserStream;
	}

	~Buffer() => Close();
	
	protected void Close()
	{
		if (!isUserStream && stream != null)
		{
			stream.Close();
			stream = null;
		}
	}
	
	public virtual int Read ()
	{
		if (bufPos < bufLen)
		{
			return buf[bufPos++];
		}
		else if (Pos < fileLen)
		{
			Pos = Pos; // shift buffer start to Pos
			return buf[bufPos++];
		}
		else if (stream != null && !stream.CanSeek && ReadNextStreamChunk() > 0)
		{
			return buf[bufPos++];
		}
		else
		{
			return EOF;
		}
	}

	public int Peek ()
	{
		int curPos = Pos;
		int ch = Read();
		Pos = curPos;
		return ch;
	}
	
	// beg .. begin, zero-based, inclusive, in byte
	// end .. end, zero-based, exclusive, in byte
	public string GetString (int beg, int end)
	{
		int len = 0;
		var buf = new char[end - beg];
		int oldPos = Pos;
		Pos = beg;
		while (Pos < end)
			buf[len++] = (char)Read();
		Pos = oldPos;
		return new String(buf, 0, len);
	}

	public int Pos
	{
		get => bufPos + bufStart;
		set
		{
			if (value >= fileLen && stream != null && !stream.CanSeek)
			{
				// Wanted position is after buffer and the stream
				// is not seek-able e.g. network or console,
				// thus we have to read the stream manually till
				// the wanted position is in sight.
				while (value >= fileLen && ReadNextStreamChunk() > 0);
			}

			if (value < 0 || value > fileLen)
				throw new Exception("buffer out of bounds access, position: " + value);

			if (value >= bufStart && value < bufStart + bufLen)
			{
				// already in buffer
				bufPos = value - bufStart;
			}
			else if (stream != null)
			{
				// must be swapped in
				stream.Seek(value, SeekOrigin.Begin);
				bufLen = stream.Read(buf, 0, buf.Length);
				bufStart = value;
				bufPos = 0;
			}
			else
			{
				// set the position to the end of the file, Pos will return fileLen.
				bufPos = fileLen - bufStart;
			}
		}
	}
	
	// Read the next chunk of bytes from the stream, increases the buffer
	// if needed and updates the fields fileLen and bufLen.
	// Returns the number of bytes read.
	private int ReadNextStreamChunk()
	{
		int free = buf.Length - bufLen;
		if (free == 0)
		{
			// in the case of a growing input stream
			// we can neither seek in the stream, nor can we
			// foresee the maximum length, thus we must adapt
			// the buffer size on demand.
			var newBuf = new byte[bufLen * 2];
			Array.Copy(buf, newBuf, bufLen);
			buf = newBuf;
			free = bufLen;
		}
		if (stream != null)
		{
			int read = stream.Read(buf, bufLen, free);
			if (read > 0)
			{
				fileLen = bufLen = (bufLen + read);
				return read;
			}
		}
		// end of stream reached
		return 0;
	}
}

//-----------------------------------------------------------------------------------
// UTF8Buffer
//-----------------------------------------------------------------------------------
sealed class UTF8Buffer : Buffer
{
	public UTF8Buffer(Buffer b) :
		base(b)
	{
	}

	public override int Read()
	{
		int ch;
		do
		{
			ch = base.Read();
			// until we find a utf8 start (0xxxxxxx or 11xxxxxx)
		}
		while ((ch >= 128) && ((ch & 0xC0) != 0xC0) && (ch != EOF));
		
		if (ch < 128 || ch == EOF)
		{
			// nothing to do, first 127 chars are the same in ascii and utf8
			// 0xxxxxxx or end of file character
		}
		else if ((ch & 0xF0) == 0xF0)
		{
			// 11110xxx 10xxxxxx 10xxxxxx 10xxxxxx
			int c1 = ch & 0x07; ch = base.Read();
			int c2 = ch & 0x3F; ch = base.Read();
			int c3 = ch & 0x3F; ch = base.Read();
			int c4 = ch & 0x3F;
			ch = (((((c1 << 6) | c2) << 6) | c3) << 6) | c4;
		}
		else if ((ch & 0xE0) == 0xE0)
		{
			// 1110xxxx 10xxxxxx 10xxxxxx
			int c1 = ch & 0x0F; ch = base.Read();
			int c2 = ch & 0x3F; ch = base.Read();
			int c3 = ch & 0x3F;
			ch = (((c1 << 6) | c2) << 6) | c3;
		}
		else if ((ch & 0xC0) == 0xC0)
		{
			// 110xxxxx 10xxxxxx
			int c1 = ch & 0x1F; ch = base.Read();
			int c2 = ch & 0x3F;
			ch = (c1 << 6) | c2;
		}

		return ch;
	}
}

//-----------------------------------------------------------------------------------
// Scanner
//-----------------------------------------------------------------------------------
sealed class Scanner
{
	const char EOL = '\n';
	const int eofSym = 0; /* pdt */
	const int maxT = 41;
	const int noSym = 41;


	public Buffer buffer; // scanner buffer
	
	Token? t;          // current token
	int ch;           // current input character
	int pos;          // byte position of current character
	int charPos;      // position by unicode characters starting with 0
	int col;          // column number of current character
	int line;         // line number of current character
	int oldEols;      // EOLs that appeared in a comment;
	static readonly Dictionary<int, int> start = new(128); // maps first token character to start state

	Token tokens;     // list of tokens already peeked (first token is a dummy)
	Token pt;         // current peek token
	
	char[] tval = new char[128]; // text of current token
	int tlen;         // length of current token
	
	static Scanner()
	{
		for (int i = 65; i <= 90; ++i) start[i] = 1;
		for (int i = 95; i <= 95; ++i) start[i] = 1;
		for (int i = 97; i <= 122; ++i) start[i] = 1;
		for (int i = 48; i <= 57; ++i) start[i] = 2;
		start[34] = 12; 
		start[39] = 5; 
		start[36] = 13; 
		start[61] = 16; 
		start[46] = 31; 
		start[43] = 17; 
		start[45] = 18; 
		start[60] = 32; 
		start[62] = 20; 
		start[124] = 23; 
		start[40] = 33; 
		start[41] = 24; 
		start[91] = 25; 
		start[93] = 26; 
		start[123] = 27; 
		start[125] = 28; 
		start[Buffer.EOF] = -1;

	}
	
	public Scanner (string fileName)
	{
		var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
		buffer = new Buffer(stream, false);
		Init();
	}
	
	public Scanner (Stream s)
	{
		buffer = new Buffer(s, true);
		Init();
	}
	
	[MemberNotNull(nameof(pt), nameof(tokens))]
	void Init()
	{
		pos = -1; line = 1; col = 0; charPos = -1;
		oldEols = 0;
		NextCh();
		if (ch == 0xEF)
		{
			// check optional byte order mark for UTF-8
			NextCh(); int ch1 = ch;
			NextCh(); int ch2 = ch;
			if (ch1 != 0xBB || ch2 != 0xBF) {
				throw new Exception(String.Format("illegal byte order mark: EF {0,2:X} {1,2:X}", ch1, ch2));
			}
			buffer = new UTF8Buffer(buffer); col = 0; charPos = -1;
			NextCh();
		}
		pt = tokens = new Token();  // first token is a dummy
	}
	
	void NextCh()
	{
		if (oldEols > 0)
		{
			ch = EOL;
			oldEols--;
		} 
		else
		{
			pos = buffer.Pos;
			// buffer reads unicode chars, if UTF8 has been detected
			ch = buffer.Read(); col++; charPos++;
			// replace isolated '\r' by '\n' in order to make
			// eol handling uniform across Windows, Unix and Mac
			if (ch == '\r' && buffer.Peek() != '\n')
				ch = EOL;
			if (ch == EOL)
			{
				line++;
				col = 0;
			}
		}

	}

	void AddCh()
	{
		if (tlen >= tval.Length)
		{
			var newBuf = new char[2 * tval.Length];
			Array.Copy(tval, 0, newBuf, 0, tval.Length);
			tval = newBuf;
		}
		if (ch != Buffer.EOF)
		{
			tval[tlen++] = (char) ch;
			NextCh();
		}
	}



	bool Comment0() {
		int level = 1, pos0 = pos, line0 = line, col0 = col, charPos0 = charPos;
		NextCh();
		if (ch == '/') {
			NextCh();
			for(;;) {
				if (ch == 10) {
					level--;
					if (level == 0) { oldEols = line - line0; NextCh(); return true; }
					NextCh();
				} else if (ch == Buffer.EOF) return false;
				else NextCh();
			}
		} else {
			buffer.Pos = pos0; NextCh(); line = line0; col = col0; charPos = charPos0;
		}
		return false;
	}

	bool Comment1() {
		int level = 1, pos0 = pos, line0 = line, col0 = col, charPos0 = charPos;
		NextCh();
		if (ch == '*') {
			NextCh();
			for(;;) {
				if (ch == '*') {
					NextCh();
					if (ch == '/') {
						level--;
						if (level == 0) { oldEols = line - line0; NextCh(); return true; }
						NextCh();
					}
				} else if (ch == '/') {
					NextCh();
					if (ch == '*') {
						level++; NextCh();
					}
				} else if (ch == Buffer.EOF) return false;
				else NextCh();
			}
		} else {
			buffer.Pos = pos0; NextCh(); line = line0; col = col0; charPos = charPos0;
		}
		return false;
	}


	void CheckLiteral(Token t)
	{
		switch (t.val) {
			case "COMPILER": t.kind = 6; break;
			case "IGNORECASE": t.kind = 7; break;
			case "CHARACTERS": t.kind = 8; break;
			case "TOKENS": t.kind = 9; break;
			case "PRAGMAS": t.kind = 10; break;
			case "COMMENTS": t.kind = 11; break;
			case "FROM": t.kind = 12; break;
			case "TO": t.kind = 13; break;
			case "NESTED": t.kind = 14; break;
			case "IGNORE": t.kind = 15; break;
			case "PRODUCTIONS": t.kind = 16; break;
			case "END": t.kind = 19; break;
			case "ANY": t.kind = 23; break;
			case "WEAK": t.kind = 29; break;
			case "SYNC": t.kind = 36; break;
			case "IF": t.kind = 37; break;
			case "CONTEXT": t.kind = 38; break;
			default: break;
		}
	}

	Token NextToken()
	{
		while (ch == ' ' ||
			ch >= 9 && ch <= 10 || ch == 13
		) NextCh();
		if (ch == '/' && Comment0() ||ch == '/' && Comment1()) return NextToken();
		int recKind = noSym;
		int recEnd = pos;
		t = new Token();
		t.pos = pos; t.col = col; t.line = line; t.charPos = charPos;
		int state;
		state = start.ContainsKey(ch) ? start[ch] : 0;
		tlen = 0; AddCh();
		
		switch (state)
		{
			case -1:
				t.kind = eofSym;
				// NextCh already done
				break; 

			case 0:
				if (recKind != noSym)
				{
					tlen = recEnd - t.pos;
					SetScannerBehindT(t);
				}
				t.kind = recKind;
				// NextCh already done
				break;
			case 1:
				recEnd = pos; recKind = 1;
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'Z' || ch == '_' || ch >= 'a' && ch <= 'z') {AddCh(); goto case 1;}
				else {t.kind = 1; t.val = new String(tval, 0, tlen); CheckLiteral(t); return t;}
			case 2:
				recEnd = pos; recKind = 2;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 2;}
				else {t.kind = 2; break;}
			case 3:
				{t.kind = 3; break;}
			case 4:
				{t.kind = 4; break;}
			case 5:
				if (ch <= 9 || ch >= 11 && ch <= 12 || ch >= 14 && ch <= '&' || ch >= '(' && ch <= '[' || ch >= ']' && ch <= 65535) {AddCh(); goto case 6;}
				else if (ch == 92) {AddCh(); goto case 7;}
				else {goto case 0;}
			case 6:
				if (ch == 39) {AddCh(); goto case 9;}
				else {goto case 0;}
			case 7:
				if (ch >= ' ' && ch <= '~') {AddCh(); goto case 8;}
				else {goto case 0;}
			case 8:
				if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 8;}
				else if (ch == 39) {AddCh(); goto case 9;}
				else {goto case 0;}
			case 9:
				{t.kind = 5; break;}
			case 10:
				recEnd = pos; recKind = 42;
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'Z' || ch == '_' || ch >= 'a' && ch <= 'z') {AddCh(); goto case 10;}
				else {t.kind = 42; break;}
			case 11:
				recEnd = pos; recKind = 43;
				if (ch >= '-' && ch <= '.' || ch >= '0' && ch <= ':' || ch >= 'A' && ch <= 'Z' || ch == '_' || ch >= 'a' && ch <= 'z') {AddCh(); goto case 11;}
				else {t.kind = 43; break;}
			case 12:
				if (ch <= 9 || ch >= 11 && ch <= 12 || ch >= 14 && ch <= '!' || ch >= '#' && ch <= '[' || ch >= ']' && ch <= 65535) {AddCh(); goto case 12;}
				else if (ch == 10 || ch == 13) {AddCh(); goto case 4;}
				else if (ch == '"') {AddCh(); goto case 3;}
				else if (ch == 92) {AddCh(); goto case 14;}
				else {goto case 0;}
			case 13:
				recEnd = pos; recKind = 42;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 10;}
				else if (ch >= 'A' && ch <= 'Z' || ch == '_' || ch >= 'a' && ch <= 'z') {AddCh(); goto case 15;}
				else {t.kind = 42; break;}
			case 14:
				if (ch >= ' ' && ch <= '~') {AddCh(); goto case 12;}
				else {goto case 0;}
			case 15:
				recEnd = pos; recKind = 42;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 10;}
				else if (ch >= 'A' && ch <= 'Z' || ch == '_' || ch >= 'a' && ch <= 'z') {AddCh(); goto case 15;}
				else if (ch == '=') {AddCh(); goto case 11;}
				else {t.kind = 42; break;}
			case 16:
				{t.kind = 17; break;}
			case 17:
				{t.kind = 20; break;}
			case 18:
				{t.kind = 21; break;}
			case 19:
				{t.kind = 22; break;}
			case 20:
				{t.kind = 25; break;}
			case 21:
				{t.kind = 26; break;}
			case 22:
				{t.kind = 27; break;}
			case 23:
				{t.kind = 28; break;}
			case 24:
				{t.kind = 31; break;}
			case 25:
				{t.kind = 32; break;}
			case 26:
				{t.kind = 33; break;}
			case 27:
				{t.kind = 34; break;}
			case 28:
				{t.kind = 35; break;}
			case 29:
				{t.kind = 39; break;}
			case 30:
				{t.kind = 40; break;}
			case 31:
				recEnd = pos; recKind = 18;
				if (ch == '.') {AddCh(); goto case 19;}
				else if (ch == '>') {AddCh(); goto case 22;}
				else if (ch == ')') {AddCh(); goto case 30;}
				else {t.kind = 18; break;}
			case 32:
				recEnd = pos; recKind = 24;
				if (ch == '.') {AddCh(); goto case 21;}
				else {t.kind = 24; break;}
			case 33:
				recEnd = pos; recKind = 30;
				if (ch == '.') {AddCh(); goto case 29;}
				else {t.kind = 30; break;}

		}
		t.val = new String(tval, 0, tlen);
		return t;
	}
	
	void SetScannerBehindT(Token t)
	{
		buffer.Pos = t.pos;
		NextCh();
		line = t.line; col = t.col; charPos = t.charPos;
		for (int i = 0; i < tlen; i++)
			NextCh();
	}
	
	// get the next token (possibly a token already seen during peeking)
	public Token Scan()
	{
		if (tokens.next == null)
			return NextToken();
		else
			return pt = tokens = tokens.next;
	}

	// peek for the next token, ignore pragmas
	public Token Peek()
	{
		do
		{
			if (pt.next == null)
				pt.next = NextToken();
			pt = pt.next;
		}
		while (pt.kind > maxT); // skip pragmas
	
		return pt;
	}

	// make sure that peeking starts at the current scan position
	public void ResetPeek() => pt = tokens;

} // end Scanner

}