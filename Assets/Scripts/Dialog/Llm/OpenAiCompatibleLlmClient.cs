using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace LoreLegacyMonsters.Dialog.Llm
{
    public static class OpenAiCompatibleLlmClient
    {
        public static string BuildRequestJson(string model, ChatMessageJson[] messages, float temperature, int maxTokens, bool stream)
        {
            var req = new ChatCompletionRequest
            {
                model = model,
                messages = messages,
                temperature = temperature,
                max_tokens = maxTokens,
                stream = stream
            };
            return JsonUtility.ToJson(req);
        }

        public static IEnumerator SendChatCompletion(
            string completionsUrl,
            string jsonBody,
            int timeoutSeconds,
            Action<bool, string> done)
        {
            if (string.IsNullOrWhiteSpace(completionsUrl))
            {
                Debug.LogError("[LLM] SendChatCompletion: empty completions URL");
                done?.Invoke(false, "Empty completions URL");
                yield break;
            }

            using var request = new UnityWebRequest(completionsUrl, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = Mathf.Clamp(timeoutSeconds, 1, 3600);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                var detail = request.downloadHandler?.text;
                var msg = string.IsNullOrEmpty(detail) ? request.error : $"{request.error}: {detail}";
                Debug.LogWarning($"[LLM] Non-stream request failed ({request.result}): {msg}");
                done?.Invoke(false, msg ?? "Request failed");
                yield break;
            }

            var text = request.downloadHandler.text;
            if (!TryExtractAssistantContent(text, out var content) || string.IsNullOrWhiteSpace(content))
            {
                Debug.LogWarning($"[LLM] Non-stream parse failed. Body head: {Truncate(text, 200)}");
                done?.Invoke(false, "Could not parse assistant content. Body: " + Truncate(text, 400));
                yield break;
            }

            done?.Invoke(true, content);
        }

        /// <summary>Streaming chat; <paramref name="onDelta"/> receives each decoded text delta; <paramref name="done"/> (success, full text or error).</summary>
        public static IEnumerator StreamChatCompletion(
            string completionsUrl,
            string jsonBody,
            int timeoutSeconds,
            Action<string> onDelta,
            Action<bool, string> done,
            Action<UnityWebRequest> onRequestCreated = null)
        {
            if (string.IsNullOrWhiteSpace(completionsUrl))
            {
                Debug.LogError("[LLM] StreamChatCompletion: empty completions URL");
                done?.Invoke(false, "Empty completions URL");
                yield break;
            }

            var handler = new OpenAiSseDownloadHandler(onDelta);
            var request = new UnityWebRequest(completionsUrl, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
            request.downloadHandler = handler;
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "text/event-stream");
            request.timeout = Mathf.Clamp(timeoutSeconds, 1, 3600);
            onRequestCreated?.Invoke(request);

            try
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    var detail = request.downloadHandler?.text;
                    var msg = string.IsNullOrEmpty(detail) ? request.error : $"{request.error}: {detail}";
                    Debug.LogWarning($"[LLM] Stream request failed ({request.result}, code={request.responseCode}): {msg}");
                    done?.Invoke(false, msg ?? "Stream request failed");
                    yield break;
                }

                handler.Flush();

                var full = handler.FullText;
                if (string.IsNullOrWhiteSpace(full))
                {
                    Debug.LogWarning($"[LLM] Stream produced empty body. Chunks received: {handler.ChunkCount}, bytes: {handler.BytesReceived}");
                    done?.Invoke(false, "Empty stream body");
                    yield break;
                }

                done?.Invoke(true, full);
            }
            finally
            {
                request.Dispose();
            }
        }

        public static bool IsTransientFailure(string errorMessage, UnityWebRequest.Result result)
        {
            if (result == UnityWebRequest.Result.ConnectionError || result == UnityWebRequest.Result.DataProcessingError)
                return true;
            if (string.IsNullOrEmpty(errorMessage)) return false;
            if (errorMessage.Contains("500") || errorMessage.Contains("502") || errorMessage.Contains("503") || errorMessage.Contains("504"))
                return true;
            return false;
        }

        public static bool TryExtractAssistantContent(string json, out string content)
        {
            content = null;
            if (string.IsNullOrEmpty(json)) return false;

            try
            {
                var parsed = JsonUtility.FromJson<ChatCompletionResponse>(json);
                if (parsed?.choices == null || parsed.choices.Length == 0) return false;
                var m = parsed.choices[0]?.message;
                if (m == null) return false;
                content = m.content;
                return !string.IsNullOrEmpty(content);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string SanitizeReply(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            var s = raw.Trim().Replace("\r\n", "\n");
            if (s.Length > 4000) s = s.Substring(0, 4000) + "…";
            var sb = new StringBuilder(s.Length);
            foreach (var c in s)
            {
                if (c == '\n' || c == '\t' || !char.IsControl(c)) sb.Append(c);
            }

            return sb.ToString();
        }

        static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max) return s;
            return s.Substring(0, max) + "…";
        }

        sealed class OpenAiSseDownloadHandler : DownloadHandlerScript
        {
            readonly OpenAiSseAccumulator _acc;

            public string FullText => _acc.FullText;
            public int ChunkCount => _acc.ChunkCount;
            public int BytesReceived => _acc.BytesReceived;

            public OpenAiSseDownloadHandler(Action<string> onDelta)
            {
                _acc = new OpenAiSseAccumulator(onDelta);
            }

            public void Flush() => _acc.Flush();

            protected override bool ReceiveData(byte[] data, int dataLength)
            {
                if (data == null || dataLength <= 0) return false;
                _acc.ReceiveData(data, dataLength);
                return true;
            }

            protected override void CompleteContent()
            {
                _acc.Flush();
            }
        }
    }
}
