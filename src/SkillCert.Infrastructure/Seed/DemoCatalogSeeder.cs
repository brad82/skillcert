using Microsoft.EntityFrameworkCore;
using SkillCert.Domain.Competencies;
using SkillCert.Domain.Groups;
using SkillCert.Domain.Lists;
using SkillCert.Domain.Reviewers;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Infrastructure.Seed;

/// <summary>
/// Creates the AFA Skills Record (its competencies and list tree) and the three patroller groups, assigns the
/// list to them and spreads the demo candidates across them. Skips everything if the list already exists.
/// </summary>
public sealed class DemoCatalogSeeder(SkillCertDbContext db, TimeProvider timeProvider)
{
    /// <summary>Which demo candidates (by number) belong to which group. Groups overlap on purpose (spec §6).</summary>
    private static readonly Dictionary<string, int[]> CandidatesByGroup = new()
    {
        ["New Patroller"] = [1, 2, 3, 4],
        ["Returning Patroller"] = [5, 6, 7],
        ["Senior Patroller"] = [7, 8, 9, 10],
    };

    public async Task<bool> SeedAsync(CancellationToken cancellationToken = default)
    {
        var definition = AfaRecordDefinition.Load();
        if (await db.CompetencyLists.AnyAsync(l => l.Title == definition.List.Title, cancellationToken))
        {
            return false;
        }

        // A year before "now", so the generated review history has published revisions to assess against.
        var at = timeProvider.GetUtcNow().AddDays(-450);
        var classifications = await db.ReviewerClassifications.ToListAsync(cancellationToken);
        var classificationIds = classifications.ToDictionary(c => c.Code, c => c.Id);

        var list = new CompetencyList(definition.List.Title, "Advanced First Aid practical skills record.", at);
        AddNodes(definition.Nodes, parentNodeId: null);
        db.CompetencyLists.Add(list);

        foreach (var (groupName, candidateNumbers) in CandidatesByGroup)
        {
            var group = new UserGroup(groupName, null, at);
            if (definition.List.AssignedGroups.Contains(groupName))
            {
                group.AssignList(list.Id, at);
            }

            var emails = candidateNumbers.Select(n => $"candidate{n:00}@skillcert.test").ToList();
            var userIds = await db.DomainUsers.Where(u => emails.Contains(u.Email)).Select(u => u.Id).ToListAsync(cancellationToken);
            foreach (var userId in userIds)
            {
                group.AddMember(userId, at);
            }

            db.UserGroups.Add(group);
        }

        await db.SaveChangesAsync(cancellationToken);
        return true;

        void AddNodes(IEnumerable<AfaNode> nodes, Guid? parentNodeId)
        {
            foreach (var node in nodes)
            {
                if (node.IsHeading)
                {
                    var heading = list.AddHeading(parentNodeId, node.Code, node.Title);
                    AddNodes(node.Children ?? [], heading.Id);
                    continue;
                }

                var methods = node.PermittedMethods ?? definition.Defaults.PermittedMethods;
                var competency = Competency.Create(
                    node.Code,
                    ReviewHierarchy.CloseUpward(new RevisionContent(
                        node.Title,
                        node.ShortTitle,
                        node.Description,
                        node.RecertificationDays ?? definition.Defaults.RecertificationDays,
                        AllowsSelfReview: methods.Contains("Self"),
                        AllowsPeerReview: methods.Contains("Peer"),
                        methods.Where(classificationIds.ContainsKey).Select(m => classificationIds[m]).ToList(),
                        (node.Resources ?? []).Select(r => new RevisionResource(r.Title, new Uri(r.Url), Enum.Parse<ResourceType>(r.Type))).ToList()),
                        classifications),
                    at,
                    byUserId: null);
                db.Competencies.Add(competency);
                list.AddCompetency(parentNodeId, competency.Id);
            }
        }
    }
}
