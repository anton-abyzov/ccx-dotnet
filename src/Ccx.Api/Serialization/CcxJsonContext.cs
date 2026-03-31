using System.Text.Json.Serialization;
using Ccx.Api.Models;

namespace Ccx.Api.Serialization;

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(MessageRequest))]
[JsonSerializable(typeof(MessageResponse))]
[JsonSerializable(typeof(StreamEvent))]
[JsonSerializable(typeof(ContentBlock))]
[JsonSerializable(typeof(Message))]
[JsonSerializable(typeof(ToolDefinition))]
[JsonSerializable(typeof(Delta))]
[JsonSerializable(typeof(Usage))]
[JsonSerializable(typeof(List<ContentBlock>))]
[JsonSerializable(typeof(List<Message>))]
[JsonSerializable(typeof(List<ToolDefinition>))]
public partial class CcxJsonContext : JsonSerializerContext;
