using FlowLink.Data.AppDatabase.Models;

namespace FlowLink.Data.AppDatabase.Migrations;

public class SchemaVersion2Migration : IMigration
{
    public int TargetVersion => 2;

    public void Up(SQLite.SQLiteConnection db)
    {
        db.CreateTable<NotificationEntity>();
        db.Execute("ALTER TABLE ApplicationInfoEntity RENAME TO ApplicationEntity;");
    }
}
