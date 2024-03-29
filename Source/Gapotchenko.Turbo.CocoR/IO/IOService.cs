﻿using Gapotchenko.Turbo.CocoR.Options;
using System.Composition;

#nullable enable

namespace Gapotchenko.Turbo.CocoR.IO;

[Shared]
[Export(typeof(IIOService))]
sealed class IOService : IIOService
{
    [ImportingConstructor]
    public IOService(IOptionsService optionsService)
    {
        m_OptionsService = optionsService;
    }

    readonly IOptionsService m_OptionsService;

    public void CreateFileBackup(string filePath)
    {
        if (m_OptionsService.KeepOldFiles)
        {
            if (File.Exists(filePath))
                BackupFile(filePath);
        }
    }

    static void BackupFile(string filePath) => File.Copy(filePath, filePath + ".old", true);
}
