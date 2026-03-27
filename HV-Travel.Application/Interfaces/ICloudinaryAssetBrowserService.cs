using System.Threading;
using System.Threading.Tasks;
using HVTravel.Application.Models;

namespace HVTravel.Application.Interfaces;

public interface ICloudinaryAssetBrowserService
{
    Task<CloudinaryAssetBrowseResult> GetAssetsAsync(CloudinaryAssetBrowseRequest request, CancellationToken cancellationToken = default);
}
