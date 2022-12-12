﻿#nullable enable

namespace Gapotchenko.Turbo.CocoR.IO;

interface IIOService
{
    void CreateFileBackup(string filePath);

    IReadOnlyList<string> ModifiedFiles { get; }
}
