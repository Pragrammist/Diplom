public record CommentData(
    string Text, 
    string Date, 
    string Place, 
    string InternetPlace, 
    string Seller, 
    string ClientName, 
    string MarketPlaceType, 
    string Url,
    float[] Embeddings,
    Lable Label
)
 {
    public string LabelValue => Label.Label; 
    public float LabelScore => Label.Score;

    public string Id => Text;
};

public record CommentDataWithElasticScore(
    CommentData CommentData,
    double ElasticScore) : 
    CommentData(
        CommentData.Text, 
        CommentData.Date, 
        CommentData.Place, 
        CommentData.InternetPlace, 
        CommentData.Seller, 
        CommentData.ClientName, 
        CommentData.MarketPlaceType, 
        CommentData.Url, 
        CommentData.Embeddings, 
        CommentData.Label
    );
   
