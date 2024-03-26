using Hangfire;



public interface IObservable
{
    Task NotifyObservers(MarketplaceIndetificator marketplace);
}

public interface IObserver
{
    Task Update(MarketplaceIndetificator marketplace);
}


public class MarketPlacesObservable : IObservable
{

    private readonly IMarketPlaceRepository _marketPlaceRepository;

   

    private readonly MarketPlacesObserver _marketPlacesObserver;

    public MarketPlacesObservable(IMarketPlaceRepository marketPlaceRepository, MarketPlacesObserver marketPlacesObserver)
    {
        _marketPlaceRepository = marketPlaceRepository;
        _marketPlacesObserver = marketPlacesObserver;
    }

    

    
    
    public async Task NotifyObservers(MarketplaceIndetificator marketplace)
    {
        
        var fm = await _marketPlaceRepository.GetMarketPlace(marketplace.Id);
        

        if(fm is null)
        {
            var isAdded = await _marketPlaceRepository.SetMarketplace(marketplace);

            if(!isAdded)
                return;
        }

        await _marketPlacesObserver.Update(marketplace);
            
    }
}



public class MarketPlacesObserver : IObserver
{
    public Task Update(MarketplaceIndetificator marketplace)
    {
        BackgroundJob.Enqueue<DataLoaderService>(d => d.LoadData(marketplace.Id));
        return Task.CompletedTask;
    }
}

