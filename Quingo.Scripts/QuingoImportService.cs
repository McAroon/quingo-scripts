using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quingo.Infrastructure.Database;
using Quingo.Infrastructure.Database.Repos;
using Quingo.Shared.Entities;

namespace Quingo.Scripts;

public class QuingoImportService
{
    private readonly ILogger<QuingoImportService> _logger;
    private readonly PackRepo _repo;
    
    public QuingoImportService(ILogger<QuingoImportService> logger, PackRepo repo)
    {
        _logger = logger;
        _repo = repo;
    }
    
    public async Task<Pack> CreatePack(ApplicationDbContext db, string packName,
        IEnumerable<string> tags, IEnumerable<string> linkTypes)
    {
        var pack = await db.Packs.FirstOrDefaultAsync(x => x.Name == packName);
        if (pack == null)
        {
            pack = new Pack
            {
                Name = packName,
                Description = $"Generated on {DateTime.UtcNow:g}"
            };
            db.Packs.Add(pack);
            _logger.LogInformation("Created pack: {name}", packName);
        }
        else
        {
            pack = await _repo.GetPack(db, pack.Id, true);
            pack!.Description = $"Updated at {DateTime.UtcNow:g}";
        }

        foreach (var tag in tags)
        {
            CreateTag(tag, pack);
        }

        foreach (var link in linkTypes)
        {
            CreateLinkType(link, pack);
        }

        return pack;
    }

    public Node CreateNode(string name, string tag, Pack pack)
    {
        var node = pack.Nodes.FirstOrDefault(x => x.Name == name && x.HasTag(tag));
        if (node != null)
        {
            if (node.DeletedAt != null) 
                return null;
            return node;
        }

        node = new Node
        {
            Pack = pack,
            Name = name,
        };
        node.NodeTags.Add(new NodeTag
        {
            Node = node,
            Tag = pack.Tags.First(x => x.Name == tag)
        });
        pack.Nodes.Add(node);

        _logger.LogInformation("Created node: {name} {tag}", name, tag);

        return node;
    }
    
    public NodeLink CreateLink(Node nodeFrom, Node nodeTo, string linkType, Pack pack, bool restoreDeleted = false)
    {
        if (nodeFrom == null || nodeTo == null || nodeFrom.DeletedAt != null || nodeTo.DeletedAt != null) 
            return null;
        
        var link = nodeFrom.NodeLinksFrom.FirstOrDefault(x => x.NodeTo.Name == nodeTo.Name);
        
        if (link?.DeletedAt != null && !restoreDeleted) 
            return null;
        
        if (link == null)
        {
            link = new NodeLink
            {
                NodeLinkType = pack.NodeLinkTypes.First(x => x.Name == linkType),
                NodeFrom = nodeFrom,
                NodeTo = nodeTo,
            };
            nodeFrom.NodeLinksFrom.Add(link);

            _logger.LogInformation("Created node link: {from} {to} {type}", nodeFrom.Name, nodeTo.Name, linkType);
        }

        if (restoreDeleted && link.DeletedAt != null && link.DeletedByUserId == null)
        {
            link.DeletedAt = null;
            _logger.LogInformation("Restored node link: {from} {to} {type}", nodeFrom.Name, nodeTo.Name, linkType);
        }

        return link;
    }

    public void DeleteLink(Node nodeFrom, Node nodeTo, string linkType)
    {
        if (nodeFrom == null || nodeTo == null || nodeFrom.DeletedAt != null || nodeTo.DeletedAt != null) return;
        
        var link = nodeFrom.NodeLinksFrom.FirstOrDefault(x => x.NodeTo.Name == nodeTo.Name);
        if (link is not { DeletedAt: null }) return;
        
        link.DeletedAt = DateTime.UtcNow;
        _logger.LogInformation("Deleted node link: {from} {to} {type}", nodeFrom.Name, nodeTo.Name, linkType);
    }
    
    private void CreateTag(string tagName, Pack pack)
    {
        var tag = pack.Tags.FirstOrDefault(x => x.Name == tagName);
        if (tag != null) return;

        tag = new Tag { Name = tagName, Pack = pack };
        pack.Tags.Add(tag);
        _logger.LogInformation("Created tag: {name}", tagName);
    }

    private void CreateLinkType(string linkName, Pack pack)
    {
        var link = pack.NodeLinkTypes.FirstOrDefault(x => x.Name == linkName);
        if (link != null) return;

        link = new NodeLinkType { Name = linkName, Pack = pack };
        pack.NodeLinkTypes.Add(link);
        _logger.LogInformation("Created link type: {name}", linkName);
    }
}