using System;

namespace LoreLegacyMonsters.Dialog.Llm
{
    [Serializable]
    public class ChatMessageJson
    {
        public string role;
        public string content;
    }

    [Serializable]
    public class ChatCompletionRequest
    {
        public string model;
        public ChatMessageJson[] messages;
        public float temperature;
        public int max_tokens;
        public bool stream;
    }

    [Serializable]
    public class ChatCompletionResponse
    {
        public ChoiceBlock[] choices;
    }

    [Serializable]
    public class ChoiceBlock
    {
        public MessageBlock message;
    }

    [Serializable]
    public class MessageBlock
    {
        public string role;
        public string content;
    }

    [Serializable]
    public class ChatStreamChunk
    {
        public StreamChoiceBlock[] choices;
    }

    [Serializable]
    public class StreamChoiceBlock
    {
        public StreamDeltaBlock delta;
    }

    [Serializable]
    public class StreamDeltaBlock
    {
        public string content;
    }
}
