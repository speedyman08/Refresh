using Bunkum.Core.Services;
using Bunkum.Protocols.Http.Direct;
using JetBrains.Annotations;
using Refresh.GameServer.Authentication;
using Refresh.GameServer.Database;
using Refresh.GameServer.Types;
using Refresh.GameServer.Types.Contests;
using Refresh.GameServer.Types.Levels;
using Refresh.GameServer.Types.Roles;
using Refresh.GameServer.Types.UserData;
using Refresh.GameServer.Types.UserData.Leaderboard;
using RefreshTests.GameServer.Time;

namespace RefreshTests.GameServer;

public class TestContext : IDisposable
{
    public Lazy<TestRefreshGameServer> Server { get; }
    public GameDatabaseContext Database { get; }
    public HttpClient Http { get; }
    public MockDateTimeProvider Time { get; }
    private DirectHttpListener Listener { get; }
    
    public TestContext(Lazy<TestRefreshGameServer> server, GameDatabaseContext database, HttpClient http, DirectHttpListener listener, MockDateTimeProvider time)
    {
        this.Server = server;
        this.Database = database;
        this.Http = http;
        this.Listener = listener;
        this.Time = time;
    }

    private int _users = 100; // start at 100 since usernames require 3 characters
    private int UserIncrement => this._users++;

    public HttpClient GetAuthenticatedClient(TokenType type,
        GameUser? user = null,
        int tokenExpirySeconds = GameDatabaseContext.DefaultTokenExpirySeconds)
    {
        return this.GetAuthenticatedClient(type, out _, user, tokenExpirySeconds);
    }
    
    public HttpClient GetAuthenticatedClient(TokenType type, TokenGame game, TokenPlatform platform,
        GameUser? user = null,
        int tokenExpirySeconds = GameDatabaseContext.DefaultTokenExpirySeconds)
    {
        return this.GetAuthenticatedClient(type, game, platform, out _, user, tokenExpirySeconds);
    }

    public HttpClient GetAuthenticatedClient(TokenType type, out string tokenData,
        GameUser? user = null,
        int tokenExpirySeconds = GameDatabaseContext.DefaultTokenExpirySeconds)
    {
        user ??= this.CreateUser();

        TokenGame game = type switch
        {
            TokenType.Game => TokenGame.LittleBigPlanet2,
            _ => TokenGame.Website,
        };

        TokenPlatform platform = type switch
        {
            TokenType.Game => TokenPlatform.PS3,
            _ => TokenPlatform.Website,
        };

        return this.GetAuthenticatedClient(type, game, platform, out tokenData, user, tokenExpirySeconds);
    }
    
    public HttpClient GetAuthenticatedClient(TokenType type, TokenGame game, TokenPlatform platform, out string tokenData,
        GameUser? user = null,
        int tokenExpirySeconds = GameDatabaseContext.DefaultTokenExpirySeconds)
    {
        user ??= this.CreateUser();

        Token token = this.Database.GenerateTokenForUser(user, type, game, platform, tokenExpirySeconds);
        tokenData = token.TokenData;
        
        HttpClient client = this.Listener.GetClient();

        if (type == TokenType.Game)
        {
            client.DefaultRequestHeaders.Add("Cookie", "MM_AUTH=" + token.TokenData);
        }
        else
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token.TokenData);
        }

        return client;
    }

    public GameUser CreateUser(string? username = null)
    {
        username ??= this.UserIncrement.ToString();
        return this.Database.CreateUser(username, $"{username}@{username}.local");
    }

    public GameUser CreateAdmin(string? username = null)
    {
        GameUser user = this.CreateUser(username);
        this.Database.SetUserRole(user, GameUserRole.Admin);
        return user;
    }

    public Token CreateToken(GameUser user, TokenType type = TokenType.Game, TokenGame game = TokenGame.LittleBigPlanet2, TokenPlatform platform = TokenPlatform.PS3)
    {
        return this.Database.GenerateTokenForUser(user, type, game, platform);
    }
    
    public GameLevel CreateLevel(GameUser author, string title = "Level", TokenGame gameVersion = TokenGame.LittleBigPlanet1)
    {
        GameLevel level = new()
        {
            Title = title,
            Publisher = author,
            Location = GameLocation.Zero,
            Source = GameLevelSource.User,
            GameVersion = gameVersion,
        };

        this.Database.AddLevel(level);
        return level;
    }

    public void FillLeaderboard(GameLevel level, int count, byte type)
    {
        for (byte i = 0; i < count; i++)
        {
            GameUser scoreUser = this.CreateUser("score" + i);
            this.SubmitScore(i, type, level, scoreUser, TokenGame.LittleBigPlanet2, TokenPlatform.PS3);
        }
    }

    public GameSubmittedScore SubmitScore(int score, byte type, GameLevel level, GameUser user, TokenGame game, TokenPlatform platform)
    {
        SerializedScore scoreObject = new()
        {
            Host = true,
            Score = score,
            ScoreType = type,
        };
        
        GameSubmittedScore submittedScore = this.Database.SubmitScore(scoreObject, user, level, game, platform);
        Assert.That(submittedScore, Is.Not.Null);

        return submittedScore;
    }

    [Pure]
    public TService GetService<TService>() where TService : Service => this.Server.Value.GetService<TService>();

    public void Dispose()
    {
        this.Database.Dispose();
        this.Http.Dispose();
        this.Listener.Dispose();

        if (this.Server.IsValueCreated)
            this.Server.Value.Stop();

        GC.SuppressFinalize(this);
    }
}