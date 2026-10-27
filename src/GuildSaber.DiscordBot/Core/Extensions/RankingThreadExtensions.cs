using GuildSaber.Api.Features.MapRequests.Http;

namespace GuildSaber.DiscordBot.Core.Extensions;

public static class RankingThreadExtensions
{
    extension(MapRequestResponses.Thread self)
    {
        /*
         {
             "content": "",
             "tts": false,
             "flags": 32768,
             "embeds": [],
             "components": [
               {
                 "id": 595072741,
                 "type": 17,
                 "accent_color": 16711719,
                 "components": [
                   {
                     "id": 351364253,
                     "type": 9,
                     "components": [
                       {
                         "id": 849954604,
                         "type": 10,
                         "content": "Seven Spice Blend (!bsr a3c3)\nMapper(s): Dennileon1\nDifficulty: Expert, Standard\n\n⭐: 1.00 | ✨: 1404.18 (Acc > 49%)\nCategories: Tech\nNJS: 20 | Length: 01:06 | BPM: 194"
                       }
                     ],
                     "accessory": {
                       "id": 147452488,
                       "type": 11,
                       "media": {
                         "url": "https://cfcdn.beatsaver.com/c4ccc41a43bb15f252b025f03bce6f9c1dbbdbeb.jpg"
                       },
                       "spoiler": false
                     }
                   },
                   {
                     "id": 880878175,
                     "type": 9,
                     "components": [
                       {
                         "id": 696370750,
                         "type": 10,
                         "content": "**1** Submitted proposals"
                       }
                     ],
                     "accessory": {
                       "id": 367264267,
                       "type": 2,
                       "style": 2,
                       "label": "Open New Thread",
                       "action_set_id": "784637404"
                     }
                   }
                 ]
               },
               {
                 "id": 603092895,
                 "type": 17,
                 "accent_color": 15794607,
                 "components": [
                   {
                     "id": 470657179,
                     "type": 9,
                     "components": [
                       {
                         "id": 593086729,
                         "type": 10,
                         "content": "Kyatatsuki - WORLD ABLAZE (!bsr 4bc34)\nMapper(s): Sevenn\nDifficulty: ExpertPlus, Standard\n\n⭐: 3.00 | ✨: 1225.55 (Acc > 44%)\n-> ⭐: 4.00 | ✨: 1404.18 (Acc > 49%)\n-> ⭐: 3.50 | ✨: 1404.18 (Acc > 55%)\n\nCategories: Jumps\n-> Categories: Tech, Vibro\n-> Categories: Streams\n\nNJS: 21 | Length: 03:24 | BPM: 300"
                       }
                     ],
                     "accessory": {
                       "id": 372669973,
                       "type": 11,
                       "media": {
                         "url": "https://cdn.beatsaver.com/4d391bd396d1c3412eb9fad8d285ec2c3abb02b8.jpg"
                       },
                       "spoiler": false
                     }
                   },
                   {
                     "id": 695463091,
                     "type": 9,
                     "components": [
                       {
                         "id": 342772563,
                         "type": 10,
                         "content": "**3** Submitted proposals"
                       }
                     ],
                     "accessory": {
                       "id": 968115632,
                       "type": 2,
                       "style": 2,
                       "label": "Open Edit Thread",
                       "action_set_id": "301755730"
                     }
                   }
                 ]
               },
               {
                 "id": 624025323,
                 "type": 10,
                 "content": "Page: 10/12 (48 Thread)"
               },
               {
                 "id": 400781851,
                 "type": 1,
                 "components": [
                   {
                     "id": 47052564,
                     "type": 3,
                     "placeholder": "Popularity",
                     "disabled": false,
                     "options": [
                       {
                         "label": "Popularity",
                         "emoji": {
                           "name": "👀",
                           "animated": false
                         },
                         "action_set_id": "318171233"
                       },
                       {
                         "label": "Most boosted",
                         "emoji": {
                           "name": "🪙",
                           "animated": false
                         },
                         "action_set_id": "5048558"
                       }
                     ]
                   }
                 ]
               },
               {
                 "id": 866634814,
                 "type": 1,
                 "components": [
                   {
                     "id": 595743645,
                     "type": 2,
                     "style": 2,
                     "label": "Back to menu",
                     "action_set_id": "559980459"
                   },
                   {
                     "id": 127064452,
                     "type": 2,
                     "style": 1,
                     "label": "Prev",
                     "action_set_id": "300780617"
                   },
                   {
                     "id": 778892619,
                     "type": 2,
                     "style": 1,
                     "label": "Next",
                     "action_set_id": "99213569"
                   }
                 ]
               }
             ],
             "actions": {
               "5048558": {
                 "actions": []
               },
               "99213569": {
                 "actions": []
               },
               "300780617": {
                 "actions": []
               },
               "301755730": {
                 "actions": []
               },
               "318171233": {
                 "actions": []
               },
               "559980459": {
                 "actions": []
               },
               "784637404": {
                 "actions": []
               }
             }
           }
         */


        /*
        public ContainerBuilder ToContainerBuilder(
            RankedMapResponses.RankedMap? rankedMap,
            CategoryResponses.Category[] categories,
            IOptions<EmojiSettings> emojiSettings)
        {
            Trace.Assert(
                self.RankedMapId == rankedMap?.Id,
                "Thread's RankedMapId does not match the provided RankedMap's Id."
            );

            var sectionBuilder = new SectionBuilder();
            var threadContainerBuilder = new ContainerBuilder();

            foreach (var (i, proposal) in self.Proposals.Index())
            {
                var versions = proposal.Configuration.MapVersions;
                if (i > 0) threadContainerBuilder.WithSeparator();
                else threadContainerBuilder.WithAccentColor(Color.FromDifficulty(versions.First().Difficulty));

                var sb = new StringBuilder()
                    .Append("**[").Append(song.Info.BeatSaverName).Append("](https://beatsaver.com/maps/")
                    .Append(song.Key).Append(")**")
                    .Append(" (").Append(song.Key is { } key ? key.ToBsrKey() : "no !bsr").Append(")\n")
                    .Append("Mapper(s): ").AppendLine(song.Info.MapperName)
                    .Append("Difficulty: ").Append(version.Difficulty.Difficulty.ToString())
                    .Append(", ").AppendLine(version.Difficulty.GameMode)
                    .AppendLine();

                sb.Append("⭐: ").Append(self.Rating.DiffStar.ToString("0.00")).Append(" | ");
                sb.Append("✨: ").Append(self.Rating.AccStar.ToString("0.00"));
            }
    */
    }
}