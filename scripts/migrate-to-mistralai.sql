-- =============================================================================
-- Switch AI provider: Ollama (vector(768)) → MistralAI (vector(1024))
--
-- Run this script ONCE when you change AI:Provider to "MistralAI" in
-- appsettings.json.
--
-- What it does:
--   1. Deletes all existing chunks (Ollama 768-dim vectors are incompatible
--      with MistralAI 1024-dim vectors — they cannot coexist in one column).
--   2. Replaces the Embedding column with vector(1024).
--   3. Resets all StudyMaterials to Pending so the API re-vectorizes them
--      automatically on next startup (PendingMaterialsRequeueService).
--
-- After running this script:
--   - Set AI:Provider = "MistralAI" and AI:EmbeddingDimensions = 1024 in
--     appsettings.json (or appsettings.Development.json for local dev).
--   - Set AI:MistralAI:ApiKey to your Mistral API key.
--   - Restart the API — it will re-vectorize all PDFs automatically.
--
-- IMPORTANT: This is a destructive operation. All existing chat sessions
-- will lose their source-chunk references until re-vectorization is complete.
-- =============================================================================

BEGIN;

-- Step 1: Remove all chunks (Ollama 768-dim vectors are incompatible)
DELETE FROM "MaterialChunks";

-- Step 2: Replace Embedding column (768-dim → 1024-dim)
ALTER TABLE "MaterialChunks" DROP COLUMN "Embedding";
ALTER TABLE "MaterialChunks" ADD COLUMN "Embedding" vector(1024) NOT NULL;

-- Step 3: Reset all materials so the API re-vectorizes them with MistralAI
UPDATE "StudyMaterials"
SET "VectorizationStatus" = 0,   -- 0 = Pending
    "VectorizationError"  = NULL
WHERE "VectorizationStatus" IN (1, 2, 3);  -- Processing / Completed / Failed

COMMIT;
