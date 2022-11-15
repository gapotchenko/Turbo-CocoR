namespace Gapotchenko.Turbo.CocoR.NET;

sealed class Generator
{
    const int EOF = -1;

    FileStream fram;
    StreamWriter gen;
    readonly Tab tab;
    string frameFile;

    public Generator(Tab tab)
    {
        this.tab = tab;
    }

    public FileStream OpenFrame(string fileName)
    {
        if (tab.frameDir != null)
            frameFile = Path.Combine(tab.frameDir, fileName);
        if (frameFile == null || !File.Exists(frameFile))
            frameFile = Path.Combine(tab.srcDir, fileName);
        if (frameFile == null || !File.Exists(frameFile))
            throw new Exception("Cannot find : " + fileName);

        return fram = new FileStream(frameFile, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public StreamWriter OpenGen(string target)
    {
        string filePath = Path.Combine(tab.outDir, target);

        if (tab.KeepOldFiles)
        {
            if (File.Exists(filePath))
                File.Copy(filePath, filePath + ".old", true);
        }

        return gen = new StreamWriter(new FileStream(filePath, FileMode.Create));
    }

    public void GenCopyright()
    {
        string filePath = null;
        if (tab.frameDir != null)
            filePath = Path.Combine(tab.frameDir, "Copyright.frame");
        if (!File.Exists(filePath))
            filePath = Path.Combine(tab.srcDir, "Copyright.frame");
        if (!File.Exists(filePath))
            return;

        FileStream scannerFram = fram;
        fram = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        CopyFramePart(null);
        fram = scannerFram;
    }

    public void SkipFramePart(string stop)
    {
        CopyFramePart(stop, false);
    }


    public void CopyFramePart(string stop)
    {
        CopyFramePart(stop, true);
    }

    // if stop == null, copies until end of file
    void CopyFramePart(string stop, bool generateOutput)
    {
        char startCh = (char)0;
        int endOfStopString = 0;

        if (stop != null)
        {
            startCh = stop[0];
            endOfStopString = stop.Length - 1;
        }

        int ch = framRead();
        while (ch != EOF)
        {
            if (stop != null && ch == startCh)
            {
                int i = 0;
                do
                {
                    if (i == endOfStopString)
                        return; // stop[0..i] found
                    ch = framRead(); i++;
                } while (ch == stop[i]);
                // stop[0..i-1] found; continue with last read character
                if (generateOutput)
                    gen.Write(stop.Substring(0, i));
            }
            else
            {
                if (generateOutput)
                    gen.Write((char)ch);
                ch = framRead();
            }
        }

        if (stop != null)
            throw new Exception("Incomplete or corrupt frame file: " + frameFile);
    }

    int framRead()
    {
        try
        {
            return fram.ReadByte();
        }
        catch (Exception e)
        {
            throw new Exception("Error reading frame file: " + frameFile, e);
        }
    }
}
