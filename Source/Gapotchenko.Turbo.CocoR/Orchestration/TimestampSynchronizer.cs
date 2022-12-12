namespace Gapotchenko.Turbo.CocoR.Orchestration;

sealed class TimestampSynchronizer
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    DateTime m_Timestamp;

    public void AddFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            var ts = File.GetLastWriteTimeUtc(filePath);
            if (ts > m_Timestamp)
                m_Timestamp = ts;
        }
    }

    public DateTime? Timestamp => m_Timestamp == default ? null : m_Timestamp;
}
