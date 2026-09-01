using SQLite;

namespace FlowLink.Data.AppDatabase.Models;

public class SchemaVersionEntity
{
    [PrimaryKey]
    public int Version { get; set; }
}
