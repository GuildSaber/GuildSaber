using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CP_SDK.UI.Components;
using CP_SDK.XUI;
using GuildSaber.Mod.Helpers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GuildSaber.Mod.Features.PlayerCard.UI.Components;

internal sealed class PlayerCardCategories : XUIVLayout
{
    private const int CategoriesPerPage = 6;
    private const float ContentHeight = 14;
    private const float PagerHeight = 3;
    private readonly List<CategoryCell> _categoryCells = [];
    private readonly List<CHOrVLayout> _categoryColumns = [];
    private ImmutableArray<PlayerCardCategoryAchievement> _categories = [];
    private Color _categoryAccent;
    private float _columnWidth = 28;
    private int _page;
    private XUISecondaryButton _pageLeft = null!;
    private XUISecondaryButton _pageRight = null!;
    private XUIHLayout _pager = null!;
    private XUIHLayout _row = null!;
    private XUIText _summary = null!;

    private PlayerCardCategories() : base("PlayerCardCategories")
    {
        SetPadding(0);
        SetSpacing(0);
        OnReady(Build);
    }

    public float Height => ContentHeight + (_categories.Length > CategoriesPerPage ? PagerHeight : 0);
    public float Width => _columnWidth * 2 + 22;

    private sealed class CategoryCell : XUIHLayout
    {
        private XUIText _text = null!;

        public CategoryCell() : base("PlayerCardCategory")
        {
            SetWidth(28);
            SetHeight(4.5f);
            SetPadding(0);
            SetSpacing(0);
            OnReady(Build);
        }

        public float Measure(PlayerCardCategoryAchievement category)
            => _text.Element.TMProUGUI.GetPreferredValues(GetText(category)).x;

        public void Render(PlayerCardCategoryAchievement? category, float width)
        {
            SetWidth(width);
            _text.Element.LElement.preferredWidth = width;
            SetActive(category.HasValue);
            if (category is not { } value) return;

            _text.SetText(GetText(value));
        }

        private static string GetText(PlayerCardCategoryAchievement category)
            => $"{category.CategoryName}: <b><color=#{ColorUtility.ToHtmlStringRGBA(category.Color)}>"
               + $"{category.AchievementName}</color></b>";

        private void Build(CHOrVLayout layout)
        {
            layout.CSizeFitter.horizontalFit = layout.CSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            layout.LElement.flexibleHeight = 0;
            layout.HOrVLayoutGroup.childForceExpandHeight = false;
            layout.HOrVLayoutGroup.childForceExpandWidth = false;
            layout.HOrVLayoutGroup.childAlignment = TextAnchor.MiddleCenter;

            XUIText.Make(string.Empty)
                .Bind(ref _text)
                .SetFontSize(3.6f)
                .SetFontSizes(2.3f, 3.6f)
                .SetAlign(TextAlignmentOptions.Midline)
                .SetWrapping(false)
                .SetColor(Color.white)
                .SetOverflowMode(TextOverflowModes.Ellipsis)
                .OnReady(x =>
                {
                    x.TMProUGUI.enableAutoSizing = true;
                    x.LElement.minHeight = 0;
                    x.LElement.preferredWidth = 28;
                    x.LElement.preferredHeight = 4.5f;
                    x.LElement.flexibleWidth = 0;
                })
                .BuildUI(layout.transform);
        }
    }

    public static PlayerCardCategories Make() => new();

    public void Render(PlayerCardProgress progress, Color accent)
    {
        _categories = progress.Categories;
        _categoryAccent = accent;
        _columnWidth = _categories.IsEmpty ? 0 : Mathf.Min(32, _categories.Max(_categoryCells[0].Measure) + 2);

        foreach (var column in _categoryColumns)
            column.SetWidth(_columnWidth);

        SetWidth(Width);
        _row.SetWidth(Width);
        _pager.SetWidth(Width);
        _summary
            .SetText(GetSummaryText(progress))
            .SetColor(progress.AverageCategoryAchievementColor);
        RenderPage();
    }

    private static string GetSummaryText(PlayerCardProgress progress)
    {
        var filledDots = Mathf.Clamp((int)Math.Round(progress.Equilibrium / 20), 0, 5);
        var dots = string.Join(" ", Enumerable.Range(0, 5)
            .Select(index => index < filledDots ? "●" : "<color=#FFFFFF40>●</color>"));

        return $"<line-height=80%><b><i>Avg {progress.AverageCategoryAchievement:0.##}</i></b>\n"
               + $"<size=84%>{dots}\n({progress.Equilibrium:0.##}%)</size></line-height>";
    }

