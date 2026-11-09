using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigiRAG.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Multi-tenant row-level security, moved out of the hand-written database/01_init_schema.sql
    /// and into the EF Core migration pipeline so there is a single source of truth for the schema.
    ///
    /// The old script targeted snake_case columns (tenant_id); EF creates quoted PascalCase
    /// columns ("TenantId"), so the original policies could never have matched this schema.
    ///
    /// NOTE: Postgres exempts superusers and (without FORCE) the table owner from RLS. The app
    /// currently connects as "postgres", so these policies are dormant until it connects as a
    /// dedicated non-superuser role. FORCE is deliberately not enabled: it would reject every
    /// write from the Hangfire ingestion worker, which has no HTTP request and therefore never
    /// sets app.tenant_id.
    /// </summary>
    public partial class AddRowLevelSecurity : Migration
    {
        private static readonly string[] TenantScopedTables =
        {
            "knowledge_bases",
            "documents",
            "document_chunks",
            "ingestion_jobs",
            "chat_sessions"
        };

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in TenantScopedTables)
            {
                migrationBuilder.Sql($"""
                    ALTER TABLE {table} ENABLE ROW LEVEL SECURITY;

                    DROP POLICY IF EXISTS tenant_isolation_{table} ON {table};

                    CREATE POLICY tenant_isolation_{table} ON {table}
                        FOR ALL
                        USING (
                            NULLIF(current_setting('app.tenant_id', true), '') IS NULL
                            OR "TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                        )
                        WITH CHECK (
                            NULLIF(current_setting('app.tenant_id', true), '') IS NULL
                            OR "TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                        );
                    """);
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in TenantScopedTables)
            {
                migrationBuilder.Sql($"""
                    DROP POLICY IF EXISTS tenant_isolation_{table} ON {table};
                    ALTER TABLE {table} DISABLE ROW LEVEL SECURITY;
                    """);
            }
        }
    }
}
