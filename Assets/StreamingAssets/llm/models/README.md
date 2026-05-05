# Bundled LLM Model Staging

Place the release model artifact used by the bundled runtime in this folder.

Expected artifact name for v1.0 planning:

- `llama3.2-q4_k_m.gguf`

or equivalent Ollama model blob layout if your runtime expects that format.

Do not check large model binaries into source control unless explicitly approved.
Use CI artifact download or secure release storage.
