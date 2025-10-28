using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.CAAccount.Provider;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class PoseidonIdentifierHashProvider : CAServerAppService, IPoseidonIdentifierHashProvider
{
    public string GenerateIdentifierHash(string subject, byte[] salt)
    {
        if (subject.Length > CircuitParameters.MaxSubLength)
        {
            throw new UserFriendlyException(
                $"Input subject exceeding max length of {CircuitParameters.MaxSubLength}.");
        }

        var subjectBytes = Encoding.ASCII.GetBytes(subject);
        var hashString =
            new Poseidon.Net.Poseidon().Hash(ChunksToFieldElements(subjectBytes, CircuitParameters.MaxSubLength, 31));
        
        return new Poseidon.Net.Poseidon().Hash(new List<string>()
        {
            hashString, ChunksToFieldElements(salt, CircuitParameters.SaltLength, CircuitParameters.SaltLength).First()
        });
    }

    private List<string> ChunksToFieldElements(byte[] bytes, int requiredLength, int chunkSize = 31)
    {
        var paddedBytes = PadSucceedingZeros(bytes, requiredLength);
        var bigEndianSubjectBytes = paddedBytes.Reverse().ToArray();

        var chunks = SplitByteArrayFromEnd(bigEndianSubjectBytes, chunkSize);
        return chunks.Select(chunk => new BigInteger(chunk, true, true).ToString()).ToList();
    }

    private static byte[] PadSucceedingZeros(byte[] original, int length)
    {
        if (original.Length >= length)
            return original;
        var result = new byte[length];
        Array.Copy(original, result, original.Length);
        return result;
    }

    public List<string> ToPublicInput(string identifierHash)
    {
        return new List<string>{ identifierHash };
    }

    private static byte[][] SplitByteArrayFromEnd(byte[] buffer, int chunkSize)
    {
        var numChunks = (buffer.Length + chunkSize - 1) / chunkSize;
        var chunks = new byte[numChunks][];

        if (numChunks == 1)
        {
            chunks[0] = new byte[buffer.Length];
            Array.Copy(buffer, 0, chunks[0], 0, buffer.Length);
        }

        var currentIndex = buffer.Length - chunkSize;
        var currentChunkIndex = 0;
        while (true)
        {
            var currentChunkSize = Math.Min(chunkSize, buffer.Length - currentChunkIndex * chunkSize);
            chunks[currentChunkIndex] = new byte[currentChunkSize];
            Array.Copy(buffer, currentIndex, chunks[currentChunkIndex], 0, currentChunkSize);
            if (currentIndex == 0)
            {
                break;
            }
            else
            {
                currentIndex = Math.Max(0, currentIndex - currentChunkSize);
            }

            currentChunkIndex++;
        }

        return chunks;
    }
}