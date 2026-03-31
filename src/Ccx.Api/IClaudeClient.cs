using Ccx.Api.Models;

namespace Ccx.Api;

public interface IClaudeClient
{
    IAsyncEnumerable<StreamEvent> StreamMessageAsync(MessageRequest request, CancellationToken ct = default);
}
