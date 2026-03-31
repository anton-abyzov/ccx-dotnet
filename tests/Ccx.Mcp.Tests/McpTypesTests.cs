using System.Text.Json;
using Ccx.Mcp;
using FluentAssertions;

namespace Ccx.Mcp.Tests;

public class McpTypesTests
{
    [Fact]
    public void JsonRpcRequest_Serializes()
    {
        var req = new JsonRpcRequest { Id = 1, Method = "tools/list" };
        var json = JsonSerializer.Serialize(req, McpJsonContext.Default.JsonRpcRequest);

        json.Should().Contain("\"jsonrpc\":\"2.0\"");
        json.Should().Contain("\"method\":\"tools/list\"");
    }

    [Fact]
    public void JsonRpcResponse_Deserializes()
    {
        var json = """{"jsonrpc":"2.0","id":1,"result":{"tools":[]}}""";
        var response = JsonSerializer.Deserialize(json, McpJsonContext.Default.JsonRpcResponse);

        response.Should().NotBeNull();
        response!.Id.Should().Be(1);
        response.Error.Should().BeNull();
        response.Result.Should().NotBeNull();
    }

    [Fact]
    public void JsonRpcResponse_ErrorDeserializes()
    {
        var json = """{"jsonrpc":"2.0","id":1,"error":{"code":-32600,"message":"Invalid"}}""";
        var response = JsonSerializer.Deserialize(json, McpJsonContext.Default.JsonRpcResponse);

        response!.Error.Should().NotBeNull();
        response.Error!.Code.Should().Be(-32600);
        response.Error.Message.Should().Be("Invalid");
    }

    [Fact]
    public void McpToolDef_Deserializes()
    {
        var json = """{"name":"read","description":"Read a file"}""";
        var tool = JsonSerializer.Deserialize(json, McpJsonContext.Default.McpToolDef);

        tool!.Name.Should().Be("read");
        tool.Description.Should().Be("Read a file");
    }
}
