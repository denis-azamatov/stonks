using FluentMigrator;

namespace Migrator.Migrations.M0001
{
    [Migration(1)]
    public class M0001 : Migration
    {
        public override void Up()
        {
            Execute.EmbeddedScript(GetType().Name + ".sql");
        }

        public override void Down()
        {
            throw new NotImplementedException();
        }
    }
}
