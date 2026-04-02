## 项目简介

**澄明录**是一个个人知识博客系统，用于沉淀阅读笔记与思考。支持 Markdown 写作、知识图谱可视化、阅读轨迹追踪，以及完整的后台内容管理（文章、标签、用户、RBAC 权限）。

## 技术栈

- **后端框架**：.NET 10，ASP.NET Core，Blazor（混合渲染：Static SSR + Interactive Server）
- **数据库**：PostgreSQL，EF Core，迁移 SQL 中含自定义约束（CHECK、无向唯一索引）
- **认证与授权**：ASP.NET Core Identity，基于角色 + Claim 的 RBAC 权限体系
- **前端**：原生 CSS 设计系统（`app.css`），EasyMDE Markdown 编辑器，Vanilla JS
- **测试**：xUnit，集成测试直接连 PostgreSQL（无 mock）

## 编码规范

- **用户可见文字必须用中文**：包括错误提示、警告、按钮文字、占位符、空状态文案等
- **代码注释必须用中文**：包括 C#、Razor、JS、CSS 中的所有注释
- **Blazor 组件内 `<script>` 标签不会执行**：JS 必须放到 `wwwroot/js/` 静态文件中
- **修改前先读文件**：不要对未读过的代码提出修改建议

## Skill 路由

当用户请求匹配某个可用 Skill 时，必须将调用该 Skill 作为第一个动作。不要直接回答，不要先用其他工具。Skill 有专门的工作流，能产出比临时回答更好的结果。

路由规则：

- 产品想法、"值不值得做"、头脑风暴 → 调用 office-hours
- Bug、报错、"为什么挂了"、500 错误 → 调用 investigate
- 发布、部署、推送、创建 PR → 调用 ship
- QA、测试站点、找 Bug → 调用 qa
- 代码审查、检查 diff → 调用 review
- 发布后更新文档 → 调用 document-release
- 每周复盘 → 调用 retro
- 设计系统、品牌 → 调用 design-consultation
- 视觉审查、设计打磨 → 调用 design-review
- 架构评审 → 调用 plan-eng-review
