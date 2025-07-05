using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quingo.Infrastructure.Database.Repos;
using Quingo.Infrastructure.Files;

namespace Quingo.Scripts;

public class FbPicUpdate
{
    private readonly FileService _fileService;
    private readonly PackRepo _repo;
    private readonly FileStoreService _fileStoreService;
    private readonly ILogger<FbPicUpdate> _logger;

    private const string ImagesDir = "picUpdate20250216";
    private const string UploadPrefix = "fb20250216";
    

    public FbPicUpdate(FileService fileService, PackRepo repo, FileStoreService fileStoreService, ILogger<FbPicUpdate> logger)
    {
        _fileService = fileService;
        _repo = repo;
        _fileStoreService = fileStoreService;
        _logger = logger;
    }

    public async Task Execute()
    {
        await using var ctx = await _repo.CreateDbContext();
        var foundPack = await ctx.Packs.FirstAsync(x => x.Name == FbConstants.PackName && x.DeletedAt == null);
        var pack = await _repo.GetPack(ctx, foundPack.Id, true);
        var dir = _fileService.FilePath(ImagesDir);
        var files = Directory.EnumerateFiles(dir);

        foreach (var file in files)
        {
            var playerId = Path.GetFileNameWithoutExtension(file);
            var playerNode = pack!.Nodes
                .FirstOrDefault(x => x.HasTag(FbConstants.TagPlayer) && x.Meta.PropertiesDict["tm_player_id"] == playerId);
            if (playerNode == null)
            {
                _logger.LogWarning("Player not found {playerId}", playerId);
                continue;
            }
            
            if (playerNode.ImageUrl?.StartsWith("http") != true) continue;

            try
            {
                var fileName = Path.GetFileName(file);
                await using var stream = File.OpenRead(file);
                var uploadedFile = await _fileStoreService.UploadFile(fileName, "image/png", stream, UploadPrefix);
                playerNode.ImageUrl = uploadedFile;
                
                await ctx.SaveChangesAsync();
                _logger.LogInformation("Updated {playerId} {name}", playerId, playerNode.Name);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception");
            }
        }
    }
}