---
tags: [user-guide, faq, questions]
created: 2026-06-18
updated: 2026-06-18
type: user
status: stable
aliases: [FAQ, Frequently Asked Questions]
---

# Frequently Asked Questions

> [[_HOME|← Home]]

---

## For Students

**Q: Why does the AI say "I couldn't find this in the uploaded study material"?**

A: The AI only knows what's in the PDFs your teacher uploaded. If a topic isn't in those PDFs, the AI can't answer it. Ask your teacher to upload a PDF that covers that topic.

---

**Q: Can the AI lie or make up answers?**

A: EduRAG is designed to only answer from your study materials. If it can't find relevant content it says so. It may occasionally make small errors — always cross-check important answers with your textbook.

---

**Q: My practice question answer was marked INCORRECT but I think I was right. What do I do?**

A: The AI grades based on what's in the study material. If you believe your answer is correct, discuss it with your teacher. The AI's explanation will show you what the material says.

---

**Q: Can I use EduRAG to do my homework?**

A: EduRAG is a learning tool, not a homework-completion service. Use it to understand concepts, then write your own answers. Your teachers will know the study material well — always try to understand, not just copy.

---

**Q: Why are the practice questions always about the same topics?**

A: Questions are generated from the study materials uploaded to your subject. If there are only a few PDFs, the questions will cover those topics. Ask your teacher to upload more materials.

---

**Q: I can't log in. What do I do?**

A: Check:
1. You are using the exact email your teacher gave you
2. Your password is correct (case-sensitive)
3. The EduRAG server is running (ask your teacher/IT)

---

**Q: Can my classmates see my chat history?**

A: No. Your chat sessions are private and only visible to you.

---

## For Admins

**Q: How long does vectorization take?**

A: Depends on PDF size and server speed. Approximately:
- 10-page PDF: ~30 seconds
- 50-page PDF: ~2–5 minutes
- 100-page PDF: ~5–10 minutes

During this time students can still use the chat — they just won't get answers from the new PDF yet.

---

**Q: Can I upload Word documents or PowerPoint files?**

A: Not directly. Convert to PDF first (File → Save As → PDF in Office apps), then upload.

---

**Q: What happens if I delete a subject that students are currently chatting in?**

A: Deleting a subject cascades — it removes all materials and chunks. Students' existing chat message history is preserved but future questions in that subject will have no context. Students cannot open new chat sessions in a deleted subject.

---

**Q: Can I upload the same PDF twice?**

A: No. EduRAG computes a SHA-256 fingerprint of each PDF. Uploading an identical file returns a `409 Conflict` error. If you need to replace a PDF, delete the old one first then upload the new version.

---

**Q: Do I need internet access for EduRAG?**

A: No. EduRAG runs entirely on your local server. The AI models (Ollama) are downloaded once and then run offline. No data is sent to external services.

---

**Q: What if a scanned PDF returns no answers?**

A: Scanned PDFs are images — the text can't be extracted without OCR. Options:
1. Use a text-based PDF (from a digital source)
2. A future version of EduRAG will add OCR support for scanned documents

---

**Q: How many students can use the chat at the same time?**

A: The bottleneck is the Ollama model. With a GPU, llama3.2 can handle 2–4 concurrent chat sessions comfortably. On CPU only, responses will be slower with concurrent users. For large deployments, consider a server with a modern GPU.

---

## Related Docs

- [[00-Getting-Started]] — first-run setup
- [[01-Admin-Guide]] — admin operations
- [[02-Student-Guide]] — student operations
- [[../System/06-Troubleshooting]] — technical errors
