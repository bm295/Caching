namespace Caching.Framework.Models;

public sealed record ReplicaInspectionResult(
    string NodeId,
    string BaseAddress,
    bool IsLocalNode,
    bool HasValue,
    string? Value);
