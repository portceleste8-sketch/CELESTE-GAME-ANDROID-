using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Celeste.Core.Services;

namespace Celeste.Android;

/// <summary>
/// CelesteGame - Classe principal do jogo MonoGame para Android.
/// Inicializa o serviço de plataforma, logger e assets.
/// </summary>
public class CelesteGame : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch? _spriteBatch;
    private readonly string _contentRoot;
    private readonly bool _fpsEnabled;
    private readonly bool _verboseLogs;
    private IPlatformPaths? _platformPaths;
    private ILogSystem? _logger;

    public CelesteGame(string contentRoot, bool fpsEnabled = false, bool verboseLogs = false)
    {
        _contentRoot = contentRoot;
        _fpsEnabled = fpsEnabled;
        _verboseLogs = verboseLogs;

        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";

        // Fullscreen em Android
        _graphics.IsFullScreen = true;
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
    }

    protected override void Initialize()
    {
        base.Initialize();

        try
        {
            // Inicializar serviços de plataforma
            InitializePlatformServices();

            _logger?.Log(LogLevel.Info, "Game", "CelesteGame inicializado com sucesso");
            _logger?.Log(LogLevel.Info, "Game", $"ContentRoot: {_contentRoot}");
            _logger?.Log(LogLevel.Info, "Game", $"FPS Counter: {_fpsEnabled}");
            _logger?.Log(LogLevel.Info, "Game", $"Verbose Logs: {_verboseLogs}");

            // Validar conteúdo
            var locator = ServiceLocator.GetAssetLocator();
            if (!locator.ValidateContent(out var missing))
            {
                _logger?.Log(LogLevel.Fatal, "Game", $"Content validation falhou! Faltam: {string.Join(", ", missing)}");
                throw new InvalidOperationException("Content não validado");
            }

            _logger?.Log(LogLevel.Info, "Game", "Content validation passou");
        }
        catch (Exception ex)
        {
            _logger?.LogCrash(ex, "Erro na inicialização do game");
            throw;
        }
    }

    private void InitializePlatformServices()
    {
        // Criar implementações de plataforma
        _platformPaths = new DesktopPlatformPaths(_contentRoot);
        _platformPaths.EnsureDirectoriesExist();

        _logger = new LogSystemImpl();
        (_logger as LogSystemImpl)?.Initialize(_platformPaths.LogsRoot);

        // Registrar no service locator
        ServiceLocator.Initialize(_platformPaths, _logger);
    }

    protected override void LoadContent()
    {
        base.LoadContent();

        _spriteBatch = new SpriteBatch(GraphicsDevice);

        try
        {
            _logger?.Log(LogLevel.Info, "Game", "Carregando conteúdo do jogo...");

            // TODO: Carregar o jogo principal (Celeste Game Logic)
            // Para agora, apenas um placeholder de inicialização

            _logger?.Log(LogLevel.Info, "Game", "Conteúdo carregado com sucesso");
        }
        catch (Exception ex)
        {
            _logger?.LogError("Game", ex, "Erro ao carregar conteúdo");
            throw;
        }
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        // TODO: Lógica principal de atualização do jogo
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch?.Begin();
        {
            // TODO: Renderizar o jogo
            // Placeholder: desenhar texto de teste
        }
        _spriteBatch?.End();

        base.Draw(gameTime);
    }

    protected override void OnExiting(object sender, EventArgs args)
    {
        _logger?.Log(LogLevel.Info, "Game", "Aplicação encerrando");
        _logger?.Flush();

        base.OnExiting(sender, args);
    }
}
