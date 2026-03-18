using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SAFARIstack.Infrastructure.Resilience;

/// <summary>
/// Edge buffer for offline resilience - queues operations when network is unavailable
/// </summary>
public class EdgeBuffer : IEdgeBuffer
{
    private readonly ILogger<EdgeBuffer> _logger;
    private readonly ConcurrentQueue<BufferedOperation> _buffer = new();
    private readonly string _bufferFilePath;
    private const int MAX_BUFFER_SIZE = 1000;

    public EdgeBuffer(ILogger<EdgeBuffer> logger)
    {
        _logger = logger;
        _bufferFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SAFARIstack",
            "edge-buffer.json");
        
        LoadBufferFromDisk();
    }

    public async Task<bool> BufferOperationAsync(
        string operationType,
        string entityType,
        Guid entityId,
        object data,
        CancellationToken cancellationToken = default)
    {
        if (_buffer.Count >= MAX_BUFFER_SIZE)
        {
            _logger.LogWarning("Edge buffer is full, dropping operation {OperationType} for {EntityType}:{EntityId}",
                operationType, entityType, entityId);
            return false;
        }

        var operation = new BufferedOperation
        {
            Id = Guid.NewGuid(),
            OperationType = operationType,
            EntityType = entityType,
            EntityId = entityId,
            Data = JsonSerializer.Serialize(data),
            Timestamp = DateTime.UtcNow,
            RetryCount = 0
        };

        _buffer.Enqueue(operation);
        await PersistBufferToDiskAsync(cancellationToken);

        _logger.LogInformation("Buffered operation {OperationType} for {EntityType}:{EntityId}",
            operationType, entityType, entityId);

        return true;
    }

    public async Task<IEnumerable<BufferedOperation>> GetPendingOperationsAsync(
        CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_buffer.ToList());
    }

    public async Task<bool> RemoveOperationAsync(
        Guid operationId,
        CancellationToken cancellationToken = default)
    {
        var operations = _buffer.ToList();
        var operation = operations.FirstOrDefault(o => o.Id == operationId);
        
        if (operation == null)
            return false;

        // Create new queue without the operation
        var newQueue = new ConcurrentQueue<BufferedOperation>(
            operations.Where(o => o.Id != operationId));
        
        // Replace the queue
        while (_buffer.TryDequeue(out _)) { }
        foreach (var op in newQueue)
        {
            _buffer.Enqueue(op);
        }

        await PersistBufferToDiskAsync(cancellationToken);
        return true;
    }

    public async Task<int> ProcessBufferedOperationsAsync(
        Func<BufferedOperation, Task<bool>> processor,
        CancellationToken cancellationToken = default)
    {
        var processed = 0;
        var operations = _buffer.ToList();

        foreach (var operation in operations)
        {
            try
            {
                var success = await processor(operation);
                if (success)
                {
                    await RemoveOperationAsync(operation.Id, cancellationToken);
                    processed++;
                }
                else
                {
                    operation.RetryCount++;
                    operation.LastRetryAt = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process buffered operation {OperationId}", operation.Id);
                operation.RetryCount++;
                operation.LastRetryAt = DateTime.UtcNow;
                operation.LastError = ex.Message;
            }
        }

        if (processed > 0)
        {
            await PersistBufferToDiskAsync(cancellationToken);
        }

        return processed;
    }

    public int GetBufferCount() => _buffer.Count;

    private async Task PersistBufferToDiskAsync(CancellationToken cancellationToken)
    {
        try
        {
            var directory = Path.GetDirectoryName(_bufferFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_buffer.ToList(), new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_bufferFilePath, json, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist edge buffer to disk");
        }
    }

    private void LoadBufferFromDisk()
    {
        try
        {
            if (File.Exists(_bufferFilePath))
            {
                var json = File.ReadAllText(_bufferFilePath);
                var operations = JsonSerializer.Deserialize<List<BufferedOperation>>(json);
                
                if (operations != null)
                {
                    foreach (var operation in operations)
                    {
                        _buffer.Enqueue(operation);
                    }
                    
                    _logger.LogInformation("Loaded {Count} buffered operations from disk", operations.Count);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load edge buffer from disk");
        }
    }
}

public interface IEdgeBuffer
{
    Task<bool> BufferOperationAsync(
        string operationType,
        string entityType,
        Guid entityId,
        object data,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<BufferedOperation>> GetPendingOperationsAsync(
        CancellationToken cancellationToken = default);

    Task<bool> RemoveOperationAsync(Guid operationId, CancellationToken cancellationToken = default);

    Task<int> ProcessBufferedOperationsAsync(
        Func<BufferedOperation, Task<bool>> processor,
        CancellationToken cancellationToken = default);

    int GetBufferCount();
}

public class BufferedOperation
{
    public Guid Id { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Data { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int RetryCount { get; set; }
    public DateTime? LastRetryAt { get; set; }
    public string? LastError { get; set; }
}
