---
tags: [user-guide, getting-started, onboarding]
created: 2026-06-18
updated: 2026-06-18
type: user
status: stable
aliases: [Getting Started, Onboarding, First Steps]
---

# Getting Started

> [[_HOME|← Home]]

## What is EduRAG?

EduRAG is an AI-powered study assistant for school students. Administrators upload PDF study materials and students can ask questions and get answers directly from those materials — powered by local AI (no internet required).

---

## Roles

| Role | What they do |
|------|-------------|
| **Admin** (Teacher / Administrator) | Create classes, subjects, chapters; upload PDFs; manage accounts |
| **Student** | Select their class and subject; chat with AI; practice questions |

---

## First Login

1. Open the EduRAG app in your browser: `http://localhost:3000`
2. Enter your email and password
3. You will be taken to your role-specific dashboard

**Default admin credentials (change immediately after first login):**
- Email: `admin@edurag.local`
- Password: `Admin@123`

---

## Admin First-Run Checklist

- [ ] Login as admin
- [ ] Change the default admin password (User Management → Edit Profile)
- [ ] Create at least one Class (e.g., "Class 7")
- [ ] Create a Subject under that class (e.g., "Science")
- [ ] Optionally create Chapters (e.g., "Chapter 1 — Photosynthesis")
- [ ] Upload a PDF study material for the subject
- [ ] Wait for vectorization status to show **Completed** (usually 1–5 minutes)
- [ ] Create a Student account
- [ ] Share student login credentials

---

## Student First-Run Checklist

- [ ] Login with credentials provided by your teacher
- [ ] On the Class Selection screen, click your class
- [ ] Click your subject
- [ ] Type a question in the chat box and press Send
- [ ] Try "Give me 5 practice questions on this topic"

---

## Key Pages

| Page | Who | Purpose |
|------|-----|---------|
| `/admin/dashboard` | Admin | Overview stats |
| `/admin/classes` | Admin | Manage classes |
| `/admin/materials` | Admin | View upload status |
| `/student/select` | Student | Pick class and subject |
| `/student/chat/:classId/:subjectId` | Student | AI chat |

---

## Related Docs

- [[01-Admin-Guide]] — detailed admin walkthrough
- [[02-Student-Guide]] — detailed student walkthrough
- [[03-FAQ]] — common questions
- [[../System/05-Deployment]] — how to install EduRAG
