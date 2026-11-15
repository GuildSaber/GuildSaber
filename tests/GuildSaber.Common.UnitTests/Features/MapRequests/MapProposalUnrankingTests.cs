using AwesomeAssertions;
using GuildSaber.Api.Features.MapRequests.Http;
using GuildSaber.Api.Features.MapRequests.Services;
using GuildSaber.Api.Features.MapRequests.Settings;
using GuildSaber.Api.Features.Songs;
using GuildSaber.Common.Services.BeatLeader;
using GuildSaber.Common.Services.BeatSaver;
using GuildSaber.Common.Services.ScoreSaber;
using GuildSaber.Common.StrongTypes;
using GuildSaber.Database.Models.Server.MapRequests.Proposals;
using GuildSaber.Database.Models.Server.MapRequests.Proposals.Types;
using GuildSaber.Database.Models.Server.RankedMaps;
using GuildSaber.Database.Models.Server.RankedScores;
using GuildSaber.Database.Models.Server.Scores;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties;
using GuildSaber.Database.Models.StrongTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ReviewResponse = GuildSaber.Api.Features.MapRequests.Services.MapProposalReviewService.ReviewResponse;

namespace GuildSaber.Common.UnitTests.Features.MapRequests;

public partial class MapProposalServiceTests
{
    [Test]
    public async Task Unranking_ShouldRequireTheReviewedRevisionAndAllowRankingAgainInTheSameThread()
    {
        await using var database = await TestDatabase.CreateAsync();
        var db = database.Context;
        var transitions = CreateService(db);
        var reviews = CreateReviewService(db, new RecordingQueue());
        await transitions.SubmitAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1), CancellationToken.None);
        var original = (await reviews.AcceptAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1),
            CancellationToken.None, bypassVoteRequirements: true)).Should().BeOfType<ReviewResponse.Accepted>().Which;

        var drafts = new MapProposalDraftService(db, new BeatSaverApi(_unusedHttpClient),
            new SongService(db, new BeatLeaderApi(_unusedHttpClient), new ScoreSaberApi(_unusedHttpClient)),
            new FixedTimeProvider(), Options.Create(new MapRequestSettings()));
        var removal = (await drafts.CreateAsync(new ContextId(1), new MapRequestThreadId(1), new PlayerId(1),
                new MapProposalRequests.CreateDraft.Unrank("Removal reason"), CancellationToken.None))
            .Should().BeOfType<MapProposalDraftService.CreateResponse.Success>().Which.Proposal;

        db.MapProposals.Add(CreateDraft(100));
        await db.SaveChangesAsync();
        await transitions.SubmitAsync(new ContextId(1), new MapProposalId(100), new PlayerId(1),
            CancellationToken.None);
        await reviews.AcceptAsync(new ContextId(1), new MapProposalId(100), new PlayerId(1),
            CancellationToken.None, bypassVoteRequirements: true);

        (await transitions.SubmitAsync(new ContextId(1), removal.Id, new PlayerId(1), CancellationToken.None))
            .Should().BeOfType<MapProposalService.TransitionResponse.TargetChanged>();
        (await db.MapProposals.SingleAsync(x => x.Id == removal.Id)).Should().BeOfType<DraftProposal>();

        var replacement = (await drafts.ReplaceAsync(new ContextId(1), new MapRequestThreadId(1), removal.Id,
            new PlayerId(1), new MapProposalRequests.CreateDraft.Unrank("Updated removal reason"),
            CancellationToken.None)).Should().BeOfType<MapProposalDraftService.CreateResponse.Success>().Which.Proposal;
        ((MapProposalChange.Unrank)replacement.Change).TargetAcceptedProposalId.Should().Be(new MapProposalId(100));
        (await db.MapProposals.AnyAsync(x => x.Id == removal.Id)).Should().BeFalse();

        await AddAccuracyCurveAsync(db);
        var point = await db.Points.SingleAsync();
        var score = new BeatLeaderScore
        {
            PlayerId = new PlayerId(1), SongDifficultyId = new SongDifficulty.SongDifficultyId(1),
            BeatLeaderScoreId = null,
            BaseScore = BaseScore.CreateUnsafe(115).Value, Modifiers = default, SetAt = _createdAt,
            MaxCombo = 1, IsFullCombo = true, MissedNotes = 0, BadCuts = 0, Statistics = null
        };
        db.Scores.Add(score);
        await db.SaveChangesAsync();
        db.RankedScores.Add(new ValidRankedScore
        {
            GuildId = new GuildId(1), ContextId = new ContextId(1), RankedMapId = original.RankedMapId,
            SongDifficultyId = new SongDifficulty.SongDifficultyId(1), PointId = point.Id, PlayerId = new PlayerId(1),
            ScoreId = score.Id, PrevScoreId = null, IsSelected = true,
            EffectiveScore = EffectiveScore.CreateUnsafe(115).Value, EditedAt = _transitionAt,
            RawPoints = RawPoints.CreateUnsafe(5).Value, Rank = 1
        });
        db.RankedMapPointCurves.Add(new RankedMapPointCurve
        {
            RankedMapId = original.RankedMapId, PointId = point.Id, Curve = _accuracyCurve, UpdatedAt = _transitionAt
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await transitions.SubmitAsync(new ContextId(1), replacement.Id, new PlayerId(1), CancellationToken.None);
        (await reviews.AcceptAsync(new ContextId(1), replacement.Id, new PlayerId(1), CancellationToken.None))
            .Should().BeOfType<ReviewResponse.VoteRequirementsNotMet>();
        (await reviews.AcceptAsync(new ContextId(1), replacement.Id, new PlayerId(1), CancellationToken.None,
            bypassVoteRequirements: true)).Should().BeOfType<ReviewResponse.Unranked>();

        (await db.RankedScores.CountAsync()).Should().Be(0);
        (await db.RankedMapPointCurves.CountAsync()).Should().Be(0);
        (await db.Scores.CountAsync()).Should().Be(1);

        db.MapProposals.Add(CreateDraft(200));
        await db.SaveChangesAsync();
        await transitions.SubmitAsync(new ContextId(1), new MapProposalId(200), new PlayerId(1),
            CancellationToken.None);
        var rankedAgain = (await reviews.AcceptAsync(new ContextId(1), new MapProposalId(200), new PlayerId(1),
            CancellationToken.None, bypassVoteRequirements: true)).Should().BeOfType<ReviewResponse.Accepted>().Which;

        rankedAgain.RankedMapId.Should().NotBe(original.RankedMapId);
        (await db.RankedMaps.SingleAsync()).ThreadId.Should().Be(new MapRequestThreadId(1));
        (await db.MapProposals.OfType<AcceptedProposal>().CountAsync()).Should().Be(4);
    }
}