using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.Extension;

namespace DiscordBot.Helper
{
    public class ConcurrentRandomHelper
    {

        public Random GetConcurrentRandomInstance(string? seed = null)
        {
            if (string.IsNullOrEmpty(seed)) return new ConcurrentRandom();
            byte[] textData = Encoding.UTF8.GetBytes(seed);
            byte[] hash = SHA256.HashData(textData);
            return new ConcurrentRandom(BitConverter.ToInt32(hash));
        }

        public class ConcurrentRandom : Random
        {
            private ConcurrentDictionary<(int, int), ConcurrentQueue<int>> ResultQueueDict { get; } = [];
            private int BatchSize { get; } = 100_000;

            public ConcurrentRandom() : base()
            {
            }

            public ConcurrentRandom(int seed) : base(seed)
            {
            }

            public override int Next(int minValue, int maxValue)
            {
                ConcurrentQueue<int> resultQueue = ResultQueueDict.GetValueOrDefault((minValue, maxValue), new ConcurrentQueue<int>());
                ResultQueueDict[(minValue, maxValue)] = resultQueue;
                if (resultQueue.IsEmpty) GenerateBatch(resultQueue, minValue, maxValue);
                resultQueue.TryDequeue(out int result);
                return result;
            }

            private void GenerateBatch(ConcurrentQueue<int> resultQueue, int minValue, int maxValue)
            {
                for (int i = 0; i < BatchSize; i++)
                {
                    resultQueue.Enqueue(base.Next(minValue, maxValue));
                }
            }
        }

    }
}