    private void Build(CHOrVLayout layout)
    {
        layout.CSizeFitter.horizontalFit = layout.CSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        layout.LElement.flexibleHeight = 0;
        layout.HOrVLayoutGroup.childForceExpandHeight = false;
        layout.HOrVLayoutGroup.childAlignment = TextAnchor.MiddleCenter;

        XUIHLayout.Make(
                MakeCategoryColumn(),
                XUIText.Make(string.Empty)
                    .Bind(ref _summary)
                    .SetFontSize(3.8f)
                    .SetFontSizes(2.5f, 3.8f)
                    .SetAlign(TextAlignmentOptions.Midline)
                    .SetWrapping(false)
                    .OnReady(x =>
                    {
                        x.TMProUGUI.enableAutoSizing = true;
                        x.LElement.minHeight = 0;
                        x.LElement.preferredWidth = 20;
                        x.LElement.preferredHeight = ContentHeight;
                        x.LElement.flexibleWidth = 0;
                        x.LElement.flexibleHeight = 0;
                    }),
                MakeCategoryColumn())
            .OnReady(x =>
            {
                x.CSizeFitter.horizontalFit = x.CSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
                x.LElement.flexibleHeight = 0;
                x.HOrVLayoutGroup.childForceExpandHeight = false;
                x.HOrVLayoutGroup.childForceExpandWidth = false;
                x.HOrVLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
            })
            .SetWidth(78)
            .SetHeight(ContentHeight)
            .SetPadding(0)
            .SetSpacing(1)
            .Bind(ref _row)
            .BuildUI(layout.transform);

        XUIHLayout.Make(
                XUISecondaryButton.Make("<", PageLeft)
                    .Bind(ref _pageLeft).SetColor(Color.white).SetWidth(5).SetHeight(3),
                XUISecondaryButton.Make(">", PageRight)
                    .Bind(ref _pageRight).SetColor(Color.white).SetWidth(5).SetHeight(3))
            .Bind(ref _pager)
            .OnReady(x =>
            {
                x.CSizeFitter.horizontalFit = x.CSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
                x.LElement.flexibleHeight = 0;
            })
            .SetWidth(78)
            .SetHeight(PagerHeight)
            .SetPadding(0)
            .SetSpacing(16)
            .BuildUI(layout.transform);
    }

    private XUIVLayout MakeCategoryColumn()
        => XUIVLayout.Make()
            .OnReady(column =>
            {
                _categoryColumns.Add(column);
                column.CSizeFitter.horizontalFit =
                    column.CSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
                column.LElement.flexibleHeight = 0;
                column.HOrVLayoutGroup.childForceExpandHeight = false;
                column.HOrVLayoutGroup.childAlignment = TextAnchor.MiddleCenter;

                for (var i = 0; i < CategoriesPerPage / 2; i++)
                {
                    var category = new CategoryCell();
                    category.BuildUI(column.transform);
                    _categoryCells.Add(category);
                }
            })
            .SetWidth(28)
            .SetHeight(ContentHeight)
            .SetPadding(0)
            .SetSpacing(0.25f);

    private void RenderPage()
    {
        var lastPage = _categories.IsEmpty ? 0 : (_categories.Length - 1) / CategoriesPerPage;
        _page = Mathf.Clamp(_page, 0, lastPage);
        var categories = _categories
            .Skip(_page * CategoriesPerPage)
            .Take(CategoriesPerPage)
            .ToArray();
        ReadOnlySpan<int> displayOrder = [0, 2, 4, 1, 3, 5];

        for (var i = 0; i < _categoryCells.Count; i++)
        {
            var categoryIndex = displayOrder[i];
            _categoryCells[i].Render(categoryIndex < categories.Length ? categories[categoryIndex] : null,
                _columnWidth);
        }

        _pager.SetActive(lastPage > 0);
        SetPageButton(_pageLeft, _page > 0);
        SetPageButton(_pageRight, _page < lastPage);
    }

    private void PageLeft()
    {
        _page--;
        RenderPage();
    }

    private void PageRight()
    {
        _page++;
        RenderPage();
    }

    private void SetPageButton(XUISecondaryButton button, bool available)
        => button
            .SetColor(available ? _categoryAccent : Color.white.WithAlpha(0.25f))
            .SetInteractable(available);
}