namespace ClarityRecords.Domain.Authorization;

public static class Permissions
{
    public const string ArticlesCreate       = "articles.create";
    public const string ArticlesEditOwn      = "articles.edit_own";
    public const string ArticlesEditAll      = "articles.edit_all";
    public const string ArticlesPublish      = "articles.publish";
    public const string ArticlesDelete       = "articles.delete";

    public const string TagsManage           = "tags.manage";
    public const string KnowledgeLinksManage = "knowledge_links.manage";

    public const string UsersView            = "users.view";
    public const string UsersManage          = "users.manage";

    public const string RolesManage          = "roles.manage";

    public static readonly string[] All =
    [
        ArticlesCreate, ArticlesEditOwn, ArticlesEditAll, ArticlesPublish, ArticlesDelete,
        TagsManage, KnowledgeLinksManage,
        UsersView, UsersManage,
        RolesManage
    ];
}
