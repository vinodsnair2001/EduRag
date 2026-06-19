---
tags: [skill, change-tracker, documentation, protocol]
created: 2026-06-18
updated: 2026-06-18
type: skill
status: stable
aliases: [Change Tracker, Doc Sync Protocol]
---

# Change Tracker

> [[_HOME|← Home]] · [[edurag-claude-skill|← Claude Skill]]

## Purpose

Every change to the EduRAG codebase must be reflected immediately in the EduRagBrain documentation vault. This document defines the lookup table: which code change → which docs to update.

---

## Change → Doc Mapping

### Domain Entity Changed (add/rename/remove field)

| Trigger | Update these docs |
|---------|-----------------|
| New entity field | [[../Architecture/02-Domain-Layer]] entity section |
| New entity field that maps to DB | [[../System/01-Database-Schema]] table DDL |
| New entity | [[../Architecture/02-Domain-Layer]], [[../System/01-Database-Schema]], [[../Architecture/04-Infrastructure-Layer]] (EF config) |
| Entity renamed | All wikilinks, [[../Architecture/02-Domain-Layer]], [[../System/01-Database-Schema]] |

### New API Endpoint Added

| Trigger | Update these docs |
|---------|-----------------|
| New controller endpoint | [[../System/02-API-Reference]] — add row to endpoint table |
| New endpoint with new auth rule | [[../System/03-Security]] |
| New endpoint on AdminController | [[../User/01-Admin-Guide]] — add to relevant section |
| New endpoint on ChatController | [[../User/02-Student-Guide]] — if user-visible |

### Infrastructure Change

| Trigger | Update these docs |
|---------|-----------------|
| New repository method | [[../Architecture/04-Infrastructure-Layer]] |
| New Dapper query | [[../Architecture/04-Infrastructure-Layer]] |
| New NuGet package | [[../System/04-Configuration]] NuGet table |
| New background service | [[../Architecture/04-Infrastructure-Layer]], [[../System/00-System-Overview]] |

### Configuration Change

| Trigger | Update these docs |
|---------|-----------------|
| New appsettings key | [[../System/04-Configuration]] |
| New docker-compose service | [[../System/04-Configuration]], [[../System/05-Deployment]] |
| New environment variable | [[../System/04-Configuration]] |

### Frontend Change

| Trigger | Update these docs |
|---------|-----------------|
| New component | [[../Architecture/06-Frontend]] component map table |
| New route | [[../Architecture/06-Frontend]] route structure |
| New npm package | [[../Architecture/06-Frontend]] tech stack table |
| New shadcn component | [[../Architecture/06-Frontend]] shadcn components table |
| UI design token change | [[../Architecture/06-Frontend]] design tokens section |

### AI / RAG Change

| Trigger | Update these docs |
|---------|-----------------|
| New prompt template | [[../Architecture/07-AI-Pipeline]] RAG prompts section |
| Model swap | [[../Architecture/07-AI-Pipeline]], [[../System/00-System-Overview]], [[../System/04-Configuration]], [[../System/05-Deployment]] |
| Chunking parameter change | [[../Architecture/07-AI-Pipeline]] chunking table, [[../System/04-Configuration]] |
| topK change | [[../System/04-Configuration]], [[../Architecture/07-AI-Pipeline]] |

---

## Changelog Entry Format

Every change must be logged in [[../Changelog/CHANGELOG]] with this format:

```markdown
## YYYY-MM-DD — <short description>

**Type:** Feature | Fix | Change | Refactor | Docs | Security
**Affected:** Backend | Frontend | Database | AI | Config | Docs

### What changed
- One bullet per discrete change

### Docs updated
- [[path/to/doc]] — what was updated
```

---

## Step-by-Step: Making a Change

1. Make the code change
2. Open this document
3. Find the row matching what you changed
4. Open each listed doc
5. Update the affected sections
6. Open [[../Changelog/CHANGELOG]] and add an entry
7. Update the `updated:` frontmatter date in every touched doc

---

## Related Docs

- [[edurag-claude-skill]] — full Claude behaviour rules
- [[../Changelog/CHANGELOG]] — all historical changes
