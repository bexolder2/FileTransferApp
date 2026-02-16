using System.Buffers.Binary;
using System.Text;
using System.Text.Json;

namespace FileTransfer.Infrastructure.Transfer.Protocol;

public sealed class TransferProtocolSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task WriteJsonFrameAsync<T>(
        Stream stream,
        TransferFrameType frameType,
        T payload,
        CancellationToken cancellationToken)
    {
        byte[] payloadBytes = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
        await WriteFrameHeaderAsync(stream, frameType, payloadBytes.Length, cancellationToken);
        await stream.WriteAsync(payloadBytes, cancellationToken);
    }

    public async Task<T> ReadJsonFrameAsync<T>(
        Stream stream,
        TransferFrameType expectedFrameType,
        CancellationToken cancellationToken)
    {
        (TransferFrameType frameType, int payloadLength) = await ReadFrameHeaderAsync(stream, cancellationToken);
        if (frameType != expectedFrameType)
        {
            throw new InvalidDataException($"Expected {expectedFrameType} frame but received {frameType}.");
        }

        byte[] payload = new byte[payloadLength];
        await ReadExactAsync(stream, payload, cancellationToken);
        T? deserialized = JsonSerializer.Deserialize<T>(payload, JsonOptions);
        if (deserialized is null)
        {
            throw new InvalidDataException("Failed to deserialize protocol frame payload.");
        }

        return deserialized;
    }

    public async Task WriteFrameAsync(
        Stream stream,
        TransferFrameType frameType,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken)
    {
        await WriteFrameHeaderAsync(stream, frameType, payload.Length, cancellationToken);
        if (!payload.IsEmpty)
        {
            await stream.WriteAsync(payload, cancellationToken);
        }
    }

    public async Task<(TransferFrameType FrameType, byte[] Payload)> ReadFrameAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        (TransferFrameType frameType, int payloadLength) = await ReadFrameHeaderAsync(stream, cancellationToken);
        byte[] payload = new byte[payloadLength];
        if (payloadLength > 0)
        {
            await ReadExactAsync(stream, payload, cancellationToken);
        }

        return (frameType, payload);
    }

    private static async Task WriteFrameHeaderAsync(
        Stream stream,
        TransferFrameType frameType,
        int payloadLength,
        CancellationToken cancellationToken)
    {
        byte[] header = new byte[5];
        header[0] = (byte)frameType;
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(1), payloadLength);
        await stream.WriteAsync(header, cancellationToken);
    }

    private static async Task<(TransferFrameType FrameType, int PayloadLength)> ReadFrameHeaderAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        byte[] header = new byte[5];
        await ReadExactAsync(stream, header, cancellationToken);

        TransferFrameType frameType = (TransferFrameType)header[0];
        int payloadLength = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(1));
        if (payloadLength < 0)
        {
            throw new InvalidDataException("Protocol frame payload length is invalid.");
        }

        return (frameType, payloadLength);
    }

    private static async Task ReadExactAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        int offset = 0;
        while (offset < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), cancellationToken);
            if (read <= 0)
            {
                throw new EndOfStreamException("Unexpected end of stream while reading protocol frame.");
            }

            offset += read;
        }
    }
}
