using AttribDoc;
using Refresh.GameServer.Types.Data;
using Refresh.GameServer.Types.Roles;

namespace Refresh.GameServer.Endpoints.ApiV3.DataTypes.Response;

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public class ApiRouteResponse : IApiResponse
{
    public required string Method { get; set; }
    public required string RouteUri { get; set; }
    public required string Summary { get; set; }
    public required bool AuthenticationRequired { get; set; }
    public required GameUserRole? MinimumRole { get; set; }
    public required IEnumerable<ApiParameterResponse> Parameters { get; set; }
    public required IEnumerable<ApiErrorResponse> PotentialErrors { get; set; }
    
    public static ApiRouteResponse? FromOld(Route? old)
    {
        if (old == null) return null;

        return new ApiRouteResponse
        {
            Method = old.Method,
            RouteUri = old.RouteUri,
            Summary = old.Summary,
            AuthenticationRequired = old.AuthenticationRequired,
            Parameters = ApiParameterResponse.FromOldList(old.Parameters),
            PotentialErrors = ApiErrorResponse.FromOldList(old.PotentialErrors),
            MinimumRole = (GameUserRole?)old.ExtraProperties.GetValueOrDefault("minimumRole"),
        };
    }

    public static IEnumerable<ApiRouteResponse> FromOldList(IEnumerable<Route> oldList) => oldList.Select(old => FromOld(old)).ToList()!;
}