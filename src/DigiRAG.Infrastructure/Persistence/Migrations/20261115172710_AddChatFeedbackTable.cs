using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigiRAG.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Creates the chat_feedbacks table.
    ///
    /// The entity and its configuration existed, and the model snapshot already claimed
    /// the table, but InitialCreate never created it - so the model and the database had
    /// drifted and POST /api/v1/chat/feedback failed with
    /// 42P01: relation "chat_feedbacks" does not exist.
    ///
    /// Because the snapshot already contains the table, EF scaffolds this migration empty;
    /// the statements below are written by hand. They use IF NOT EXISTS so the migration is
    /// safe on any environment where the table was created out of band.
    /// </summary>
    public partial class AddChatFeedbackTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS chat_feedbacks (
                    "Id" uuid NOT NULL,
                    "TenantId" uuid NOT NULL,
                    "ChatSessionId" uuid NOT NULL,
                    "ChatMessageId" uuid NULL,
                    "IsPositive" boolean NOT NULL,
                    "Comment" text NULL,
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_chat_feedbacks" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_chat_feedbacks_chat_sessions_ChatSessionId"
                        FOREIGN KEY ("ChatSessionId") REFERENCES chat_sessions ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_chat_feedbacks_chat_messages_ChatMessageId"
                        FOREIGN KEY ("ChatMessageId") REFERENCES chat_messages ("Id") ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS "IX_chat_feedbacks_TenantId_ChatSessionId"
                    ON chat_feedbacks ("TenantId", "ChatSessionId");
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS chat_feedbacks;");
        }
    }
}
