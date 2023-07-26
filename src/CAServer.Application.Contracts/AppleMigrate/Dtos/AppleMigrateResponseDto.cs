using System.Collections.Generic;

namespace CAServer.AppleMigrate.Dtos;

public class AppleMigrateResponseDto
{
    public string Id { get; set; }
    public string Identifier { get; set; }
    public string IdentifierHash { get; set; }
    public string OriginalIdentifier { get; set; }
    public string Salt { get; set; }
}

public class MigrateRecord
{
    public List<AppleMigrateRecord> AppleMigrateRecords { get; set; } = new();
}

public class AppleMigrateRecord
{
    public string Id { get; set; }
    public string Identifier { get; set; }
    public string OriginalIdentifier { get; set; }
}