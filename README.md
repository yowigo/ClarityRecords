# 澄明录 · ClarityRecords

> 记录思考，而不只是存储文章。

---

## 设计理念

大多数博客系统是内容仓库——文章进去，文章出来，仅此而已。

澄明录的出发点不同：**思考是有结构的**。一篇文章不是孤立的，它和其他文章之间存在真实的关联。阅读一篇文章的路径，本身也是值得记录的东西。

这个项目试图回答一个问题：如果把博客当成一个**思维外骨骼**而非发布平台，它应该长什么样？

三个核心判断：

1. **知识是网状的，不是线性的。** 侧边栏不是分类目录，而是知识图谱的入口——文章之间的手工关联链接构成了一张真实的思维地图。

2. **阅读路径本身有信息。** 每次打开哪篇文章、在哪篇文章跳到哪篇文章，这些轨迹比文章本身更能反映思维的流动。`reading_trace` 表记录的不是流量，是思路。

3. **写作工具应该消失在背景里。** 编辑器不重要，发布流程不重要，重要的是文字本身和文字之间的关系。后台管理的存在是为了让这一切不碍事。

---

## 技术栈

| 层 | 技术 |
|---|---|
| 框架 | .NET 10 + Blazor Static SSR |
| 数据库 | PostgreSQL |
| ORM | EF Core |
| Markdown | Markdig |
| 前端编辑器 | EasyMDE |
| 知识图谱渲染 | D3.js / Vis.js |

**渲染策略：** 公开页面用 Static SSR（SEO 友好），`/admin/**` 用 Interactive Server（富交互）。

---

## 项目结构

```
ClarityRecords.Domain/          # 实体、领域接口
ClarityRecords.Infrastructure/  # EF Core、数据库迁移、服务实现
ClarityRecords.Web/             # Blazor 应用、页面、API 端点
ClarityRecords.Tests/           # 集成测试（真实数据库）
```

---

## 本地运行

**前提：** .NET 10 SDK、PostgreSQL

```bash
# 1. 复制配置模板并填入真实连接串
cp ClarityRecords.Web/appsettings.json ClarityRecords.Web/appsettings.Development.json
# 编辑 appsettings.Development.json，填入数据库连接串和初始管理员密码

# 2. 运行数据库迁移
dotnet ef database update \
  --project ClarityRecords.Infrastructure \
  --startup-project ClarityRecords.Web

# 3. 导入种子数据（可选）
psql -U youruser -d clarityrecords -f seed.sql

# 4. 启动
dotnet run --project ClarityRecords.Web
```

访问 `https://localhost:5001`，后台管理在 `/admin`。

---

## 开发路线

- [x] 公开页面骨架（首页、文章详情、知识图谱、标签页）
- [x] 数据库 schema + EF Core 迁移
- [x] 阅读轨迹记录
- [ ] 后台管理系统（完整 RBAC）— 进行中
- [ ] 语义搜索（pgvector）
- [ ] AI 辅助对话（基于文章内容）

---

*名字来源：澄明，清澈透亮。录，记录。记录那些终于想清楚的时刻。*
