using FluentMigrator;
using Nop.Data.Migrations;
using Nop.Plugin.Shipping.DPD.Domain;

namespace Nop.Plugin.Shipping.DPD.Data
{
    [SkipMigrationOnUpdate]
    [NopMigration("2021/01/28 08:40:55:1687541", "Shipping.DPD base schema")]
    public class SchemaMigration : AutoReversingMigration
    {
        protected IMigrationManager _migrationManager;

        public SchemaMigration(IMigrationManager migrationManager)
        {
            _migrationManager = migrationManager;
        }

        public override void Up()
        {
            _migrationManager.BuildTable<PickupPointAddress>(Create);
        }
    }
}