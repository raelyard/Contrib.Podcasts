﻿using System.Collections.Generic;
using System.Linq;
using Contrib.Podcasts.Models;
using Contrib.Podcasts.Services;
using Contrib.Podcasts.ViewModels;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.Core.Common.Models;

namespace Contrib.Podcasts.Drivers {
  public class RecentPodcastEpisodesDriver : ContentPartDriver<RecentPodcastEpisodesPart> {
    private readonly IPodcastService _podcastService;
    private readonly IContentManager _contentManager;

    public RecentPodcastEpisodesDriver(IPodcastService podcastService, IContentManager contentManager) {
      _podcastService = podcastService;
      _contentManager = contentManager;
    }

    protected override string Prefix {
      get { return "RecentPodcastEpisodes"; }
    }

    protected override DriverResult Display(RecentPodcastEpisodesPart part, string displayType, dynamic shapeHelper) {
      var podcast = _contentManager.Get<PodcastPart>(part.PodcastId);
      if (podcast == null) return null;

      var episodes = _contentManager.Query(VersionOptions.Published, "PodcastEpisode")
        .Join<CommonPartRecord>().Where(cr => cr.Container == podcast.Record.ContentItemRecord)
        .OrderByDescending(cr => cr.CreatedUtc)
        .Slice(0, part.Count)
        .Select(ci => ci.As<PodcastEpisodePart>());

      var list = shapeHelper.List();
      list.AddRange(episodes.Select(e => _contentManager.BuildDisplay(e, "Summary")));

      var episodeList = shapeHelper.Parts_Podcasts_PodcastEpisode_List(ContentItems: list);

      return ContentShape("Parts_Podcasts_RecentPodcastEpisodes", () =>
          shapeHelper.Parts_Podcasts_RecentPodcastEpisodes(
            ContentItems: episodeList,
            Podcast: podcast,
            Prefix: Prefix
          )
      );
    }

    protected override DriverResult Editor(RecentPodcastEpisodesPart part, dynamic shapeHelper) {
      var viewModel = new RecentPodcastEpisodesViewModel {
        Count = part.Count,
        PodcastId = part.PodcastId,
        Podcasts = _podcastService.Get().ToList().OrderBy(p => p.Title)
      };

      return ContentShape("Parts_Podcasts_RecentPodcastEpisodes",
        () => shapeHelper.EditorTemplate(TemplateName: "Parts.Podcasts.RecentPodcastEpisodes", Model: viewModel, Prefix: Prefix));
    }

    protected override DriverResult Editor(RecentPodcastEpisodesPart part, IUpdateModel updater, dynamic shapeHelper) {
      var viewModel = new RecentPodcastEpisodesViewModel();

      if (updater.TryUpdateModel(viewModel, Prefix, null, null)) {
        part.PodcastId = viewModel.PodcastId;
        part.Count = viewModel.Count;
      }

      return Editor(part, shapeHelper);
    }
  }
}