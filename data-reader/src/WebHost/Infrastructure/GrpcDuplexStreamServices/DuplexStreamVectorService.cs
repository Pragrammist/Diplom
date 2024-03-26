
using Grpc.Core;

public class DuplexStreamVectorService
{
    readonly AsyncDuplexStreamingCall<VectorTextRequest, VectorResponse>  _duplexStream;


    public DuplexStreamVectorService(VectorService.VectorServiceClient client)
    {
        
        
        _duplexStream = client.SendVector();
        
    }
    

    public async Task<Vector> SendText(string text)
    {
        await _duplexStream.RequestStream.WriteAsync(new VectorTextRequest{
            Text = text
        });
        
        var resultFromService = await _duplexStream.ResponseStream.MoveNext();

        if(!resultFromService)
            throw new InvalidOperationException("Lable service end streaming");
        
        return new Vector(_duplexStream.ResponseStream.Current.Vector);
    }

    
}

public record Vector(IEnumerable<float> Embeddings);

public class TestService
{
    DuplexStreamVectorService _s; 
    DuplexStreamLableService _s2;
    public TestService(DuplexStreamVectorService s, DuplexStreamLableService s2)
    {
        _s = s;
        _s2 = s2;
    }

    public async Task TestMethod(string text)
    {
        await _s.SendText(text);
        await _s.SendText(text);
    }
}