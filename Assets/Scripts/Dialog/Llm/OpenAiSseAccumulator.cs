using System;
using System.Text;
using UnityEngine;

namespace LoreLegacyMonsters.Dialog.Llm
{
    /// <summary>
    /// Buffers raw HTTP chunks, splits SSE lines, and appends assistant deltas to a full reply.
    /// Extracted for reuse by <see cref="OpenAiCompatibleLlmClient"/> and unit tests.
    /// </summary>
    public sealed class OpenAiSseAccumulator
    {
        readonly StringBuilder _lineBuf = new StringBuilder(256);
        readonly StringBuilder _full = new StringBuilder(512);
        readonly Action<string> _onDelta;

        public OpenAiSseAccumulator(Action<string> onDelta)
        {
            _onDelta = onDelta;
        }

        public string FullText => _full.ToString();
        public int ChunkCount { get; private set; }
        public int BytesReceived { get; private set; }

        public void ReceiveData(byte[] data, int dataLength)
        {
            if (data == null || dataLength <= 0) return;
            BytesReceived += dataLength;
            _lineBuf.Append(Encoding.UTF8.GetString(data, 0, dataLength));
            DrainCompleteLines();
        }

        public void Flush()
        {
            var rest = _lineBuf.ToString();
            _lineBuf.Clear();
            if (string.IsNullOrWhiteSpace(rest)) return;
            foreach (var raw in rest.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                ProcessLine(raw.TrimEnd('\r'));
        }

        void DrainCompleteLines()
        {
            while (true)
            {
                var s = _lineBuf.ToString();
                var nl = s.IndexOf('\n');
                if (nl < 0) break;
                var line = s.Substring(0, nl).TrimEnd('\r');
                var rest = nl + 1 < s.Length ? s.Substring(nl + 1) : string.Empty;
                _lineBuf.Clear();
                _lineBuf.Append(rest);
                ProcessLine(line);
            }
        }

        void ProcessLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;
            if (!line.StartsWith("data:", StringComparison.Ordinal)) return;
            var payload = line.Substring(5).TrimStart();
            if (payload == "[DONE]") return;
            if (string.IsNullOrEmpty(payload)) return;

            try
            {
                var parsed = JsonUtility.FromJson<ChatStreamChunk>(payload);
                if (parsed?.choices == null || parsed.choices.Length == 0) return;
                var d = parsed.choices[0]?.delta;
                if (d == null || string.IsNullOrEmpty(d.content)) return;
                ChunkCount++;
                _full.Append(d.content);
                _onDelta?.Invoke(d.content);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LLM] Dropped malformed SSE chunk ({ex.Message}): {payload.Substring(0, Mathf.Min(payload.Length, 120))}");
            }
        }
    }
}
