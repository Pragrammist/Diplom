using Grpc.Core;

public class DuplexStreamLableService
{
    readonly AsyncDuplexStreamingCall<LableTextRequest, LableResponse>  _duplexStream;


    public DuplexStreamLableService(LableService.LableServiceClient client)
    {
        
        
        _duplexStream = client.SendLable();
        
    }
    public static implicit operator AsyncDuplexStreamingCall<LableTextRequest, LableResponse>(DuplexStreamLableService c) 
        => c._duplexStream;

    public async Task<Lable> SendText(string text)
    {
        await _duplexStream.RequestStream.WriteAsync(new LableTextRequest{
            Text = text
        });
        
        var resultFromService = await _duplexStream.ResponseStream.MoveNext();

        if(!resultFromService)
            throw new InvalidOperationException("Lable service end streaming");
        
        return new Lable(_duplexStream.ResponseStream.Current.Label, _duplexStream.ResponseStream.Current.Score);
    }

    
}

public record Lable(string Label, float Score);

