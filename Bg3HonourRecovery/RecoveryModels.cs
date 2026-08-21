namespace Bg3HonourRecovery;

public sealed record DisabledSession(string Guid, int Occurrences, string? ImagePath);

public sealed record ProfileScanResult(
    string ProfilePath,
    IReadOnlyList<DisabledSession> Sessions,
    DateTime LastWriteTime,
    long FileSize);

public sealed record RecoveryResult(
    string ProfilePath,
    string? BackupPath,
    int RemovedEntries,
    IReadOnlyList<string> RemovedGuids);
