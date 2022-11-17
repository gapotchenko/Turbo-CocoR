﻿namespace Gapotchenko.Turbo.CocoR.NET;

#nullable enable

sealed class Generator
{
    public Generator(Tab tab)
    {
        this.tab = tab;
    }

    readonly Tab tab;

    public Frame? CurrentFrame { get; set; }

    public Frame? TryOpenFrame(string fileName)
    {
        string filePath = Path.Combine(tab.srcDir, fileName);
        if (!File.Exists(filePath) && tab.frameDir != null)
            filePath = Path.Combine(tab.frameDir, fileName);
        if (!File.Exists(filePath))
            return null;

        return new Frame(filePath);
    }

    public Frame OpenFrame(string fileName) =>
        TryOpenFrame(fileName) ??
        throw new Exception("Cannot find : " + fileName);

    StreamWriter? gen;

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
        using var frame = TryOpenFrame("Copyright.frame");
        if (frame != null)
            frame.CopyPart(null, gen);
    }

    public void SkipFramePart(string name) => (CurrentFrame ?? throw new InvalidOperationException()).SkipPart(name);

    public void CopyFramePart(string? name) =>
        (CurrentFrame ?? throw new InvalidOperationException())
        .CopyPart(
            name,
            gen ?? throw new InvalidOperationException());

    public void BeginNamespace(ReadOnlySpan<char> name)
    {
        gen.Write("namespace ");
        gen.WriteLine(name);
        gen.WriteLine('{');
    }

    public void EndNamespace()
    {
        gen.WriteLine('}');
    }
}