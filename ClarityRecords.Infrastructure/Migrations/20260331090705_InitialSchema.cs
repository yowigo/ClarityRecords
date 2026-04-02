using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClarityRecords.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "articles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Embedding = table.Column<float[]>(type: "real[]", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_articles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "article_manual_links",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    FromArticleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToArticleId = table.Column<Guid>(type: "uuid", nullable: false),
                    RelationNote = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_article_manual_links", x => x.Id);
                    table.ForeignKey(
                        name: "FK_article_manual_links_articles_FromArticleId",
                        column: x => x.FromArticleId,
                        principalTable: "articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_article_manual_links_articles_ToArticleId",
                        column: x => x.ToArticleId,
                        principalTable: "articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "article_tags",
                columns: table => new
                {
                    ArticleId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_article_tags", x => new { x.ArticleId, x.TagId });
                    table.ForeignKey(
                        name: "FK_article_tags_articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_article_tags_tags_TagId",
                        column: x => x.TagId,
                        principalTable: "tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reading_trace",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ArticleId = table.Column<Guid>(type: "uuid", nullable: true),
                    RelatedArticleId = table.Column<Guid>(type: "uuid", nullable: true),
                    ManualLinkId = table.Column<Guid>(type: "uuid", nullable: true),
                    Note = table.Column<string>(type: "text", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reading_trace", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reading_trace_article_manual_links_ManualLinkId",
                        column: x => x.ManualLinkId,
                        principalTable: "article_manual_links",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_reading_trace_articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_reading_trace_articles_RelatedArticleId",
                        column: x => x.RelatedArticleId,
                        principalTable: "articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            // CHECK: EventType 只允许三个固定值
            migrationBuilder.Sql("""
                ALTER TABLE reading_trace
                  ADD CONSTRAINT CK_reading_trace_event_type
                  CHECK ("EventType" IN ('new_article', 'manual_link', 'insight'));
                """);

            // CHECK: 跨字段验证——manual_link 必须有 ArticleId 和 RelatedArticleId
            migrationBuilder.Sql("""
                ALTER TABLE reading_trace
                  ADD CONSTRAINT CK_reading_trace_event_fields CHECK (
                    ("EventType" = 'manual_link' AND "ArticleId" IS NOT NULL AND "RelatedArticleId" IS NOT NULL)
                    OR ("EventType" = 'new_article' AND "ArticleId" IS NOT NULL)
                    OR ("EventType" = 'insight')
                  );
                """);

            // CHECK: 禁止自链接
            migrationBuilder.Sql("""
                ALTER TABLE article_manual_links
                  ADD CONSTRAINT CK_article_manual_links_no_self_link
                  CHECK ("FromArticleId" != "ToArticleId");
                """);

            // UNIQUE: 无向图去重（(A→B) 和 (B→A) 视为同一条边）
            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX UX_article_manual_links_undirected
                  ON article_manual_links (
                    LEAST("FromArticleId"::text, "ToArticleId"::text),
                    GREATEST("FromArticleId"::text, "ToArticleId"::text)
                  );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_article_manual_links_FromArticleId",
                table: "article_manual_links",
                column: "FromArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_article_manual_links_ToArticleId",
                table: "article_manual_links",
                column: "ToArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_article_tags_TagId",
                table: "article_tags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_articles_Slug",
                table: "articles",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reading_trace_ArticleId",
                table: "reading_trace",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_reading_trace_ManualLinkId",
                table: "reading_trace",
                column: "ManualLinkId");

            migrationBuilder.CreateIndex(
                name: "IX_reading_trace_RelatedArticleId",
                table: "reading_trace",
                column: "RelatedArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_tags_Name",
                table: "tags",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tags_Slug",
                table: "tags",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS UX_article_manual_links_undirected;");

            migrationBuilder.DropTable(
                name: "article_tags");

            migrationBuilder.DropTable(
                name: "reading_trace");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropTable(
                name: "article_manual_links");

            migrationBuilder.DropTable(
                name: "articles");
        }
    }
}
