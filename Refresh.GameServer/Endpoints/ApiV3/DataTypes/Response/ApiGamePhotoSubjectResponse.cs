using Bunkum.Core.Storage;
using Refresh.GameServer.Database;
using Refresh.GameServer.Types.Data;
using Refresh.GameServer.Types.Photos;

namespace Refresh.GameServer.Endpoints.ApiV3.DataTypes.Response;

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public class ApiGamePhotoSubjectResponse : IApiResponse,IDataConvertableFrom<ApiGamePhotoSubjectResponse, GamePhotoSubject>
{
    public required ApiGameUserResponse? User { get; set; }
    public required string DisplayName { get; set; }
    public required IEnumerable<float> Bounds { get; set; }

    public static ApiGamePhotoSubjectResponse? FromOld(GamePhotoSubject? old, DataContext dataContext)
    {
        if (old == null) return null;

        return new ApiGamePhotoSubjectResponse
        {
            User = ApiGameUserResponse.FromOld(old.User, dataContext),
            DisplayName = old.DisplayName,
            Bounds = old.Bounds,
        };
    }

    public static IEnumerable<ApiGamePhotoSubjectResponse> FromOldList(IEnumerable<GamePhotoSubject> oldList,
        DataContext dataContext) => oldList.Select(old => FromOld(old, dataContext)).ToList()!;
}