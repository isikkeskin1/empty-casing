# Read-only file context — development feature

## Use

1. Open Capri Mainframe and optionally type a question into the prompt box.
2. Press READ FILE and explicitly choose a local file.
3. Capri validates the type and size, reads UTF-8 text and screens for
   likely credentials. The app never runs selected source code.
4. If using a cloud brain, approve sending **this file's excerpt** to that
   provider. Declining sends nothing.
5. Capri answers your question (or summarizes the file) in the current
   chat without invoking the desktop action planner.

Supported formats: .txt, .md, .csv, .json, .log, .py, .cs, .js, .ts,
.tsx, .jsx, .java, .c, .cpp, .h, .hpp, .html, .css, .xml,
.yaml, .yml, .toml, .ini, .sql, .rs and .go.

Only local non-reparse files are accepted. This is not a PDF or
Word-document reader; documents, image OCR, spreadsheets, and project-wide
search require separate, consented implementations.

## Privacy and limits

- Maximum source size: 128 KiB; first 12,000 characters only.
- The scanner recognizes common credential formats. It is **not**
  a complete privacy/PII detector. Review files yourself before sharing.
- A local Ollama brain receives the question directly on your PC.
  A cloud brain receives the excerpt only after a separate confirmation.
  Third-party provider policies may apply.
- The full file path is never sent to the brain; only the filename,
  excerpt and user's question.
- File text is fenced and described as **untrusted data**. Instructions
  embedded in a selected file cannot authorize computer actions.
- Raw excerpts aren't stored in chat history. File-generated results are
  displayed for the current session only and excluded from later model
  conversation context and persistent history. The filename/question
  metadata remains in the local history.
- No auto-TTS of file-derived answers: this prevents accidentally relaying
  file content to Fish Audio.

## Device/manual test checklist

- Test code, JSON, Markdown and CSV on disk, including Unicode UTF-8.
- Test malformed Unicode and binary/NUL text; reject safely.
- Test large files: reject over 128 KiB, truncate excerpt above 12k chars.
- Test .env/.exe/.pdf and directory selection; do not ingest.
- Test files containing API-key patterns and private-key headers.
- Verify a cloud-model confirmation appears each time; decline and inspect
  provider logs for absence of a request.
- Verify no raw excerpt appears in the local JSON history.
- Restart Capri and verify temporary file-derived answers don't persist.
- Verify later chat turns don't include temporary file-derived answers.
- Verify file-derived responses are never sent to the Fish voice provider.
- Test failed file read, denied permissions and encrypted/cloud-synced files.

This is a source implementation awaiting manual Windows/device QA.
