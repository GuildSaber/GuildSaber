using AwesomeAssertions;
using GuildSaber.Api.Features.MapRequests;
using GuildSaber.Common.StrongTypes;
using GuildSaber.Database.Models.Server.MapRequests.Proposals;
using GuildSaber.Database.Models.Server.MapRequests.Proposals.Snapshots;
using GuildSaber.Database.Models.Server.MapRequests.Proposals.Types;
using GuildSaber.Database.Models.Server.RankedMaps;
using GuildSaber.Database.Models.Server.Scores;

namespace GuildSaber.Common.UnitTests.Features.MapRequests;

public class MapProposalTransitionTests
{
    [Test]
    public void Author_ShouldOnlySubmitDraftsAndCloseSubmittedProposals()
    {
        var draft = CreateDraft();
        var submitted = new SubmittedProposal
        {
            AuthorId = draft.AuthorId,
            CreatedAt = draft.CreatedAt,
            SubmittedAt = draft.CreatedAt,
            Rating = draft.Rating,
            Requirements = draft.Requirements
        };

        MapProposalService.GetTransitionError(draft, new PlayerId(1), MapProposalStatus.Submitted).Should().BeNull();
        MapProposalService.GetTransitionError(submitted, new PlayerId(1), MapProposalStatus.Closed).Should().BeNull();
        MapProposalService.GetTransitionError(draft, new PlayerId(1), MapProposalStatus.Closed)
            .Should().BeOfType<MapProposalService.TransitionResponse.InvalidState>();
        MapProposalService.GetTransitionError(submitted, new PlayerId(1), MapProposalStatus.Submitted)
            .Should().BeOfType<MapProposalService.TransitionResponse.InvalidState>();
        MapProposalService.GetTransitionError(draft, new PlayerId(2), MapProposalStatus.Submitted)
            .Should().BeOfType<MapProposalService.TransitionResponse.Forbidden>();
    }

    [Test]
    public void AcceptedAndClosedProposals_ShouldNotBeSubmittedOrClosedAgain()
    {
        var draft = CreateDraft();
        MapProposal[] proposals =
        [
            new AcceptedProposal
            {
                AuthorId = draft.AuthorId, CreatedAt = draft.CreatedAt, SubmittedAt = draft.CreatedAt,
                AcceptedAt = draft.CreatedAt, Rating = draft.Rating, Requirements = draft.Requirements
            },
            new ClosedProposal
            {
                AuthorId = draft.AuthorId, CreatedAt = draft.CreatedAt, ClosedAt = draft.CreatedAt,
                Rating = draft.Rating, Requirements = draft.Requirements
            }
        ];

        foreach (var proposal in proposals)
        foreach (var target in new[] { MapProposalStatus.Submitted, MapProposalStatus.Closed })
            MapProposalService.GetTransitionError(proposal, new PlayerId(1), target)
                .Should().BeOfType<MapProposalService.TransitionResponse.InvalidState>();
    }

    [Test]
    [Arguments(-1f)]
    [Arguments(float.NaN)]
    [Arguments(float.PositiveInfinity)]
    public void InvalidRating_ShouldPreventSubmission(float rating)
    {
        var draft = CreateDraft();
        draft.Rating.AccStar = new(rating);
        draft.Rating.DiffStar = new(rating);

        MapProposalService.GetConfigurationErrors(draft).Select(x => x.Key)
            .Should().BeEquivalentTo(["Rating.AccStar", "Rating.DiffStar"]);
    }

    [Test]
    [Arguments(0f)]
    [Arguments(float.MaxValue)]
    public void FiniteNonNegativeRatings_ShouldBeValid(float rating)
    {
        var draft = CreateDraft();
        draft.Rating.AccStar = new(rating);
        draft.Rating.DiffStar = new(rating);

        MapProposalService.GetConfigurationErrors(draft).Should().BeEmpty();
    }

    [Test]
    public void MissingVersions_ShouldPreventSubmission()
    {
        var draft = CreateDraft();
        draft.MapVersions.Clear();

        MapProposalService.GetConfigurationErrors(draft).Should().Contain(x => x.Key == nameof(MapProposal.MapVersions));
    }

    [Test]
    public void ContradictoryModifiers_ShouldPreventSubmission()
    {
        var draft = new DraftProposal
        {
            AuthorId = new PlayerId(1), CreatedAt = DateTimeOffset.UnixEpoch,
            Rating = new RankedMapRating { AccStar = new(1), DiffStar = new(1) },
            Requirements = new RankedMapRequirements(false, false, null,
                AbstractScore.EModifiers.FasterSong, AbstractScore.EModifiers.FasterSong, null),
            MapVersions = [new MapProposalVersion { PlayModeName = "Standard" }]
        };

        MapProposalService.GetConfigurationErrors(draft).Should().Contain(x => x.Key == "Requirements.MandatoryModifiers");
    }

    private static DraftProposal CreateDraft() => new()
    {
        AuthorId = new PlayerId(1),
        CreatedAt = DateTimeOffset.UnixEpoch,
        Rating = new RankedMapRating { AccStar = new(1), DiffStar = new(1) },
        Requirements = new RankedMapRequirements(false, false, null, 0, 0, null),
        MapVersions = [new MapProposalVersion { PlayModeName = "Standard" }]
    };
}
