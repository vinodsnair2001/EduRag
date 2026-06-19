-- =============================================================================
-- Switch AI provider: MistralAI (vector(1024)) → Ollama (vector(768))
--
-- Run this script ONCE when you change AI:Provider back to "Ollama" in
-- appsettings.json.
--
-- What it does:
--   1. Deletes all existing chunks (MistralAI 1024-dim vectors are incompatible
--      with Ollama 768-dim vectors — they cannot coexist in one column).
--   2. Replaces the Embedding column with vector(768).
--   3. Resets all StudyMaterials to Pending so the API re-vectorizes them
--      automatically on next startup (PendingMaterialsRequeueService).
--
-- After running this script:
--   - Set AI:Provider = "Ollama" and AI:EmbeddingDimensions = 768 in
--     appsettings.json (or appsettings.Development.json for local dev).
--   - Ensure Ollama is running with nomic-embed-text and llama3.2 models.
--   - Restart the API — it will re-vectorize all PDFs automatically.
--
-- IMPORTANT: This is a destructive operation. All existing chat sessions
-- will lose their source-chunk references until re-vectorization is complete.
-- =============================================================================

BEGIN;

-- Step 1: Remove all chunks (MistralAI 1024-dim vectors are incompatible)
DELETE FROM "MaterialChunks";

-- Step 2: Replace Embedding column (1024-dim → 768-dim)
ALTER TABLE "MaterialChunks" DROP COLUMN "Embedding";
ALTER TABLE "MaterialChunks" ADD COLUMN "Embedding" vector(768) NOT NULL;

-- Step 3: Reset all materials so the API re-vectorizes them with Ollama
UPDATE "StudyMaterials"
SET "VectorizationStatus" = 0,   -- 0 = Pending
    "VectorizationError"  = NULL
WHERE "VectorizationStatus" IN (1, 2, 3);  -- Processing / Completed / Failed

COMMIT;
