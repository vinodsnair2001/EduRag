---
tags: [user-guide, admin, classes, upload, management]
created: 2026-06-18
updated: 2026-06-18
type: user
status: stable
aliases: [Admin Guide, Admin Manual]
---

# Admin Guide

> [[_HOME|← Home]] · [[00-Getting-Started|← Getting Started]]

## Dashboard

After login, the admin dashboard shows:
- Total classes, subjects, chapters
- Total study materials and their vectorization status breakdown
- Recent uploads with status badges

---

## Managing Classes

### Create a Class

1. Go to **Classes** in the sidebar
2. Click **+ New Class**
3. Enter the class name (e.g., "Class 7") and grade number (1–12)
4. Click **Save**

### Edit a Class

1. Find the class in the list
2. Click the **Edit** (pencil) icon
3. Update name or grade
4. Click **Save**

### Delete a Class

> ⚠️ **Cascades:** Deleting a class deletes ALL its subjects, chapters, study materials, and uploaded files permanently.

1. Click the **Delete** (trash) icon next to the class
2. Confirm in the dialog

---

## Managing Subjects

1. Click on a Class to open its detail page
2. Under **Subjects**, click **+ Add Subject**
3. Enter subject name and optional description
4. Click **Save**

Subjects belong to one class. To move a subject to another class, delete and recreate.

---

## Managing Chapters

1. On the Class detail page, expand a Subject
2. Click **+ Add Chapter**
3. Enter chapter title and order index (1 = first, 2 = second, etc.)
4. Click **Save**

Chapters appear in order on the student selection screen.

---

## Uploading Study Materials

### Upload a PDF

1. Go to the Class detail page → expand the Subject
2. Click **Upload PDF** (or navigate to `/admin/classes/:id/subjects/:sid/upload`)
3. Drag-and-drop your PDF onto the upload zone, or click to browse
4. If the PDF belongs to a specific chapter, select the chapter from the dropdown
5. Click **Upload**

**Constraints:**
- File type: **PDF only**
- Maximum size: **50 MB**
- Duplicate detection: uploading the same file again (identical content) shows a 409 error

### Vectorization Status

After upload, the material goes through these states:

| Status | Colour | Meaning |
|--------|--------|---------|
| **Pending** | Grey | Queued for processing |
| **Processing** | Yellow | Being chunked and embedded |
| **Completed** | Green | Ready — students can ask questions |
| **Failed** | Red | Error during processing — hover for details |

Processing time depends on PDF size. A 50-page PDF typically takes 2–5 minutes.

### View All Materials

Go to **Materials** in the sidebar to see all uploaded PDFs with:
- File name and size
- Class / Subject / Chapter assignment
- Vectorization status
- Upload date
- Delete option

### Delete a Material

Deleting a material removes:
- The database record
- All extracted text chunks (and their embeddings)
- The stored PDF file

This cannot be undone. Students using this subject will see fewer (or no) context chunks until new materials are uploaded.

---

## Managing User Accounts

### Create a Student Account

1. Go to **Users** in the sidebar
2. Click **+ New User**
3. Fill in: full name, email, password
4. Set role to **Student**
5. Click **Create**

Share the email and password with the student. They should change their password on first login.

### Create Another Admin

Same as above, but set role to **Admin**. Admins can manage all classes and all users.

### Deactivate a User

1. Find the user in the list
2. Toggle **Active** to off
3. The user will no longer be able to login

---

## Tips

- Upload PDFs per chapter for best answer accuracy — narrower scope means better search results
- PDFs with clear headings and structured text extract better than scanned images
- A student's questions are always scoped to the class+subject of their chat session; you cannot mix materials across subjects in one chat
- Check the **Materials** page regularly for any **Failed** uploads

---

## Related Docs

- [[00-Getting-Started]] — first-run setup
- [[02-Student-Guide]] — what students experience
- [[../System/02-API-Reference]] — API endpoints used by the admin portal
