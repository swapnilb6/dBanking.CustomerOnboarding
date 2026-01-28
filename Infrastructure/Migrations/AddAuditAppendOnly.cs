using Microsoft.EntityFrameworkCore.Migrations;
namespace dBanking.Infrastructure.Migrations
{
    public partial class AddAuditAppendOnly : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
            CREATE OR REPLACE FUNCTION fn_audit_records_block_write()
            RETURNS trigger AS $$
            BEGIN
                RAISE EXCEPTION 'Audit records are append-only; UPDATE/DELETE is forbidden.';
            END;
            $$ LANGUAGE plpgsql;

            CREATE TRIGGER trg_audit_block_update
            BEFORE UPDATE ON audit_records
            FOR EACH ROW EXECUTE FUNCTION fn_audit_records_block_write();

            CREATE TRIGGER trg_audit_block_delete
            BEFORE DELETE ON audit_records
            FOR EACH ROW EXECUTE FUNCTION fn_audit_records_block_write();
        ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
            DROP TRIGGER IF EXISTS trg_audit_block_update ON audit_records;
            DROP TRIGGER IF EXISTS trg_audit_block_delete ON audit_records;
            DROP FUNCTION IF EXISTS fn_audit_records_block_write();
        ");
        }
    }

}
