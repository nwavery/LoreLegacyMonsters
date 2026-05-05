using System.Text;
using LoreLegacyMonsters.Dialog.Llm;
using NUnit.Framework;

namespace LoreLegacyMonsters.Tests
{
    public class OpenAiSseAccumulatorTests
    {
        static byte[] Utf8(string s) => Encoding.UTF8.GetBytes(s);

        static string DeltaPayload(string content) =>
            "{\"choices\":[{\"delta\":{\"content\":\"" + content.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"}}]}";

        [Test]
        public void Accumulator_AppendsContentFromCompleteDataLine()
        {
            var acc = new OpenAiSseAccumulator(_ => { });
            var line = "data: " + DeltaPayload("Hi") + "\n";
            acc.ReceiveData(Utf8(line), line.Length);
            acc.Flush();
            Assert.AreEqual("Hi", acc.FullText);
            Assert.AreEqual(1, acc.ChunkCount);
        }

        [Test]
        public void Accumulator_SplitsMidLineAcrossReceiveChunks()
        {
            var acc = new OpenAiSseAccumulator(_ => { });
            var full = "data: " + DeltaPayload("Hello") + "\n";
            acc.ReceiveData(Utf8(full.Substring(0, 5)), 5);
            acc.ReceiveData(Utf8(full.Substring(5)), full.Length - 5);
            acc.Flush();
            Assert.AreEqual("Hello", acc.FullText);
            Assert.AreEqual(1, acc.ChunkCount);
        }

        [Test]
        public void Accumulator_IgnoresDoneTerminator()
        {
            var acc = new OpenAiSseAccumulator(_ => { });
            var part1 = "data: " + DeltaPayload("X") + "\n";
            acc.ReceiveData(Utf8(part1), part1.Length);
            var doneLine = "data: [DONE]\n";
            acc.ReceiveData(Utf8(doneLine), doneLine.Length);
            acc.Flush();
            Assert.AreEqual("X", acc.FullText);
        }

        [Test]
        public void Accumulator_SkipsEmptyContentDeltas()
        {
            var acc = new OpenAiSseAccumulator(_ => { });
            var emptyDelta = "{\"choices\":[{\"delta\":{\"content\":\"\"}}]}";
            var payload = "data: " + emptyDelta + "\ndata: " + DeltaPayload("Z") + "\n";
            acc.ReceiveData(Utf8(payload), payload.Length);
            acc.Flush();
            Assert.AreEqual("Z", acc.FullText);
            Assert.AreEqual(1, acc.ChunkCount);
        }

        [Test]
        public void Accumulator_ConcatenatesMultipleDeltas()
        {
            var acc = new OpenAiSseAccumulator(_ => { });
            var s = "data: " + DeltaPayload("A") + "\ndata: " + DeltaPayload("B") + "\n";
            acc.ReceiveData(Utf8(s), s.Length);
            acc.Flush();
            Assert.AreEqual("AB", acc.FullText);
            Assert.AreEqual(2, acc.ChunkCount);
        }

        [Test]
        public void Accumulator_OnDeltaInvokedPerChunk()
        {
            var n = 0;
            var acc = new OpenAiSseAccumulator(_ => n++);
            var s = "data: " + DeltaPayload("A") + "\ndata: " + DeltaPayload("B") + "\n";
            acc.ReceiveData(Utf8(s), s.Length);
            acc.Flush();
            Assert.AreEqual(2, n);
        }
    }
}
